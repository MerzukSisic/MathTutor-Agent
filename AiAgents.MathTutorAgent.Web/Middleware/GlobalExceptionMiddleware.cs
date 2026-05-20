using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AiAgents.MathTutorAgent.Web.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly Action<ILogger, Exception?> LogUnhandledExceptionMessage =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2200, nameof(LogUnhandledExceptionMessage)),
            "Unhandled exception occurred");

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            LogUnhandledExceptionMessage(logger, ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        var (statusCode, message) = MapException(context, exception);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            statusCode = context.Response.StatusCode,
            message,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static (int StatusCode, string Message) MapException(HttpContext context, Exception exception)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            return (499, "Request canceled.");
        }

        var mapped = exception switch
        {
            AntiforgeryValidationException => ((int)HttpStatusCode.BadRequest, "Invalid request verification token. Refresh and try again."),
            CryptographicException => ((int)HttpStatusCode.BadRequest, "Session security token is invalid or expired. Refresh and try again."),
            BadHttpRequestException => ((int)HttpStatusCode.BadRequest, "Bad request."),
            ValidationException => ((int)HttpStatusCode.BadRequest, "Some fields are invalid. Please check your input and try again."),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Requested resource was not found."),
            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "You do not have permission to perform this action."),
            InvalidOperationException => ((int)HttpStatusCode.BadRequest, "The requested operation is not allowed in the current state."),
            DbUpdateException dbEx when IsUniqueConstraintViolation(dbEx) =>
                ((int)HttpStatusCode.Conflict, "This record already exists. Please use a different value."),
            DbUpdateException => ((int)HttpStatusCode.BadRequest, "Could not save changes. Please verify the submitted data."),
            PostgresException => ((int)HttpStatusCode.ServiceUnavailable, "The service is temporarily unavailable. Please try again in a moment."),
            NpgsqlException => ((int)HttpStatusCode.ServiceUnavailable, "The service is temporarily unavailable. Please try again in a moment."),
            SmtpException => ((int)HttpStatusCode.ServiceUnavailable, "Email service is currently unavailable. Please try again later."),
            TimeoutException => ((int)HttpStatusCode.RequestTimeout, "The request took too long. Please try again."),
            _ => ((int)HttpStatusCode.InternalServerError, "Something went wrong. Please try again later.")
        };

        if (mapped.Item1 == (int)HttpStatusCode.InternalServerError &&
            context.Request.Path.StartsWithSegments("/api/auth/register", StringComparison.OrdinalIgnoreCase))
        {
            return ((int)HttpStatusCode.InternalServerError, "Registration is temporarily unavailable. Please try again later.");
        }

        return mapped;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pgEx)
        {
            return string.Equals(pgEx.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal);
        }

        return false;
    }
}

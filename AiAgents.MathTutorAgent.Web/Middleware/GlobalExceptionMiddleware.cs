using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;

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

        var (statusCode, message) = exception switch
        {
            AntiforgeryValidationException => ((int)HttpStatusCode.BadRequest, "Invalid request verification token. Refresh and try again."),
            BadHttpRequestException => ((int)HttpStatusCode.BadRequest, "Bad request."),
            _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error")
        };

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
}

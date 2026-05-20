using System.Net;
using System.Text.Json;

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

    private static Task HandleExceptionAsync(HttpContext context, Exception _)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            statusCode = context.Response.StatusCode,
            message = "Internal Server Error",
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

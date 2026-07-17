using System.Net;
using System.Text.Json;

namespace AppApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception [{CorrelationId}]: {Message}", correlationId, ex.Message);
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, exception.Message),
            InvalidOperationException => ((int)HttpStatusCode.BadRequest, exception.Message),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, exception.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, exception.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (!_env.IsDevelopment() && statusCode >= 500)
            message = "An unexpected error occurred";

        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var response = new
        {
            success = false,
            message,
            errors = (List<string>?)null,
            correlationId
        };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
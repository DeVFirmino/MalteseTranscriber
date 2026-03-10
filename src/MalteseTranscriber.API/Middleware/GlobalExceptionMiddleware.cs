using System.Net;
using System.Text.Json;
using MalteseTranscriber.Core.Exceptions;

namespace MalteseTranscriber.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, errorType) = ex switch
        {
            SessionNotFoundException        => (HttpStatusCode.NotFound,           "session_not_found"),
            MaxConcurrentSessionsException  => (HttpStatusCode.ServiceUnavailable, "max_sessions_reached"),
            TranscriptionConnectionException => (HttpStatusCode.BadGateway,        "transcription_error"),
            ArgumentException               => (HttpStatusCode.BadRequest,         "validation_error"),
            UnauthorizedAccessException     => (HttpStatusCode.Unauthorized,       "unauthorized"),
            InvalidOperationException       => (HttpStatusCode.Conflict,           "invalid_operation"),
            TimeoutException                => (HttpStatusCode.GatewayTimeout,     "timeout"),
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.TooManyRequests
                                            => (HttpStatusCode.TooManyRequests,    "rate_limited"),
            _                               => (HttpStatusCode.InternalServerError, "internal_error")
        };

        _logger.LogError(ex,
            "Unhandled exception [{ErrorType}] on {Method} {Path}",
            errorType, context.Request.Method, context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = errorType,
            status = (int)statusCode,
            message = _env.IsDevelopment() ? ex.Message : "An error occurred.",
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}

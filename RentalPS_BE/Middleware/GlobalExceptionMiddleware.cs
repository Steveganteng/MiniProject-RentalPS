using RentalPS_BE.Exceptions;

namespace RentalPS_BE.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request {Method} {Path} was cancelled by the client.",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "An exception occurred after the response started for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);

                throw;
            }

            var (statusCode, message) = MapException(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(
                    "Request {Method} {Path} rejected with HTTP {StatusCode}: {Message}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    message,
                    context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            await context.Response.WriteAsJsonAsync(
                new
                {
                    status = "error",
                    message,
                    timestamp = DateTime.UtcNow,
                    traceId = context.TraceIdentifier
                },
                cancellationToken: context.RequestAborted);
        }
    }

    private static (int StatusCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            NotFoundException =>
                (StatusCodes.Status404NotFound, exception.Message),

            InvalidPaymentAmountException =>
                (StatusCodes.Status400BadRequest, exception.Message),

            InvalidRentalDurationException =>
                (StatusCodes.Status400BadRequest, exception.Message),

            ArgumentException =>
                (StatusCodes.Status400BadRequest, exception.Message),

            InventoryNotAvailableException =>
                (StatusCodes.Status409Conflict, exception.Message),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "Terjadi kesalahan internal pada server. Silakan coba kembali atau hubungi administrator.")
        };
    }
}

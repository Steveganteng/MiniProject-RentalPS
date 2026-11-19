using System.Net;

namespace RentalPS_BE.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                await ctx.Response.WriteAsJsonAsync(new
                {
                    status = "error",
                    message = "Terjadi kesalahan pada server.",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
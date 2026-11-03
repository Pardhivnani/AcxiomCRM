using System.Net;
using System.Text.Json;

namespace AcxiomCRM.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred during request execution at {Path}", context.Request.Path);

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    await HandleApiExceptionAsync(context, ex);
                }
                else
                {
                    // Redirect or re-execute error handler
                    throw;
                }
            }
        }

        private static async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                status = 500,
                message = "An unexpected error occurred while processing the request.",
                details = exception.Message
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}


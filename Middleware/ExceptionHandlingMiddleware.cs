using Microsoft.AspNetCore.Http;
using Serilog;
using System.Net;

namespace Ice_Cream_Parlour_Eproject.Middleware
{
    /// <summary>
    /// Global exception handling middleware that catches unhandled exceptions
    /// and returns appropriate error responses.
    /// </summary>
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
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                message = "An error occurred while processing your request.",
                details = exception.Message
            };

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new { message = "Invalid request parameters.", details = exception.Message };
                    break;

                case KeyNotFoundException:
                case InvalidOperationException when exception.Message.Contains("not found"):
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = new { message = "The requested resource was not found.", details = exception.Message };
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new { message = "You do not have permission to access this resource.", details = exception.Message };
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}

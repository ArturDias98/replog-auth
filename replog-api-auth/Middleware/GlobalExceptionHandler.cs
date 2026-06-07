using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace replog_api_auth.Middleware;

public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new Models.Responses.ErrorResponse
            {
                Error = "internal_error",
                Message = "An unexpected error occurred."
            });
        }
    }
}

using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ParkingApp.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Forbidden"),
                ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
                InvalidOperationException => (HttpStatusCode.Conflict, "Conflict"),
                DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Concurrency Conflict"),
                _ => (HttpStatusCode.InternalServerError, "Server Error")
            };

            if (status == HttpStatusCode.InternalServerError)
                logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("{Status} for {Method} {Path}: {Message}", (int)status, context.Request.Method, context.Request.Path, ex.Message);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";

            var payload = JsonSerializer.Serialize(new
            {
                status = (int)status,
                title,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(payload);
        }
    }
}

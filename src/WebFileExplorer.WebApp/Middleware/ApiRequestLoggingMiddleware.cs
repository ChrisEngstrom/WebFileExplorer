using System.Diagnostics;

namespace WebFileExplorer.WebApp.Middleware;

// One API summary without query strings or file paths in the log message.
public sealed class ApiRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ApiRequestLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _next = next;
        _logger = loggerFactory.CreateLogger("WebFileExplorer.WebApp.RequestSummary");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        long startedAt = Stopwatch.GetTimestamp();
        int statusCode = StatusCodes.Status500InternalServerError;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        finally
        {
            // Use the route template, not the request URL, to avoid logging user paths.
            string route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(unmatched)";

            _logger.LogInformation(
                "HTTP {Method} {Route} -> {StatusCode} in {ElapsedMilliseconds:F1} ms",
                context.Request.Method,
                route,
                statusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }
}
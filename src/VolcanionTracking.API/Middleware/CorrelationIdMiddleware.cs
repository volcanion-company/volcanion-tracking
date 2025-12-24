using System.Diagnostics;

namespace VolcanionTracking.API.Middleware;

/// <summary>
/// Middleware to add correlation ID to all requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        
        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Set in Activity for OpenTelemetry tracing
        Activity.Current?.SetTag("correlation_id", correlationId);

        await _next(context);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            return correlationId.ToString();
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}

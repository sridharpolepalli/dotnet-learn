using ObservabilityDemo.Common;
using Serilog.Context;

namespace ObservabilityDemo.OrdersApi;

/// <summary>
/// Middleware that ensures every request has a correlation ID, adds it to response and log context.
/// Demonstrates: Correlation ID, enricher (via LogContext), distributed logging.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        string? incomingId = context.Request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault();
        string correlationId = CorrelationIdHelper.GetOrCreate(incomingId);

        context.Items[CorrelationIdConstants.HttpContextItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Push to Serilog so all logs in this request include CorrelationId (enricher pattern).
        using (LogContext.PushProperty(CorrelationIdConstants.LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}

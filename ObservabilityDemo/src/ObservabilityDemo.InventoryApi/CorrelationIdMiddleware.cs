using ObservabilityDemo.Common;
using Serilog.Context;

namespace ObservabilityDemo.InventoryApi;

/// <summary>
/// Middleware that reads or generates correlation ID, adds it to response and log context.
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

        using (LogContext.PushProperty(CorrelationIdConstants.LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}

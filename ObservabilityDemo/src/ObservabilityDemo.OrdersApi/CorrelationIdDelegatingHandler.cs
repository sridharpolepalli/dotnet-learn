using ObservabilityDemo.Common;
using System.Diagnostics;

namespace ObservabilityDemo.OrdersApi;

/// <summary>
/// Forwards correlation ID (and optional W3C trace context) to outgoing HTTP requests for distributed tracing.
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items[CorrelationIdConstants.HttpContextItemKey] is string correlationId)
            request.Headers.TryAddWithoutValidation(CorrelationIdConstants.HeaderName, correlationId);

        var activity = Activity.Current;
        if (activity != null)
        {
            request.Headers.TryAddWithoutValidation("traceparent", activity.Id);
            if (activity.TraceStateString != null)
                request.Headers.TryAddWithoutValidation("tracestate", activity.TraceStateString);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

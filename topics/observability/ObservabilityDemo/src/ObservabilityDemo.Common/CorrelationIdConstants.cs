namespace ObservabilityDemo.Common;

/// <summary>
/// Header and context keys used for correlation ID propagation across services and logs.
/// </summary>
public static class CorrelationIdConstants
{
    /// <summary>HTTP header name for correlation ID (request and response).</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Key used in HttpContext.Items to store the correlation ID for the request.</summary>
    public const string HttpContextItemKey = "CorrelationId";

    /// <summary>Serilog property name for correlation ID in log enrichers.</summary>
    public const string LogPropertyName = "CorrelationId";
}

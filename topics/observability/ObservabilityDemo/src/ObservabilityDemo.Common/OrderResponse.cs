namespace ObservabilityDemo.Common;

/// <summary>
/// Response DTO returned by OrdersApi after processing.
/// </summary>
public record OrderResponse(string OrderId, string ProductId, int Quantity, string Status, DateTime CreatedAt);

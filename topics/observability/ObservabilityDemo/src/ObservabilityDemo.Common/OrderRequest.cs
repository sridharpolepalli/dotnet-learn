namespace ObservabilityDemo.Common;

/// <summary>
/// Request DTO for creating an order (OrdersApi).
/// </summary>
public record OrderRequest(string ProductId, int Quantity, string CustomerId);

namespace ObservabilityDemo.Common;

/// <summary>
/// Response from InventoryApi for a stock check.
/// </summary>
public record InventoryResponse(string ProductId, int AvailableQuantity, bool InStock);

namespace ObservabilityDemo.Common;

/// <summary>
/// Helper for generating or reusing correlation IDs.
/// </summary>
public static class CorrelationIdHelper
{
    /// <summary>
    /// Returns the existing correlation ID if non-empty; otherwise generates a new one.
    /// </summary>
    public static string GetOrCreate(string? existingId)
    {
        return !string.IsNullOrWhiteSpace(existingId)
            ? existingId.Trim()
            : Guid.NewGuid().ToString("N");
    }
}

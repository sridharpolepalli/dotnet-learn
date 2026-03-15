using ObservabilityDemo.Common;
using Serilog.Context;

namespace ObservabilityDemo.OrdersApi;

/// <summary>
/// Hangfire background job: simulates order fulfillment. Logs use correlation ID for distributed logging.
/// </summary>
public class OrderFulfillmentJob
{
    private readonly ILogger<OrderFulfillmentJob> _logger;

    public OrderFulfillmentJob(ILogger<OrderFulfillmentJob> logger) => _logger = logger;

    public async Task ProcessOrderAsync(string orderId, string productId, int quantity, string correlationId)
    {
        // Push correlation ID into log context so all job logs are correlated with the original request.
        using (LogContext.PushProperty(CorrelationIdConstants.LogPropertyName, correlationId))
        {
            _logger.LogInformation(
                "Background job started. {OrderId}, {ProductId}, {Quantity}",
                orderId, productId, quantity);

            await Task.Delay(TimeSpan.FromSeconds(2)); // Simulate work

            _logger.LogInformation("Background job completed. {OrderId}", orderId);
        }
    }
}

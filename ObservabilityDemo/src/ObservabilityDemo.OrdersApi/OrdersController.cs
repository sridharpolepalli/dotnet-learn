using Microsoft.AspNetCore.Mvc;
using ObservabilityDemo.Common;

namespace ObservabilityDemo.OrdersApi;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public OrdersController(
        ILogger<OrdersController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    /// <summary>
    /// Creates an order: checks inventory (calls InventoryApi), enqueues background job, returns order.
    /// Demonstrates: structured logging, correlation ID propagation, distributed call, Hangfire.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] OrderRequest request, CancellationToken cancellationToken)
    {
        string correlationId = HttpContext.Items[CorrelationIdConstants.HttpContextItemKey] as string ?? "unknown";

        _logger.LogInformation(
            "CreateOrder started. {ProductId}, {Quantity}, {CustomerId}",
            request.ProductId, request.Quantity, request.CustomerId);

        _logger.LogDebug("CorrelationId for this request: {CorrelationId}", correlationId);

        // Call InventoryApi (distributed call – correlation ID forwarded via HttpClient)
        var inventoryClient = _httpClientFactory.CreateClient("InventoryApi");
        var inventoryUrl = $"{_config["InventoryApi:BaseUrl"]?.TrimEnd('/')}/api/inventory/check?productId={request.ProductId}";
        HttpResponseMessage? inventoryResponse = null;
        try
        {
            inventoryResponse = await inventoryClient.GetAsync(inventoryUrl, cancellationToken);
            inventoryResponse.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory check failed for {ProductId}", request.ProductId);
            return StatusCode(500, "Inventory check failed.");
        }

        var inventory = await inventoryResponse.Content.ReadFromJsonAsync<InventoryResponse>(cancellationToken);
        if (inventory == null || !inventory.InStock || inventory.AvailableQuantity < request.Quantity)
        {
            _logger.LogWarning(
                "Insufficient stock for {ProductId}. Requested {Quantity}, Available {Available}",
                request.ProductId, request.Quantity, inventory?.AvailableQuantity ?? 0);
            return BadRequest(new { reason = "Insufficient stock" });
        }

        var orderId = $"ORD-{Guid.NewGuid():N}"[..12];
        _logger.LogInformation("Order {OrderId} created for product {ProductId}, quantity {Quantity}", orderId, request.ProductId, request.Quantity);

        // Enqueue background job (pass correlation ID so job logs are correlated)
        BackgroundJob.Enqueue<OrderFulfillmentJob>(j => j.ProcessOrderAsync(orderId, request.ProductId, request.Quantity, correlationId));

        var response = new OrderResponse(orderId, request.ProductId, request.Quantity, "Accepted", DateTime.UtcNow);
        _logger.LogInformation("CreateOrder completed. {OrderId}, {Status}", response.OrderId, response.Status);
        return Ok(response);
    }
}

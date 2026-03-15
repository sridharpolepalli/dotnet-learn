using Microsoft.AspNetCore.Mvc;
using ObservabilityDemo.Common;

namespace ObservabilityDemo.InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ILogger<InventoryController> _logger;
    private static readonly Dictionary<string, int> Store = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PROD-001"] = 100,
        ["PROD-002"] = 50,
        ["PROD-003"] = 25
    };

    public InventoryController(ILogger<InventoryController> logger) => _logger = logger;

    /// <summary>
    /// Checks stock for a product. Demonstrates structured logging and correlation ID from caller.
    /// </summary>
    [HttpGet("check")]
    public ActionResult<InventoryResponse> Check([FromQuery] string productId)
    {
        _logger.LogDebug("Check inventory for {ProductId}", productId);

        if (string.IsNullOrWhiteSpace(productId))
        {
            _logger.LogWarning("Check called with empty ProductId");
            return BadRequest("productId required");
        }

        int available = Store.TryGetValue(productId.Trim(), out int qty) ? qty : 0;
        bool inStock = available > 0;
        var response = new InventoryResponse(productId, available, inStock);

        _logger.LogInformation(
            "Inventory check result. {ProductId}, {AvailableQuantity}, {InStock}",
            response.ProductId, response.AvailableQuantity, response.InStock);

        return Ok(response);
    }
}

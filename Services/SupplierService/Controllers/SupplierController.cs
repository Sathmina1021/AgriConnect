using AgriConnect.Shared.Models;
using AgriConnect.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using SupplierService.Services;

namespace SupplierService.Controllers;

[ApiController]
public class SupplierController : ControllerBase
{
    private readonly SupplierBusinessService _service;

    public SupplierController(SupplierBusinessService service) => _service = service;

    private IActionResult Unauth() => StatusCode(401, new { success = false, message = "Please log in to continue." });

    [HttpGet("/health")]
    public IActionResult Health() => Ok(_service.GetHealth());

    [HttpGet("/api/supplier/inventory")]
    public IActionResult GetInventory()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return Ok(_service.GetInventory());
    }

    [HttpPost("/api/supplier/inventory")]
    public async Task<IActionResult> AddInventory([FromBody] InventoryCreateRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        if (string.IsNullOrWhiteSpace(req.ProductName)) return BadRequest(new { success = false, message = "Product name is required." });
        var item = await _service.AddInventoryAsync(req);
        return Ok(new { success = true, id = item.Id });
    }

    [HttpPut("/api/supplier/inventory/{id:int}")]
    public async Task<IActionResult> UpdateInventory(int id, [FromBody] InventoryUpdateRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return await _service.UpdateInventoryAsync(id, req) ? Ok(new { success = true }) : NotFound();
    }

    [HttpDelete("/api/supplier/inventory/{id:int}")]
    public async Task<IActionResult> DeleteInventory(int id)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return await _service.DeleteInventoryAsync(id) ? Ok(new { success = true }) : NotFound();
    }

    [HttpGet("/api/supplier/farmer-requests")]
    public async Task<IActionResult> GetFarmerRequests()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return Ok(await _service.GetOpenFarmerRequestsAsync());
    }

    [HttpGet("/api/supplier/quotations")]
    public IActionResult GetQuotations()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return Ok(_service.GetQuotations());
    }

    [HttpPost("/api/supplier/quotations")]
    public async Task<IActionResult> SubmitQuotation([FromBody] QuotationCreateRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        if (req.SupplyRequestId <= 0 || req.UnitPrice <= 0 || req.OfferedQty <= 0)
            return BadRequest(new { success = false, message = "Valid supply request, unit price, and quantity are required." });

        var (ok, err, q) = await _service.SubmitQuotationAsync(req);
        if (!ok || q is null) return BadRequest(new { success = false, message = err });
        return Ok(new { success = true, id = q.Id });
    }

    [HttpGet("/api/supplier/deliveries")]
    public IActionResult GetDeliveries()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return Ok(_service.GetDeliveries());
    }

    [HttpPut("/api/supplier/deliveries/{id:int}/status")]
    public async Task<IActionResult> UpdateDeliveryStatus(int id, [FromBody] DeliveryStatusRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return await _service.UpdateDeliveryStatusAsync(id, req.Status) ? Ok(new { success = true }) : NotFound();
    }

    [HttpGet("/api/supplier/crops")]
    public async Task<IActionResult> GetCrops()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Supplier")) return Unauth();
        return Ok(await _service.GetAvailableCropsAsync());
    }

    [HttpGet("/api/supplier/internal/quotations")]
    public IActionResult InternalGetQuotations()
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.GetQuotations());
    }

    [HttpGet("/api/supplier/internal/quotations/{requestId:int}")]
    public IActionResult InternalGetQuotationsByRequest(int requestId)
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.GetQuotationsByRequest(requestId));
    }

    [HttpPost("/api/supplier/internal/confirm")]
    public IActionResult InternalConfirm([FromBody] ConfirmQuotationInternalRequest req)
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.ConfirmQuotationInternal(req));
    }

    [HttpGet("/api/supplier/internal/deliveries")]
    public IActionResult InternalGetDeliveries()
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.GetDeliveries());
    }
}

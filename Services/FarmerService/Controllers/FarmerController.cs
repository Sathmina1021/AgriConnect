using AgriConnect.Shared.Models;
using AgriConnect.Shared.Security;
using FarmerService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FarmerService.Controllers;

[ApiController]
public class FarmerController : ControllerBase
{
    private readonly FarmerBusinessService _service;

    public FarmerController(FarmerBusinessService service) => _service = service;

    private IActionResult Unauth() => StatusCode(401, new { success = false, message = "Please log in to continue." });

    [HttpGet("/health")]
    public IActionResult Health() => Ok(_service.GetHealth());

    [HttpGet("/api/farmer/profiles")]
    public IActionResult GetProfiles()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return Ok(_service.GetProfiles());
    }

    [HttpPost("/api/farmer/register")]
    public async Task<IActionResult> Register([FromBody] FarmerRegisterRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest(new { success = false, message = "Full name is required." });
        var profile = await _service.RegisterFarmerAsync(req);
        return Ok(new { success = true, id = profile.Id });
    }

    [HttpGet("/api/farmer/requests")]
    public async Task<IActionResult> GetRequests()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return Ok(await _service.GetSupplyRequestsWithQuoteCountsAsync());
    }

    [HttpPost("/api/farmer/requests")]
    public async Task<IActionResult> CreateRequest([FromBody] SupplyRequestCreate req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        if (string.IsNullOrWhiteSpace(req.ItemName) || req.Quantity <= 0)
            return BadRequest(new { success = false, message = "Valid item name and quantity are required." });
        var created = await _service.CreateSupplyRequestAsync(req);
        return Ok(new { success = true, id = created.Id });
    }

    [HttpGet("/api/farmer/requests/{id:int}/quotations")]
    public async Task<IActionResult> GetRequestQuotations(int id)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return Ok(await _service.GetQuotationsForRequestAsync(id));
    }

    [HttpPost("/api/farmer/requests/{id:int}/confirm")]
    public async Task<IActionResult> ConfirmOrder(int id, [FromBody] ConfirmOrderRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        var (ok, code, msg) = await _service.ConfirmOrderAsync(id, req);
        if (!ok) return StatusCode(code, new { success = false, message = msg });
        return Ok(new { success = true });
    }

    [HttpGet("/api/farmer/deliveries")]
    public async Task<IActionResult> GetDeliveries()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return Ok(await _service.GetDeliveriesAsync());
    }

    [HttpGet("/api/farmer/advisories")]
    public async Task<IActionResult> GetAdvisories()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return Ok(await _service.GetAdvisoriesAsync());
    }

    [HttpGet("/api/farmer/crops")]
    public IActionResult GetCrops()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return Ok(_service.GetCrops());
    }

    [HttpPost("/api/farmer/crops")]
    public async Task<IActionResult> CreateCrop([FromBody] CropCreateRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        if (string.IsNullOrWhiteSpace(req.CropName) || req.QuantityKg <= 0)
            return BadRequest(new { success = false, message = "Valid crop name and quantity are required." });
        var crop = await _service.PublishCropAsync(req);
        return Ok(new { success = true, id = crop.Id });
    }

    [HttpPut("/api/farmer/crops/{id:int}/sold")]
    public async Task<IActionResult> MarkSold(int id)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return await _service.MarkCropSoldAsync(id) ? Ok(new { success = true }) : NotFound();
    }

    [HttpDelete("/api/farmer/crops/{id:int}")]
    public async Task<IActionResult> DeleteCrop(int id)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Farmer")) return Unauth();
        return await _service.DeleteCropAsync(id) ? Ok(new { success = true }) : NotFound();
    }

    [HttpGet("/api/farmer/internal/requests")]
    public IActionResult InternalGetRequests()
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.GetRawSupplyRequests());
    }

    [HttpPut("/api/farmer/internal/requests/{id:int}/status")]
    public IActionResult InternalUpdateStatus(int id, [FromBody] SupplyStatusUpdate req)
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        var updated = _service.UpdateSupplyStatusInternal(id, req);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpGet("/api/farmer/internal/crops")]
    public IActionResult InternalGetCrops()
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.GetCrops());
    }

    [HttpPost("/api/farmer/internal/profiles")]
    public IActionResult InternalAddProfile([FromBody] FarmerRegisterRequest req)
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.AddProfileInternal(req));
    }
}

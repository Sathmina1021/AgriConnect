using AgriConnect.Shared.Models;
using AgriConnect.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using OfficerService.Services;

namespace OfficerService.Controllers;

[ApiController]
public class OfficerController : ControllerBase
{
    private readonly OfficerBusinessService _service;

    public OfficerController(OfficerBusinessService service) => _service = service;

    private IActionResult Unauth() => StatusCode(401, new { success = false, message = "Please log in to continue." });

    [HttpGet("/health")]
    public IActionResult Health() => Ok(_service.GetHealth());

    [HttpGet("/api/officer/workflows")]
    public async Task<IActionResult> GetWorkflows()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Officer")) return Unauth();
        return Ok(await _service.GetWorkflowsAsync());
    }

    [HttpGet("/api/officer/verifications")]
    public IActionResult GetVerifications()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Officer")) return Unauth();
        return Ok(_service.GetVerifications());
    }

    [HttpPost("/api/officer/verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out var user, "Officer")) return Unauth();
        var ok = await _service.VerifyRequestAsync(req, user?.FullName ?? "Agriculture Officer");
        return ok ? Ok(new { success = true }) : NotFound(new { success = false, message = "Supply request not found." });
    }

    [HttpGet("/api/officer/advisories")]
    public IActionResult GetAdvisories()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Officer")) return Unauth();
        return Ok(_service.GetAdvisories());
    }

    [HttpPost("/api/officer/advisories")]
    public async Task<IActionResult> CreateAdvisory([FromBody] AdvisoryCreateRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out var user, "Officer")) return Unauth();
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { success = false, message = "Title and message are required." });

        var advisory = await _service.PublishAdvisoryAsync(req, user?.FullName ?? "Agriculture Officer");
        return Ok(new { success = true, id = advisory.Id });
    }

    [HttpDelete("/api/officer/advisories/{id:int}")]
    public async Task<IActionResult> DeleteAdvisory(int id)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Officer")) return Unauth();
        return await _service.DeleteAdvisoryAsync(id) ? Ok(new { success = true }) : NotFound();
    }

    [HttpGet("/api/officer/internal/advisories")]
    public IActionResult InternalGetAdvisories()
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        return Ok(_service.GetAdvisories());
    }
}

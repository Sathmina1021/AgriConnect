using AdminService.Services;
using AgriConnect.Shared.Models;
using AgriConnect.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

[ApiController]
public class AdminController : ControllerBase
{
    private readonly AdminBusinessService _service;

    public AdminController(AdminBusinessService service) => _service = service;

    private IActionResult Unauth() => StatusCode(401, new { success = false, message = "Please log in to continue." });

    [HttpGet("/health")]
    public IActionResult Health() => Ok(_service.GetHealth());

    [HttpGet("/api/admin/summary")]
    public async Task<IActionResult> Summary()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Admin")) return Unauth();
        return Ok(await _service.GetPlatformSummaryAsync());
    }

    [HttpGet("/api/admin/users")]
    public IActionResult GetUsers()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Admin")) return Unauth();
        return Ok(_service.GetUsers());
    }

    [HttpPost("/api/admin/users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminUserCreateRequest req)
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Admin")) return Unauth();
        if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { success = false, message = "Full name and email are required." });

        var created = await _service.CreateUserByAdminAsync(req);
        return Ok(new { success = true, id = created.Id, username = created.Username, password = created.Password });
    }

    [HttpPut("/api/admin/users/{id:int}/toggle")]
    public IActionResult ToggleUser(int id)
    {
        if (!TokenHelper.Authorize(HttpContext, out var current, "Admin")) return Unauth();
        var (ok, code, msg, active) = _service.ToggleUser(id, current?.UserId ?? 0);
        if (!ok) return StatusCode(code, new { success = false, message = msg });
        return Ok(new { success = true, active });
    }

    [HttpDelete("/api/admin/users/{id:int}")]
    public IActionResult DeleteUser(int id)
    {
        if (!TokenHelper.Authorize(HttpContext, out var current, "Admin")) return Unauth();
        var (ok, code, msg) = _service.DeleteUser(id, current?.UserId ?? 0);
        if (!ok) return StatusCode(code, new { success = false, message = msg });
        return Ok(new { success = true });
    }

    [HttpGet("/api/admin/logs")]
    public IActionResult GetLogs()
    {
        if (!TokenHelper.Authorize(HttpContext, out _, "Admin")) return Unauth();
        return Ok(_service.GetLogs());
    }

    [HttpPost("/api/admin/internal/logs")]
    public IActionResult InternalAddLog([FromBody] AuditLogCreateRequest req)
    {
        if (!TokenHelper.IsInternalCall(HttpContext)) return Forbid();
        _service.RecordLog(req);
        return Ok(new { success = true });
    }
}

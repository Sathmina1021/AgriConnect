using AdminService.Services;
using AgriConnect.Shared.Models;
using AgriConnect.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AdminBusinessService _service;

    public AuthController(AdminBusinessService service) => _service = service;

    private void SetAuthCookie(UserAccount user)
    {
        var session = new UserSession(user.Id, user.Username, user.FullName, user.Role, user.District, user.Email);
        var token = TokenHelper.CreateToken(session);
        Response.Cookies.Append(TokenHelper.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Path = "/",
            SameSite = SameSiteMode.Lax
        });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { success = false, message = "Username and password are required." });

        var (ok, code, msg, user) = _service.Login(req);
        if (!ok || user is null) return StatusCode(code, new { success = false, message = msg });

        SetAuthCookie(user);
        return Ok(new { success = true, username = user.Username, role = user.Role, fullName = user.FullName, district = user.District });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { success = false, message = "Full name, username, and password are required." });

        var (ok, msg, user) = await _service.RegisterSelfAsync(req);
        if (!ok || user is null) return BadRequest(new { success = false, message = msg });

        SetAuthCookie(user);
        return Ok(new { success = true, username = user.Username, role = user.Role, fullName = user.FullName });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var session = TokenHelper.GetCurrentUser(HttpContext);
        if (session is null) return StatusCode(401, new { success = false, message = "Please log in to continue." });
        return Ok(new { id = session.UserId, username = session.Username, role = session.Role, fullName = session.FullName, district = session.District, email = session.Email });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(TokenHelper.CookieName);
        return Ok(new { success = true });
    }
}

namespace AgriConnect.Shared.Models;

public class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string District { get; set; } = "Colombo";
    public bool Active { get; set; } = true;
}

public record UserSession(int UserId, string Username, string FullName, string Role, string District, string Email);
public record LoginRequest(string Username, string Password);
public record RegisterRequest(string FullName, string Username, string Password, string Email, string Role, string District, string? Phone);

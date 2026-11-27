namespace AgriConnect.Shared.Models;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime LogTime { get; set; }
    public string ServiceName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
}

public record AuditLogCreateRequest(string ServiceName, string Action, string Details);
public record AdminUserCreateRequest(string FullName, string Email, string Role, string District, bool Active, string? Username = null, string? Password = null);

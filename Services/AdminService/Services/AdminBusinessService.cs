using System.Text.Json;
using AdminService.Data;
using AgriConnect.Shared.Clients;
using AgriConnect.Shared.Models;

namespace AdminService.Services;

public class AdminBusinessService
{
    private readonly AdminRepository _repo;
    private readonly ServiceClientHelper _client;

    public AdminBusinessService(AdminRepository repo, ServiceClientHelper client)
    {
        _repo = repo;
        _client = client;
    }

    public object GetHealth() => new
    {
        status = "Running",
        database = "Services/AdminService/Data/admin_db.json",
        users = _repo.GetUsers().Count,
        auditLogs = _repo.GetLogCount()
    };

    public (bool Ok, int StatusCode, string? Message, UserAccount? User) Login(LoginRequest req)
    {
        var user = _repo.FindByCredentials(req.Username, req.Password);
        if (user is null) return (false, 401, "Invalid username or password.", null);
        if (!user.Active) return (false, 403, "This account has been disabled by an administrator.", null);

        _repo.AddLog("AuthService", "Login", $"{user.FullName} ({user.Role}) logged in.");
        return (true, 200, null, user);
    }

    public async Task<(bool Ok, string? Message, UserAccount? User)> RegisterSelfAsync(RegisterRequest req)
    {
        var allowedRoles = new[] { "Farmer", "Supplier", "Officer" };
        if (!allowedRoles.Contains(req.Role))
            return (false, "Role must be Farmer, Supplier, or Officer.", null);

        var uname = req.Username.Trim().ToLowerInvariant();
        if (_repo.UsernameExists(uname))
            return (false, "Username is already taken.", null);

        var account = _repo.AddUser(new UserAccount
        {
            Username = uname,
            Password = req.Password,
            FullName = req.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? $"{uname}@agriconnect.lk" : req.Email.Trim(),
            Role = req.Role,
            District = string.IsNullOrWhiteSpace(req.District) ? "Colombo" : req.District.Trim(),
            Active = true
        });

        if (account.Role == "Farmer")
        {
            await _client.PostAsync($"{ServiceEndpoints.Farmer}/api/farmer/internal/profiles",
                new FarmerRegisterRequest(account.FullName, $"{account.FullName} Farm", account.District,
                    string.IsNullOrWhiteSpace(req.Phone) ? "0770000000" : req.Phone.Trim(), 2.0));
        }

        _repo.AddLog("AuthService", "Register", $"New {account.Role} account registered: {account.FullName} ({account.Username}).");
        return (true, null, account);
    }

    public async Task<object> GetPlatformSummaryAsync()
    {
        var fHealth = await _client.GetAsync<JsonElement?>($"{ServiceEndpoints.Farmer}/health");
        var sHealth = await _client.GetAsync<JsonElement?>($"{ServiceEndpoints.Supplier}/health");
        var oHealth = await _client.GetAsync<JsonElement?>($"{ServiceEndpoints.Officer}/health");

        static int ReadInt(JsonElement? el, string prop) =>
            el.HasValue && el.Value.TryGetProperty(prop, out var val) && val.TryGetInt32(out var num) ? num : 0;

        return new
        {
            totalUsers = _repo.GetUsers().Count,
            requests = ReadInt(fHealth, "requests"),
            crops = ReadInt(fHealth, "crops"),
            quotes = ReadInt(sHealth, "quotations"),
            deliveries = ReadInt(sHealth, "deliveries"),
            inventory = ReadInt(sHealth, "inventory"),
            advisories = ReadInt(oHealth, "advisories"),
            services = new
            {
                farmer = fHealth.HasValue ? "Running" : "Offline",
                supplier = sHealth.HasValue ? "Running" : "Offline",
                officer = oHealth.HasValue ? "Running" : "Offline",
                admin = "Running"
            }
        };
    }

    public List<object> GetUsers() => _repo.GetUsers().Select(u => (object)new
    {
        u.Id,
        u.Username,
        u.FullName,
        u.Email,
        u.Role,
        u.District,
        u.Active
    }).ToList();

    public async Task<UserAccount> CreateUserByAdminAsync(AdminUserCreateRequest req)
    {
        var baseName = string.IsNullOrWhiteSpace(req.Username)
            ? req.FullName.Trim().ToLowerInvariant().Replace(" ", ".")
            : req.Username.Trim().ToLowerInvariant();

        var username = baseName;
        var suffix = 1;
        while (_repo.UsernameExists(username))
            username = $"{baseName}{suffix++}";

        var password = string.IsNullOrWhiteSpace(req.Password) ? "changeme123" : req.Password;
        var account = _repo.AddUser(new UserAccount
        {
            Username = username,
            Password = password,
            FullName = req.FullName.Trim(),
            Email = req.Email.Trim(),
            Role = req.Role,
            District = req.District.Trim(),
            Active = req.Active
        });

        if (account.Role == "Farmer")
        {
            await _client.PostAsync($"{ServiceEndpoints.Farmer}/api/farmer/internal/profiles",
                new FarmerRegisterRequest(account.FullName, $"{account.FullName} Farm", account.District, "0770000000", 2.0));
        }

        _repo.AddLog("AdminService", "UserCreated", $"Account created for {account.FullName} ({account.Role}) with username '{account.Username}'.");
        return account;
    }

    public (bool Ok, int Code, string? Message, bool Active) ToggleUser(int id, int currentUserId)
    {
        if (id == currentUserId) return (false, 400, "You cannot disable your own admin account.", false);
        var user = _repo.ToggleUser(id);
        if (user is null) return (false, 404, "User not found.", false);
        _repo.AddLog("AdminService", "UserToggled", $"{user.FullName} is now {(user.Active ? "Enabled" : "Disabled")}.");
        return (true, 200, null, user.Active);
    }

    public (bool Ok, int Code, string? Message) DeleteUser(int id, int currentUserId)
    {
        if (id == currentUserId) return (false, 400, "You cannot delete your own admin account.");
        var user = _repo.DeleteUser(id);
        if (user is null) return (false, 404, "User not found.");
        _repo.AddLog("AdminService", "UserDeleted", $"Account '{user.Username}' ({user.FullName}) deleted.");
        return (true, 200, null);
    }

    public List<AuditLog> GetLogs() => _repo.GetLogs();

    public void RecordLog(AuditLogCreateRequest req) => _repo.AddLog(req.ServiceName, req.Action, req.Details);
}

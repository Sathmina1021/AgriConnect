using AgriConnect.Shared.Models;
using AgriConnect.Shared.Storage;

namespace AdminService.Data;

public record AdminDbSnapshot(
    List<UserAccount> Users,
    List<AuditLog> AuditLogs);

public class AdminRepository
{
    private readonly JsonFileStore<AdminDbSnapshot> _store;
    private readonly object _sync = new();

    private List<UserAccount> _users;
    private List<AuditLog> _logs;

    public AdminRepository(string dbPath)
    {
        _store = new JsonFileStore<AdminDbSnapshot>(dbPath);
        var data = _store.LoadOrCreate(SeedData);
        _users = data.Users ?? new();
        _logs = data.AuditLogs ?? new();
    }

    private static AdminDbSnapshot SeedData() => new(
        new List<UserAccount>
        {
            new() { Id = 1, Username = "admin", Password = "admin123", FullName = "D.M. Sathmina Mihikalpa", Email = "admin@agriconnect.lk", Role = "Admin", District = "Colombo", Active = true },
            new() { Id = 2, Username = "farmer1", Password = "farmer123", FullName = "Saman Perera", Email = "saman.perera@agriconnect.lk", Role = "Farmer", District = "Homagama", Active = true },
            new() { Id = 3, Username = "supplier1", Password = "supplier123", FullName = "Lanka Agro Supplies", Email = "contact@lankaagro.lk", Role = "Supplier", District = "Kurunegala", Active = true },
            new() { Id = 4, Username = "officer1", Password = "officer123", FullName = "Dr. Chaminda Silva", Email = "c.silva@agridept.gov.lk", Role = "Officer", District = "Gampaha", Active = true }
        },
        new List<AuditLog>
        {
            new() { Id = 1, LogTime = DateTime.Now, ServiceName = "SystemService", Action = "Startup", Details = "AgriConnect platform initialized with seed data." }
        });

    private void Persist() => _store.Save(new AdminDbSnapshot(_users, _logs));

    public List<UserAccount> GetUsers() { lock (_sync) return _users.ToList(); }

    public UserAccount? FindByCredentials(string username, string password)
    {
        lock (_sync)
        {
            return _users.FirstOrDefault(u =>
                string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
        }
    }

    public bool UsernameExists(string username)
    {
        lock (_sync) return _users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public UserAccount AddUser(UserAccount user)
    {
        lock (_sync)
        {
            user.Id = _users.Count > 0 ? _users.Max(x => x.Id) + 1 : 1;
            _users.Add(user);
            Persist();
            return user;
        }
    }

    public UserAccount? ToggleUser(int id)
    {
        lock (_sync)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user is null) return null;
            user.Active = !user.Active;
            Persist();
            return user;
        }
    }

    public UserAccount? DeleteUser(int id)
    {
        lock (_sync)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user is null) return null;
            _users.Remove(user);
            Persist();
            return user;
        }
    }

    public List<AuditLog> GetLogs(int take = 100) { lock (_sync) return _logs.Take(take).ToList(); }

    public int GetLogCount() { lock (_sync) return _logs.Count; }

    public void AddLog(string serviceName, string action, string details)
    {
        lock (_sync)
        {
            var nextId = _logs.Count > 0 ? _logs.Max(x => x.Id) + 1 : 1;
            _logs.Insert(0, new AuditLog
            {
                Id = nextId,
                LogTime = DateTime.Now,
                ServiceName = serviceName,
                Action = action,
                Details = details
            });
            Persist();
        }
    }
}

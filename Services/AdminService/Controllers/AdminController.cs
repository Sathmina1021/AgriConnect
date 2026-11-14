using System.Text.Json;
using AdminService.Data;
using AdminService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AdminContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _config;

        public AdminController(AdminContext context, IHttpClientFactory clientFactory, IConfiguration config)
        {
            _context = context;
            _clientFactory = clientFactory;
            _config = config;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            return Ok(await _context.UserAccounts.OrderByDescending(u => u.Id).ToListAsync());
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] UserAccount user)
        {
            user.CreatedDate = DateTime.Now;
            _context.UserAccounts.Add(user);
            _context.SystemLogs.Add(new SystemLog
            {
                ServiceName = "AdminService",
                Action = "UserCreated",
                Details = $"Created {user.Role} account for {user.FullName}"
            });
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        [HttpPut("users/{id}/toggle")]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            user.Active = !user.Active;
            _context.SystemLogs.Add(new SystemLog
            {
                ServiceName = "AdminService",
                Action = "UserStatusChanged",
                Details = $"{user.FullName} set to {(user.Active ? "Active" : "Inactive")}"
            });
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            return Ok(await _context.SystemLogs.OrderByDescending(l => l.Id).Take(40).ToListAsync());
        }

        [HttpPost("logs")]
        public async Task<IActionResult> AddLog([FromBody] SystemLog log)
        {
            log.LogTime = DateTime.Now;
            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok(log);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var farmerUrl = _config["Endpoints:FarmerUrl"] ?? "http://localhost:5001";
            var supplierUrl = _config["Endpoints:SupplierUrl"] ?? "http://localhost:5002";
            var officerUrl = _config["Endpoints:OfficerUrl"] ?? "http://localhost:5003";
            var client = _clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(2);

            int requests = 0, crops = 0, quotes = 0;
            bool farmerOnline = false, supplierOnline = false, officerOnline = false;

            try
            {
                var r1 = await client.GetAsync($"{farmerUrl}/api/farmer/requests");
                if (r1.IsSuccessStatusCode)
                {
                    farmerOnline = true;
                    requests = JsonSerializer.Deserialize<JsonElement>(await r1.Content.ReadAsStringAsync()).GetArrayLength();
                }
                var r2 = await client.GetAsync($"{farmerUrl}/api/farmer/crops");
                if (r2.IsSuccessStatusCode)
                {
                    crops = JsonSerializer.Deserialize<JsonElement>(await r2.Content.ReadAsStringAsync()).GetArrayLength();
                }
            }
            catch { }

            try
            {
                var s1 = await client.GetAsync($"{supplierUrl}/api/supplier/quotations");
                if (s1.IsSuccessStatusCode)
                {
                    supplierOnline = true;
                    quotes = JsonSerializer.Deserialize<JsonElement>(await s1.Content.ReadAsStringAsync()).GetArrayLength();
                }
            }
            catch { }

            try
            {
                var o1 = await client.GetAsync($"{officerUrl}/api/officer/verifications");
                if (o1.IsSuccessStatusCode)
                {
                    officerOnline = true;
                }
            }
            catch { }

            return Ok(new
            {
                totalUsers = await _context.UserAccounts.CountAsync(),
                requests,
                crops,
                quotes,
                services = new
                {
                    farmer = farmerOnline ? "Running" : "Offline",
                    supplier = supplierOnline ? "Running" : "Offline",
                    officer = officerOnline ? "Running" : "Offline",
                    admin = "Running"
                }
            });
        }
    }
}

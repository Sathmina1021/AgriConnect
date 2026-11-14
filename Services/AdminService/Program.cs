using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5004");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<AdminContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AdminDb")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminContext>();
    db.Database.EnsureCreated();

    if (!db.UserAccounts.Any())
    {
        db.UserAccounts.AddRange(
            new UserAccount { FullName = "Nimal Perera", Email = "nimal@agriconnect.lk", Role = "Farmer", District = "Anuradhapura", Active = true },
            new UserAccount { FullName = "Lanka Agro Supplies", Email = "sales@lankaagro.lk", Role = "Supplier", District = "Kurunegala", Active = true },
            new UserAccount { FullName = "Dr. Chaminda Silva", Email = "chaminda@agri.gov.lk", Role = "Officer", District = "All", Active = true },
            new UserAccount { FullName = "System Admin", Email = "admin@agriconnect.lk", Role = "Admin", District = "Colombo", Active = true }
        );

        db.SystemLogs.Add(new SystemLog
        {
            ServiceName = "AdminService",
            Action = "Startup",
            Details = "AgriConnect platform services initialized."
        });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.Run();

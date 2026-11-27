using AdminService.Data;
using AdminService.Services;
using AgriConnect.Shared.Clients;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5004");

builder.Services.AddControllers();
builder.Services.AddHttpClient<ServiceClientHelper>();
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new AdminRepository(Path.Combine(env.ContentRootPath, "Data", "admin_db.json"));
});
builder.Services.AddScoped<AdminBusinessService>();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();

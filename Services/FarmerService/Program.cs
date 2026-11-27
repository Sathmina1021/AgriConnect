using AgriConnect.Shared.Clients;
using FarmerService.Data;
using FarmerService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5001");

builder.Services.AddControllers();
builder.Services.AddHttpClient<ServiceClientHelper>();
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new FarmerRepository(Path.Combine(env.ContentRootPath, "Data", "farmer_db.json"));
});
builder.Services.AddScoped<FarmerBusinessService>();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();

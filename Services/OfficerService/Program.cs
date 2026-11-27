using AgriConnect.Shared.Clients;
using OfficerService.Data;
using OfficerService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5003");

builder.Services.AddControllers();
builder.Services.AddHttpClient<ServiceClientHelper>();
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new OfficerRepository(Path.Combine(env.ContentRootPath, "Data", "officer_db.json"));
});
builder.Services.AddScoped<OfficerBusinessService>();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();

using AgriConnect.Shared.Clients;
using SupplierService.Data;
using SupplierService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5002");

builder.Services.AddControllers();
builder.Services.AddHttpClient<ServiceClientHelper>();
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new SupplierRepository(Path.Combine(env.ContentRootPath, "Data", "supplier_db.json"));
});
builder.Services.AddScoped<SupplierBusinessService>();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();

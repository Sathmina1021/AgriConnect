var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Run();

using System.Text.Json;
using AgriConnect.Shared.Clients;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddHttpClient("Gateway")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false
    });

var app = builder.Build();
var startedAt = DateTime.UtcNow;

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", async (IHttpClientFactory factory) =>
{
    var http = factory.CreateClient("Gateway");
    http.Timeout = TimeSpan.FromSeconds(3);

    async Task<object> Probe(string baseUrl, string dbFile)
    {
        try
        {
            using var res = await http.GetAsync($"{baseUrl}/health");
            if (!res.IsSuccessStatusCode)
                return new { status = "Offline", url = baseUrl, database = dbFile };

            var text = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(text);
        }
        catch
        {
            return new { status = "Offline", url = baseUrl, database = dbFile };
        }
    }

    var farmerTask = Probe(ServiceEndpoints.Farmer, "Services/FarmerService/Data/farmer_db.json");
    var supplierTask = Probe(ServiceEndpoints.Supplier, "Services/SupplierService/Data/supplier_db.json");
    var officerTask = Probe(ServiceEndpoints.Officer, "Services/OfficerService/Data/officer_db.json");
    var adminTask = Probe(ServiceEndpoints.Admin, "Services/AdminService/Data/admin_db.json");

    await Task.WhenAll(farmerTask, supplierTask, officerTask, adminTask);

    return Results.Json(new
    {
        status = "Healthy",
        uptimeSeconds = (int)(DateTime.UtcNow - startedAt).TotalSeconds,
        persistence = "Database-per-Service (Services/*/Data/*.json)",
        services = new
        {
            farmer = farmerTask.Result,
            supplier = supplierTask.Result,
            officer = officerTask.Result,
            admin = adminTask.Result
        }
    });
});

app.MapGet("/api/endpoints", () => Results.Json(new[]
{
    new { group = "Gateway", method = "GET", path = "/api/health", role = "Public", description = "Live health check across all 4 microservices" },
    new { group = "Gateway", method = "GET", path = "/api/endpoints", role = "Public", description = "List all REST API endpoints" },
    new { group = "Auth (AdminService:5004)", method = "POST", path = "/api/auth/login", role = "Public", description = "Authenticate user and issue signed session cookie" },
    new { group = "Auth (AdminService:5004)", method = "POST", path = "/api/auth/register", role = "Public", description = "Register a new Farmer, Supplier, or Officer account" },
    new { group = "Auth (AdminService:5004)", method = "GET", path = "/api/auth/me", role = "Authenticated", description = "Get signed-in user profile" },
    new { group = "Auth (AdminService:5004)", method = "POST", path = "/api/auth/logout", role = "Authenticated", description = "Clear session cookie" },
    new { group = "FarmerService (:5001)", method = "GET", path = "/api/farmer/profiles", role = "Farmer, Admin", description = "List registered farmer profiles" },
    new { group = "FarmerService (:5001)", method = "POST", path = "/api/farmer/register", role = "Farmer, Admin", description = "Register a new farmer profile" },
    new { group = "FarmerService (:5001)", method = "GET", path = "/api/farmer/requests", role = "Farmer, Admin", description = "List supply requests with quotation counts" },
    new { group = "FarmerService (:5001)", method = "POST", path = "/api/farmer/requests", role = "Farmer, Admin", description = "Submit a new supply request" },
    new { group = "FarmerService (:5001)", method = "GET", path = "/api/farmer/requests/{id}/quotations", role = "Farmer, Admin", description = "View supplier quotations for a supply request" },
    new { group = "FarmerService (:5001)", method = "POST", path = "/api/farmer/requests/{id}/confirm", role = "Farmer, Admin", description = "Confirm a quotation and trigger delivery order" },
    new { group = "FarmerService (:5001)", method = "GET", path = "/api/farmer/deliveries", role = "Farmer, Admin", description = "Track delivery statuses for confirmed orders" },
    new { group = "FarmerService (:5001)", method = "GET", path = "/api/farmer/advisories", role = "Farmer, Admin", description = "View agricultural advisories from OfficerService" },
    new { group = "FarmerService (:5001)", method = "GET", path = "/api/farmer/crops", role = "Farmer, Admin", description = "List harvested crop marketplace listings" },
    new { group = "FarmerService (:5001)", method = "POST", path = "/api/farmer/crops", role = "Farmer, Admin", description = "Publish a harvested crop listing" },
    new { group = "FarmerService (:5001)", method = "PUT", path = "/api/farmer/crops/{id}/sold", role = "Farmer, Admin", description = "Mark a crop listing as Sold" },
    new { group = "FarmerService (:5001)", method = "DELETE", path = "/api/farmer/crops/{id}", role = "Farmer, Admin", description = "Remove a crop listing" },
    new { group = "SupplierService (:5002)", method = "GET", path = "/api/supplier/inventory", role = "Supplier, Admin", description = "List warehouse inventory items" },
    new { group = "SupplierService (:5002)", method = "POST", path = "/api/supplier/inventory", role = "Supplier, Admin", description = "Add new inventory stock" },
    new { group = "SupplierService (:5002)", method = "PUT", path = "/api/supplier/inventory/{id}", role = "Supplier, Admin", description = "Update stock quantity and unit price" },
    new { group = "SupplierService (:5002)", method = "DELETE", path = "/api/supplier/inventory/{id}", role = "Supplier, Admin", description = "Delete an inventory item" },
    new { group = "SupplierService (:5002)", method = "GET", path = "/api/supplier/farmer-requests", role = "Supplier, Admin", description = "List open supply requests from FarmerService" },
    new { group = "SupplierService (:5002)", method = "GET", path = "/api/supplier/quotations", role = "Supplier, Admin", description = "List submitted quotations" },
    new { group = "SupplierService (:5002)", method = "POST", path = "/api/supplier/quotations", role = "Supplier, Admin", description = "Submit a quotation for a supply request" },
    new { group = "SupplierService (:5002)", method = "GET", path = "/api/supplier/deliveries", role = "Supplier, Admin", description = "List confirmed order deliveries" },
    new { group = "SupplierService (:5002)", method = "PUT", path = "/api/supplier/deliveries/{id}/status", role = "Supplier, Admin", description = "Update delivery status" },
    new { group = "SupplierService (:5002)", method = "GET", path = "/api/supplier/crops", role = "Supplier, Admin", description = "Browse available farmer crops" },
    new { group = "OfficerService (:5003)", method = "GET", path = "/api/officer/workflows", role = "Officer, Admin", description = "Aggregate requests and quotations for review" },
    new { group = "OfficerService (:5003)", method = "GET", path = "/api/officer/verifications", role = "Officer, Admin", description = "List verification history" },
    new { group = "OfficerService (:5003)", method = "POST", path = "/api/officer/verify", role = "Officer, Admin", description = "Approve or reject a supply request" },
    new { group = "OfficerService (:5003)", method = "GET", path = "/api/officer/advisories", role = "Officer, Admin", description = "List published agricultural advisories" },
    new { group = "OfficerService (:5003)", method = "POST", path = "/api/officer/advisories", role = "Officer, Admin", description = "Publish a new agricultural advisory" },
    new { group = "OfficerService (:5003)", method = "DELETE", path = "/api/officer/advisories/{id}", role = "Officer, Admin", description = "Delete an agricultural advisory" },
    new { group = "AdminService (:5004)", method = "GET", path = "/api/admin/summary", role = "Admin", description = "Aggregate platform KPIs across all services" },
    new { group = "AdminService (:5004)", method = "GET", path = "/api/admin/users", role = "Admin", description = "List platform user accounts" },
    new { group = "AdminService (:5004)", method = "POST", path = "/api/admin/users", role = "Admin", description = "Create a new platform user account" },
    new { group = "AdminService (:5004)", method = "PUT", path = "/api/admin/users/{id}/toggle", role = "Admin", description = "Enable or disable a user account" },
    new { group = "AdminService (:5004)", method = "DELETE", path = "/api/admin/users/{id}", role = "Admin", description = "Delete a user account" },
    new { group = "AdminService (:5004)", method = "GET", path = "/api/admin/logs", role = "Admin", description = "Retrieve system-wide audit logs" }
}));

app.Map("/api/{**rest}", async (HttpContext ctx, IHttpClientFactory factory) =>
{
    var path = ctx.Request.Path.Value ?? "";
    string? targetBase = path switch
    {
        var p when p.StartsWith("/api/farmer", StringComparison.OrdinalIgnoreCase) => ServiceEndpoints.Farmer,
        var p when p.StartsWith("/api/supplier", StringComparison.OrdinalIgnoreCase) => ServiceEndpoints.Supplier,
        var p when p.StartsWith("/api/officer", StringComparison.OrdinalIgnoreCase) => ServiceEndpoints.Officer,
        var p when p.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) => ServiceEndpoints.Admin,
        var p when p.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) => ServiceEndpoints.Admin,
        _ => null
    };

    if (targetBase is null)
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsJsonAsync(new { success = false, message = "Unknown API route." });
        return;
    }

    var targetUrl = $"{targetBase}{path}{ctx.Request.QueryString}";
    var http = factory.CreateClient("Gateway");

    using var forwardReq = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), targetUrl);

    if (ctx.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
        forwardReq.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToArray());

    if (!HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method) && !HttpMethods.IsDelete(ctx.Request.Method))
    {
        var ms = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(ms);
        ms.Position = 0;
        forwardReq.Content = new StreamContent(ms);
        if (!string.IsNullOrEmpty(ctx.Request.ContentType))
            forwardReq.Content.Headers.TryAddWithoutValidation("Content-Type", ctx.Request.ContentType);
    }

    try
    {
        using var forwardRes = await http.SendAsync(forwardReq, HttpCompletionOption.ResponseHeadersRead);
        ctx.Response.StatusCode = (int)forwardRes.StatusCode;

        if (forwardRes.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var c in setCookies)
                ctx.Response.Headers.Append("Set-Cookie", c);
        }

        if (forwardRes.Content.Headers.ContentType is not null)
            ctx.Response.ContentType = forwardRes.Content.Headers.ContentType.ToString();

        await forwardRes.Content.CopyToAsync(ctx.Response.Body);
    }
    catch
    {
        ctx.Response.StatusCode = 503;
        await ctx.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = $"Target service ({targetBase}) is currently offline. Make sure all 4 services are running."
        });
    }
});

app.Run();

using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Shared.Models;
using AgriConnect.Shared.Security;

namespace AgriConnect.Shared.Clients;

public static class ServiceEndpoints
{
    public const string Farmer = "http://localhost:5001";
    public const string Supplier = "http://localhost:5002";
    public const string Officer = "http://localhost:5003";
    public const string Admin = "http://localhost:5004";
}

public class ServiceClientHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public ServiceClientHelper(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add(TokenHelper.InternalHeader, TokenHelper.InternalKey);
            using var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode) return default;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch
        {
            return default;
        }
    }

    public async Task<bool> PostAsync<TBody>(string url, TBody body)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Add(TokenHelper.InternalHeader, TokenHelper.InternalKey);
            using var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PutAsync<TBody>(string url, TBody body)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Add(TokenHelper.InternalHeader, TokenHelper.InternalKey);
            using var res = await _http.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task LogAuditAsync(string serviceName, string action, string details)
    {
        return PostAsync($"{ServiceEndpoints.Admin}/api/admin/internal/logs",
            new AuditLogCreateRequest(serviceName, action, details));
    }
}

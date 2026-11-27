using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgriConnect.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace AgriConnect.Shared.Security;

public static class TokenHelper
{
    public const string CookieName = "ac_session";
    public const string InternalHeader = "X-AgriConnect-Internal";
    public const string InternalKey = "ac-soc-internal-2026";
    private static readonly byte[] SigningKey = Encoding.UTF8.GetBytes("AgriConnect_SOC_CSE5203_SigningKey_2026!");

    public static string CreateToken(UserSession session)
    {
        var json = JsonSerializer.Serialize(session);
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var sig = ComputeSignature(payload);
        return $"{payload}.{sig}";
    }

    public static UserSession? ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var parts = token.Split('.');
        if (parts.Length != 2) return null;

        var expected = ComputeSignature(parts[0]);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[1])))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            return JsonSerializer.Deserialize<UserSession>(json);
        }
        catch
        {
            return null;
        }
    }

    public static UserSession? GetCurrentUser(HttpContext ctx)
    {
        if (!ctx.Request.Cookies.TryGetValue(CookieName, out var token))
            return null;
        return ValidateToken(token);
    }

    public static bool IsInternalCall(HttpContext ctx)
    {
        return ctx.Request.Headers.TryGetValue(InternalHeader, out var val) && val == InternalKey;
    }

    public static bool Authorize(HttpContext ctx, out UserSession? user, params string[] allowedRoles)
    {
        user = GetCurrentUser(ctx);
        if (user is null) return false;
        if (allowedRoles.Length == 0) return true;
        return allowedRoles.Contains(user.Role) || user.Role == "Admin";
    }

    private static string ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(SigningKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}

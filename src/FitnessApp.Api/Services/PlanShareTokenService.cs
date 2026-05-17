using System.Security.Cryptography;
using System.Text;

namespace FitnessApp.Api.Services;

public class PlanShareTokenService
{
    private readonly byte[] _secret;
    private const int DefaultTtlHours = 24;

    public PlanShareTokenService(IConfiguration config)
    {
        var key = config["Jwt:SigningKey"]
                  ?? throw new InvalidOperationException("Jwt:SigningKey is required for share tokens.");
        _secret = Encoding.UTF8.GetBytes(key);
    }

    public string Create(int planId, int? ttlHours = null)
    {
        var exp = DateTimeOffset.UtcNow.AddHours(ttlHours ?? DefaultTtlHours).ToUnixTimeSeconds();
        var payload = $"{planId}|{exp}";
        var sig = Sign(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(payload)) + "." + sig;
    }

    public string CreateForKind(string kind, int planId, int? ttlHours = null)
    {
        var exp = DateTimeOffset.UtcNow.AddHours(ttlHours ?? DefaultTtlHours).ToUnixTimeSeconds();
        var payload = $"{kind}|{planId}|{exp}";
        var sig = Sign(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(payload)) + "." + sig;
    }

    public bool TryValidate(string token, out int planId)
    {
        planId = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;
        var parts = token.Split('.', 2);
        if (parts.Length != 2) return false;

        byte[] payloadBytes;
        try { payloadBytes = Base64UrlDecode(parts[0]); }
        catch { return false; }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var expected = Sign(payload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[1])))
            return false;

        var bits = payload.Split('|');
        if (bits.Length != 2) return false;
        if (!int.TryParse(bits[0], out planId)) return false;
        if (!long.TryParse(bits[1], out var exp)) return false;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;

        return true;
    }

    public bool TryValidateForKind(string expectedKind, string token, out int planId)
    {
        planId = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;
        var parts = token.Split('.', 2);
        if (parts.Length != 2) return false;

        byte[] payloadBytes;
        try { payloadBytes = Base64UrlDecode(parts[0]); }
        catch { return false; }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var expected = Sign(payload);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[1])))
            return false;

        var bits = payload.Split('|');
        if (bits.Length != 3) return false;
        if (!string.Equals(bits[0], expectedKind, StringComparison.Ordinal)) return false;
        if (!int.TryParse(bits[1], out planId)) return false;
        if (!long.TryParse(bits[2], out var exp)) return false;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;

        return true;
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(_secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}

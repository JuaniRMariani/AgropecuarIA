using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AgropecuarIA.StorageRecoverySpike;

internal sealed record GrantPayload(
    string Purpose,
    Guid FileId,
    int Version,
    string TenantRef,
    long ExpiresUnixSeconds,
    string Nonce);

internal sealed class TokenProtector
{
    private readonly byte[] secret;

    public TokenProtector(ReadOnlySpan<byte> secret)
    {
        if (secret.Length < 32)
        {
            throw new ArgumentException("At least 256 bits of ephemeral signing material are required.", nameof(secret));
        }

        this.secret = secret.ToArray();
    }

    public string Protect(GrantPayload payload)
    {
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signature = HMACSHA256.HashData(secret, Encoding.ASCII.GetBytes(encodedPayload));
        return $"{encodedPayload}.{Base64UrlEncode(signature)}";
    }

    public bool TryUnprotect(string token, out GrantPayload? payload)
    {
        payload = null;
        var segments = token.Split('.', 2, StringSplitOptions.None);
        if (segments.Length != 2 || !TryBase64UrlDecode(segments[1], out var suppliedSignature))
        {
            return false;
        }

        var expectedSignature = HMACSHA256.HashData(secret, Encoding.ASCII.GetBytes(segments[0]));
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature))
        {
            return false;
        }

        if (!TryBase64UrlDecode(segments[0], out var payloadBytes))
        {
            return false;
        }

        try
        {
            payload = JsonSerializer.Deserialize<GrantPayload>(payloadBytes);
            return payload is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryBase64UrlDecode(string value, out byte[] bytes)
    {
        try
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
            bytes = Convert.FromBase64String(base64);
            // Reject padded and alternate encodings whose unused trailing bits decode
            // to the same bytes. Tokens have one canonical representation only.
            return string.Equals(Base64UrlEncode(bytes), value, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}

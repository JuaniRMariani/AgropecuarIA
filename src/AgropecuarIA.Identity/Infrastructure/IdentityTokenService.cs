using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace AgropecuarIA.Identity.Infrastructure;

public sealed class IdentityTokenService
{
    public string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static byte[] HashToken(string token) =>
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
}

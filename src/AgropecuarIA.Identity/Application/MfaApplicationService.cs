using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Identity.Application;

public sealed record SetupTotpResult(string SharedKey, string AuthenticatorUri);
public sealed record EnableTotpCommand(string Code);
public sealed record EnableTotpResult(string[] RecoveryCodes);
public sealed record ConsumeRecoveryCodeCommand(string RecoveryCode);

public static class MfaErrors
{
    public static IdentityOperationException TotpAlreadyEnabled() =>
        new("mfa.totp.already_enabled", 409, "TOTP is already enabled.");
    public static IdentityOperationException TotpNotEnabled() =>
        new("mfa.totp.not_enabled", 409, "TOTP is not enabled.");
    public static IdentityOperationException InvalidCode() =>
        new("mfa.code.invalid", 400, "The provided code is invalid.");
    public static IdentityOperationException InvalidRecoveryCode() =>
        new("mfa.recovery_code.invalid", 400, "The provided recovery code is invalid or has already been used.");
}

public sealed class MfaApplicationService
{
    private readonly IdentityDbContext _dbContext;
    private readonly IDataProtector _dataProtector;
    private readonly TimeProvider _timeProvider;

    public MfaApplicationService(
        IdentityDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _dataProtector = dataProtectionProvider.CreateProtector("AgropecuarIA.Identity.TOTP");
        _timeProvider = timeProvider;
    }

    public async Task<SetupTotpResult> SetupTotpAsync(
        AuthenticatedSession session,
        CancellationToken cancellationToken)
    {
        bool hasTotp = await _dbContext.TotpCredentials
            .AnyAsync(c => c.UserId == session.UserId, cancellationToken);
        if (hasTotp)
        {
            throw MfaErrors.TotpAlreadyEnabled();
        }

        byte[] secretKey = new byte[20];
        RandomNumberGenerator.Fill(secretKey);
        string base32Secret = Base32Encode(secretKey);

        string issuer = Uri.EscapeDataString("AgropecuarIA");
        string account = Uri.EscapeDataString(session.UserId.ToString());
        string uri = "otpauth://totp/" + issuer + ":" + account + "?secret=" + base32Secret + "&issuer=" + issuer + "&digits=6";

        return new SetupTotpResult(base32Secret, uri);
    }

    public async Task<EnableTotpResult> EnableTotpAsync(
        EnableTotpCommand command,
        string unverifiedSecret,
        AuthenticatedSession session,
        CancellationToken cancellationToken)
    {
        bool hasTotp = await _dbContext.TotpCredentials
            .AnyAsync(c => c.UserId == session.UserId, cancellationToken);
        if (hasTotp)
        {
            throw MfaErrors.TotpAlreadyEnabled();
        }

        if (!ValidateTotp(unverifiedSecret, command.Code))
        {
            throw MfaErrors.InvalidCode();
        }

        string protectedSecret = _dataProtector.Protect(unverifiedSecret);
        var credential = new UserTotpCredential(session.UserId, protectedSecret);
        _dbContext.TotpCredentials.Add(credential);

        string[] rawCodes = new string[10];
        for (int i = 0; i < 10; i++)
        {
            rawCodes[i] = GenerateRecoveryCode();
            string hashedCode = HashRecoveryCode(rawCodes[i]);
            _dbContext.RecoveryCodes.Add(new UserRecoveryCode(session.UserId, hashedCode));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new EnableTotpResult(rawCodes);
    }

    public async Task DisableTotpAsync(
        AuthenticatedSession session,
        CancellationToken cancellationToken)
    {
        var credential = await _dbContext.TotpCredentials
            .FirstOrDefaultAsync(c => c.UserId == session.UserId, cancellationToken);
        if (credential is null)
        {
            throw MfaErrors.TotpNotEnabled();
        }

        _dbContext.TotpCredentials.Remove(credential);

        var codes = await _dbContext.RecoveryCodes
            .Where(c => c.UserId == session.UserId)
            .ToListAsync(cancellationToken);
        _dbContext.RecoveryCodes.RemoveRange(codes);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConsumeRecoveryCodeAsync(
        ConsumeRecoveryCodeCommand command,
        AuthenticatedSession session,
        CancellationToken cancellationToken)
    {
        string hashedCode = HashRecoveryCode(command.RecoveryCode);

        var codeEntity = await _dbContext.RecoveryCodes
            .FirstOrDefaultAsync(c => c.UserId == session.UserId && c.CodeHash == hashedCode, cancellationToken);

        if (codeEntity is null || codeEntity.UsedAtUtc.HasValue)
        {
            throw MfaErrors.InvalidRecoveryCode();
        }

        codeEntity.MarkAsUsed(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private bool ValidateTotp(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !int.TryParse(code, out _))
            return false;

        byte[] secretBytes = Base32Decode(secret);
        long unixTime = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        long timeWindow = unixTime / 30;

        for (long i = -1; i <= 1; i++)
        {
            if (GenerateTotpCode(secretBytes, timeWindow + i) == code)
            {
                return true;
            }
        }
        return false;
    }

    private static string GenerateTotpCode(byte[] secret, long iteration)
    {
        byte[] counter = BitConverter.GetBytes(iteration);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        #pragma warning disable CA5350
        using var hmac = new HMACSHA1(secret);
#pragma warning restore CA5350
        byte[] hash = hmac.ComputeHash(counter);
        int offset = hash[hash.Length - 1] & 0x0F;

        int binaryCode = ((hash[offset] & 0x7f) << 24)
                       | ((hash[offset + 1] & 0xff) << 16)
                       | ((hash[offset + 2] & 0xff) << 8)
                       | (hash[offset + 3] & 0xff);

        int code = binaryCode % 1000000;
        return code.ToString("D6", CultureInfo.InvariantCulture);
    }

    private static string GenerateRecoveryCode()
    {
        byte[] bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        string code = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"{code.Substring(0, 4)}-{code.Substring(4, 4)}-{code.Substring(8, 4)}-{code.Substring(12, 4)}";
    }

    private static string HashRecoveryCode(string code)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder();
                int val = 0;
        int bitLen = 0;

        foreach (byte b in data)
        {
            val = (val << 8) | b;
            bitLen += 8;
            while (bitLen >= 5)
            {
                output.Append(alphabet[(val >> (bitLen - 5)) & 31]);
                bitLen -= 5;
            }
        }
        if (bitLen > 0)
        {
            output.Append(alphabet[(val << (5 - bitLen)) & 31]);
        }
        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        if (string.IsNullOrEmpty(input)) return [];
        input = input.TrimEnd('=');
        int byteCount = input.Length * 5 / 8;
        byte[] returnArray = new byte[byteCount];

        byte curByte = 0, bitsRemaining = 8;
        int mask = 0, arrayIndex = 0;

        foreach (char c in input.ToUpperInvariant())
        {
            int cValue = c switch
            {
                >= 'A' and <= 'Z' => c - 'A',
                >= '2' and <= '7' => c - '2' + 26,
                _ => throw new ArgumentException("Invalid Base32 string.")
            };

            if (bitsRemaining > 5)
            {
                mask = cValue << (bitsRemaining - 5);
                curByte = (byte)(curByte | mask);
                bitsRemaining -= 5;
            }
            else
            {
                mask = cValue >> (5 - bitsRemaining);
                curByte = (byte)(curByte | mask);
                returnArray[arrayIndex++] = curByte;
                curByte = (byte)(cValue << (3 + bitsRemaining));
                bitsRemaining += 3;
            }
        }
        return returnArray;
    }
}
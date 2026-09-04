using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class MfaApiIntegrationTests
{
    private const string Setup = "/api/identity/mfa/totp/setup";
    private const string Enable = "/api/identity/mfa/totp/enable";
    private const string Disable = "/api/identity/mfa/totp/disable";
    private const string Consume = "/api/identity/mfa/recovery/consume";

    [TestMethod]
    public async Task SetupTotpReturnsSharedKeyAndUri()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        using HttpResponseMessage response = await browser.PostWithoutBodyAsync(Setup, csrf);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.CacheControl?.NoStore);
        SetupTotpResult setup = await ReadAsync<SetupTotpResult>(response);
        Assert.AreEqual(32, setup.SharedKey.Length);
        Assert.IsTrue(setup.SharedKey.All(character => char.IsAsciiLetterUpper(character) || character is >= '2' and <= '7'));
        StringAssert.StartsWith(setup.AuthenticatorUri, "otpauth://totp/");
        StringAssert.Contains(setup.AuthenticatorUri, "secret=" + setup.SharedKey);
        Assert.IsFalse(string.IsNullOrWhiteSpace(setup.EnrollmentToken));
        Assert.IsFalse(setup.EnrollmentToken.Contains(setup.SharedKey, StringComparison.Ordinal));
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.recovery_codes"));
        AssertSecretsAbsentFromLogs(scenario, setup.SharedKey, setup.EnrollmentToken);
    }

    [TestMethod]
    public async Task EnableTotpPersistsEncryptedSecretAndReturnsRecoveryCodes()
    {
        var protection = new EphemeralDataProtectionProvider();
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configureServices: (services, _) => services.AddSingleton<IDataProtectionProvider>(protection));
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        SetupTotpResult setup = await SetupAsync(browser, csrf);
        using (HttpResponseMessage invalid = await browser.PostAsync(Enable,
            new EnableTotpCommand(WrongTotp(setup.SharedKey), setup.EnrollmentToken), csrf))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
        string[] recoveryCodes = await EnableAsync(browser, csrf, setup);
        Assert.HasCount(10, recoveryCodes);
        Assert.HasCount(10, recoveryCodes.Distinct(StringComparer.Ordinal));
        Assert.IsTrue(recoveryCodes.All(code => code.Length == 35 && code.Replace("-", string.Empty, StringComparison.Ordinal).Length == 32));
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();
        await using (var query = new NpgsqlCommand("SELECT \"ProtectedSecret\" FROM identity.totp_credentials", connection))
        {
            string protectedSecret = (string)(await query.ExecuteScalarAsync() ?? throw new AssertFailedException("Missing credential."));
            Assert.AreNotEqual(setup.SharedKey, protectedSecret);
            Assert.AreEqual(setup.SharedKey, protection.CreateProtector("AgropecuarIA.Identity.TOTP").Unprotect(protectedSecret));
        }
        await using (var query = new NpgsqlCommand("SELECT \"CodeHash\" FROM identity.recovery_codes", connection))
        await using (NpgsqlDataReader reader = await query.ExecuteReaderAsync())
        {
            var storedHashes = new List<string>();
            while (await reader.ReadAsync()) storedHashes.Add(reader.GetString(0));
            CollectionAssert.AreEquivalent(recoveryCodes.Select(HashRecoveryCode).ToArray(), storedHashes.ToArray());
            Assert.IsFalse(storedHashes.Intersect(recoveryCodes, StringComparer.Ordinal).Any());
        }
        using HttpResponseMessage duplicate = await browser.PostAsync(Enable,
            new EnableTotpCommand(Totp(setup.SharedKey, DateTimeOffset.UtcNow), setup.EnrollmentToken), csrf);
        Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.AreEqual(10L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.recovery_codes"));
        AssertSecretsAbsentFromLogs(scenario, [setup.SharedKey, setup.EnrollmentToken, .. recoveryCodes]);
    }

    [TestMethod]
    public async Task DisableTotpRemovesTotpAndRecoveryCodes()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        SetupTotpResult setup = await SetupAsync(browser, csrf);
        await EnableAsync(browser, csrf, setup);
        using (HttpResponseMessage invalid = await browser.PostAsync(Disable, new DisableTotpCommand(WrongTotp(setup.SharedKey)), csrf))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        Assert.AreEqual(1L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
        Assert.AreEqual(10L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.recovery_codes"));
        using HttpResponseMessage removed = await browser.PostAsync(Disable,
            new DisableTotpCommand(Totp(setup.SharedKey, DateTimeOffset.UtcNow)), csrf);
        Assert.AreEqual(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.recovery_codes"));
    }

    [TestMethod]
    public async Task ConsumeRecoveryCodeMarksCodeAsUsed()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        SetupTotpResult setup = await SetupAsync(browser, csrf);
        string[] codes = await EnableAsync(browser, csrf, setup);
        using BrowserSession competing = scenario.CreateBrowser(browser.Cookies);
        HttpResponseMessage[] results = await Task.WhenAll(
            browser.PostAsync(Consume, new ConsumeRecoveryCodeCommand(codes[0]), csrf),
            competing.PostAsync(Consume, new ConsumeRecoveryCodeCommand(codes[0]), csrf));
        try
        {
            Assert.AreEqual(1, results.Count(response => response.StatusCode == HttpStatusCode.NoContent));
            Assert.AreEqual(1, results.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
        }
        finally
        {
            foreach (HttpResponseMessage result in results) result.Dispose();
        }
        Assert.AreEqual(1L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.recovery_codes WHERE \"UsedAtUtc\" IS NOT NULL"));
        Assert.AreEqual(9L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.recovery_codes WHERE \"UsedAtUtc\" IS NULL"));
        using HttpResponseMessage replay = await browser.PostAsync(Consume, new ConsumeRecoveryCodeCommand(codes[0]), csrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [TestMethod]
    [DataRow(Setup)]
    [DataRow(Enable)]
    [DataRow(Disable)]
    [DataRow(Consume)]
    public async Task EveryMfaMutationRejectsMissingAntiforgery(string endpoint)
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        object request = endpoint switch
        {
            Enable => new EnableTotpCommand("123456", "invalid"),
            Disable => new DisableTotpCommand("123456"),
            Consume => new ConsumeRecoveryCodeCommand("invalid"),
            _ => new { },
        };
        using HttpResponseMessage denied = await browser.PostAsync(endpoint, request);
        Assert.AreEqual(HttpStatusCode.BadRequest, denied.StatusCode);
        JsonElement problem = await ReadAsync<JsonElement>(denied);
        Assert.AreEqual("request.invalid_antiforgery", problem.GetProperty("code").GetString());
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.recovery_codes"));
    }

    [TestMethod]
    [DataRow("email-owner")]
    [DataRow("identity-owned-by-another-user")]
    public async Task EnrollmentCannotBeTransferredToAnotherSessionOrUser(string fixture)
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        SetupTotpResult setup = await SetupAsync(owner, csrf);
        using BrowserSession recipient = scenario.CreateBrowser();
        string otherCsrf = await IdentityApiTestActions.SignInAsync(recipient, fixture);
        using HttpResponseMessage rejected = await recipient.PostAsync(Enable,
            new EnableTotpCommand(Totp(setup.SharedKey, DateTimeOffset.UtcNow), setup.EnrollmentToken), otherCsrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
        await EnableAsync(owner, csrf, setup);
    }

    [TestMethod]
    public async Task TamperedEnrollmentAndLegacyQuerySecretCannotEnableTotp()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        SetupTotpResult setup = await SetupAsync(browser, csrf);
        using (HttpResponseMessage tampered = await browser.PostAsync(Enable,
            new EnableTotpCommand(Totp(setup.SharedKey, DateTimeOffset.UtcNow), "tampered" + setup.EnrollmentToken), csrf))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, tampered.StatusCode);
        }
        using HttpResponseMessage legacy = await browser.PostAsync(Enable + "?base32Secret=" + setup.SharedKey,
            new { code = Totp(setup.SharedKey, DateTimeOffset.UtcNow) }, csrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, legacy.StatusCode);
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
    }

    [TestMethod]
    public async Task EnrollmentExpiresAfterTenMinutesWhileSessionRemainsRecent()
    {
        var clock = new MfaTestClock(DateTimeOffset.UtcNow);
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configureServices: (services, _) => services.AddSingleton<TimeProvider>(clock));
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        SetupTotpResult setup = await SetupAsync(browser, csrf);
        clock.Advance(TimeSpan.FromMinutes(10));
        using HttpResponseMessage expired = await browser.PostAsync(Enable,
            new EnableTotpCommand(Totp(setup.SharedKey, clock.GetUtcNow()), setup.EnrollmentToken), csrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, expired.StatusCode);
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
        using HttpResponseMessage session = await browser.GetAsync("/api/identity/session");
        Assert.AreEqual(HttpStatusCode.OK, session.StatusCode);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task StaleOrRevokedSessionCannotChangeMfa(bool revoked)
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        SetupTotpResult setup = await SetupAsync(browser, csrf);
        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var update = new NpgsqlCommand(revoked
                ? "UPDATE identity.sessions SET \"RevokedAtUtc\" = now()"
                : "UPDATE identity.sessions SET \"AuthenticatedAtUtc\" = now() - interval '1 hour'", connection);
            Assert.AreEqual(1, await update.ExecuteNonQueryAsync());
        }
        using HttpResponseMessage denied = await browser.PostAsync(Enable,
            new EnableTotpCommand(Totp(setup.SharedKey, DateTimeOffset.UtcNow), setup.EnrollmentToken), csrf);
        Assert.AreEqual(revoked ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.AreEqual(0L, await ScalarAsync(scenario, "SELECT count(*) FROM identity.totp_credentials"));
    }

    [TestMethod]
    public async Task MfaEndpointsRemainUnavailableInProduction()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Identity:Oidc:Authority"] = "https://identity.invalid",
            ["Identity:Oidc:ClientId"] = "test-client",
            ["Identity:Oidc:ClientSecret"] = "test-secret-not-a-real-credential",
            ["Identity:Oidc:GoogleEnabled"] = "false",
        };
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            environment: "Production", configuration: configuration);
        using BrowserSession browser = scenario.CreateBrowser();
        foreach (string endpoint in new[] { Setup, Enable, Disable, Consume })
        {
            using HttpResponseMessage response = await browser.PostAsync(endpoint,
                new { code = "123456", enrollmentToken = "invalid", recoveryCode = "invalid" });
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [TestMethod]
    public void TestTotpGeneratorMatchesRfc6238Vector()
    {
        // RFC 6238 Appendix B, SHA1 at Unix time 59: 94287082 (six-digit suffix 287082).
        // https://www.rfc-editor.org/rfc/rfc6238#appendix-B
        Assert.AreEqual("287082", Totp("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    private static async Task<SetupTotpResult> SetupAsync(BrowserSession browser, string csrf)
    {
        using HttpResponseMessage response = await browser.PostWithoutBodyAsync(Setup, csrf);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync<SetupTotpResult>(response);
    }

    private static async Task<string[]> EnableAsync(BrowserSession browser, string csrf, SetupTotpResult setup)
    {
        using HttpResponseMessage response = await browser.PostAsync(Enable,
            new EnableTotpCommand(Totp(setup.SharedKey, DateTimeOffset.UtcNow), setup.EnrollmentToken), csrf);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.CacheControl?.NoStore);
        return (await ReadAsync<EnableTotpResult>(response)).RecoveryCodes;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>() ?? throw new AssertFailedException("Expected a JSON response.");

    private static async Task<long> ScalarAsync(IdentityApiScenario scenario, string sql)
    {
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();
        await using var query = new NpgsqlCommand(sql, connection);
        return (long)(await query.ExecuteScalarAsync() ?? throw new AssertFailedException("Expected a count."));
    }

    private static void AssertSecretsAbsentFromLogs(IdentityApiScenario scenario, params string[] secrets)
    {
        foreach (string secret in secrets)
            Assert.IsFalse(scenario.Logs.Entries.Any(entry => entry.Contains(secret, StringComparison.Ordinal)));
    }

    private static string HashRecoveryCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))).ToLowerInvariant();

    private static string WrongTotp(string secret)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var accepted = Enumerable.Range(-3, 7)
            .Select(offset => Totp(secret, now.AddSeconds(offset * 30)))
            .ToHashSet(StringComparer.Ordinal);
        return Enumerable.Range(0, 8)
            .Select(value => value.ToString("D6", CultureInfo.InvariantCulture))
            .First(value => !accepted.Contains(value));
    }

    private static string Totp(string base32Secret, DateTimeOffset time)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        int buffer = 0;
        int bits = 0;
        foreach (char character in base32Secret)
        {
            int value = alphabet.IndexOf(character, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, value);
            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
            }
        }
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, time.ToUnixTimeSeconds() / 30);
#pragma warning disable CA5350 // RFC 6238 interoperable SHA-1 TOTP test generator.
        byte[] digest = HMACSHA1.HashData(bytes.ToArray(), counter);
#pragma warning restore CA5350
        int offset = digest[^1] & 15;
        uint truncated = BinaryPrimitives.ReadUInt32BigEndian(digest.AsSpan(offset, 4)) & 0x7fffffff;
        return (truncated % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private sealed class MfaTestClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}

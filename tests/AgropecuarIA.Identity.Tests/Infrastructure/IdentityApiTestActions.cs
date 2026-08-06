using System.Net;
using System.Text.Json;

namespace AgropecuarIA.Identity.Tests.Infrastructure;

internal static class IdentityApiTestActions
{
    public static async Task<string> SignInAsync(
        BrowserSession browser,
        string fixture,
        CancellationToken cancellationToken = default)
    {
        var antiforgeryToken = await browser.GetAntiforgeryTokenAsync(cancellationToken);
        using var response = await browser.PostAsync(
            "/api/development/identity/sign-in",
            new Dictionary<string, string> { ["fixture"] = fixture },
            antiforgeryToken,
            cancellationToken);
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        // ASP.NET Core binds antiforgery tokens to the current principal. Refresh after
        // sign-in so subsequent authenticated mutations cannot replay the anonymous token.
        return await browser.GetAntiforgeryTokenAsync(cancellationToken);
    }

    public static async Task<JsonDocument> GetSessionAsync(
        BrowserSession browser,
        CancellationToken cancellationToken = default)
    {
        using var response = await browser.GetAsync("/api/identity/session", cancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
    }

    public static async Task<Guid> StartLinkAsync(
        BrowserSession browser,
        string connection,
        string antiforgeryToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await browser.PostAsync(
            "/api/identity/link-attempts",
            new Dictionary<string, string> { ["connection"] = connection },
            antiforgeryToken,
            cancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        return json.RootElement.GetProperty("attemptId").GetGuid();
    }

    public static async Task VerifyCandidateAsync(
        BrowserSession browser,
        Guid attemptId,
        string fixture,
        string antiforgeryToken,
        CancellationToken cancellationToken = default)
    {
        using var response = await browser.PostAsync(
            $"/api/development/identity/link-attempts/{attemptId:D}/verify",
            new Dictionary<string, string> { ["fixture"] = fixture },
            antiforgeryToken,
            cancellationToken);
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static string[] Memberships(JsonElement session)
    {
        return session.GetProperty("memberships")
            .EnumerateArray()
            .Select(membership => membership.GetRawText())
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }
}

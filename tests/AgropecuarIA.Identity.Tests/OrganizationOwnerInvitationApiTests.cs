using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Tests.Infrastructure;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrganizationOwnerInvitationApiTests
{
    [TestMethod]
    public async Task OwnerCanCreateListAndInviteeCanAcceptOnlyOnce()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "3",
            });
        using BrowserSession owner = scenario.CreateBrowser();
        using BrowserSession invitee = scenario.CreateBrowser();
        using BrowserSession attacker = scenario.CreateBrowser();
        string ownerCsrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner-1");
        string inviteeCsrf = await IdentityApiTestActions.SignInAsync(invitee, "email-owner-2");
        _ = await IdentityApiTestActions.SignInAsync(attacker, "email-owner-3");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            owner,
            "Los Aromos",
            "api-owner-organization-key",
            ownerCsrf);
        ownerCsrf = await StrongAuthenticateAsync(owner, ownerCsrf);

        using HttpResponseMessage missingCsrf = await owner.PostWithIdempotencyKeyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-invitations",
            new Dictionary<string, string>(),
            antiforgeryToken: null,
            idempotencyKey: "api-owner-invitation-csrf");
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        string token;
        Guid invitationId;
        using (HttpResponseMessage created = await owner.PostWithIdempotencyKeyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-invitations",
            new Dictionary<string, string>(),
            ownerCsrf,
            "api-owner-invitation-key"))
        {
            Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
            Assert.IsTrue(created.Headers.CacheControl?.NoStore == true);
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await created.Content.ReadAsStreamAsync());
            token = payload.RootElement.GetProperty("token").GetString()!;
            invitationId = payload.RootElement
                .GetProperty("invitation")
                .GetProperty("invitationId")
                .GetGuid();
            Assert.AreEqual(43, token.Length);
        }

        using (HttpResponseMessage replay = await owner.PostWithIdempotencyKeyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-invitations",
            new Dictionary<string, string>(),
            ownerCsrf,
            "api-owner-invitation-key"))
        {
            Assert.AreEqual(HttpStatusCode.Created, replay.StatusCode);
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await replay.Content.ReadAsStreamAsync());
            Assert.IsTrue(payload.RootElement.GetProperty("isReplay").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, payload.RootElement.GetProperty("token").ValueKind);
            Assert.AreEqual(
                invitationId,
                payload.RootElement.GetProperty("invitation").GetProperty("invitationId").GetGuid());
        }

        using (HttpResponseMessage listed = await owner.GetAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-invitations"))
        {
            Assert.AreEqual(HttpStatusCode.OK, listed.StatusCode);
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await listed.Content.ReadAsStreamAsync());
            Assert.AreEqual(1, payload.RootElement.GetProperty("items").GetArrayLength());
        }

        using HttpResponseMessage foreignList = await attacker.GetAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-invitations");
        await AssertProblemAsync(
            foreignList,
            HttpStatusCode.NotFound,
            "identity.organization_owner_invitation_not_available");

        using (HttpResponseMessage accepted = await invitee.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = token },
            inviteeCsrf))
        {
            Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await accepted.Content.ReadAsStreamAsync());
            Assert.AreEqual(
                organizationId,
                payload.RootElement.GetProperty("organization").GetProperty("organizationId").GetGuid());
            Assert.AreEqual(
                OrganizationMembershipRoles.Owner,
                payload.RootElement.GetProperty("membership").GetProperty("role").GetString());
        }

        using HttpResponseMessage ownReplay = await invitee.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = token },
            inviteeCsrf);
        Assert.AreEqual(HttpStatusCode.OK, ownReplay.StatusCode);

        string attackerCsrf = await attacker.GetAntiforgeryTokenAsync();
        using HttpResponseMessage stolenReplay = await attacker.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = token },
            attackerCsrf);
        await AssertProblemAsync(
            stolenReplay,
            HttpStatusCode.NotFound,
            "identity.organization_owner_invitation_not_available");
    }

    [TestMethod]
    public async Task RevokeRequiresCsrfAndStrongCurrentIfMatch()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            owner,
            "La Reserva",
            "api-revoke-organization-key",
            csrf);
        csrf = await StrongAuthenticateAsync(owner, csrf);
        Guid invitationId;
        Guid version;
        using (HttpResponseMessage created = await owner.PostWithIdempotencyKeyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-invitations",
            new Dictionary<string, string>(),
            csrf,
            "api-revoke-invitation-key"))
        {
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await created.Content.ReadAsStreamAsync());
            JsonElement invitation = payload.RootElement.GetProperty("invitation");
            invitationId = invitation.GetProperty("invitationId").GetGuid();
            version = invitation.GetProperty("version").GetGuid();
        }

        string path = $"/api/identity/organizations/{organizationId:D}" +
            $"/owner-invitations/{invitationId:D}/revoke";
        using HttpResponseMessage missingCsrf = await owner.PostWithIfMatchAsync(
            path,
            new Dictionary<string, string>(),
            antiforgeryToken: null,
            ifMatch: $"\"{version:D}\"");
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        using HttpResponseMessage missingVersion = await owner.PostWithIfMatchAsync(
            path,
            new Dictionary<string, string>(),
            csrf,
            ifMatch: null);
        await AssertProblemAsync(
            missingVersion,
            HttpStatusCode.BadRequest,
            "identity.invalid_owner_invitation_version");

        using HttpResponseMessage stale = await owner.PostWithIfMatchAsync(
            path,
            new Dictionary<string, string>(),
            csrf,
            $"\"{Guid.NewGuid():D}\"");
        await AssertProblemAsync(
            stale,
            HttpStatusCode.PreconditionFailed,
            "identity.organization_owner_invitation_version_mismatch");

        using HttpResponseMessage revoked = await owner.PostWithIfMatchAsync(
            path,
            new Dictionary<string, string>(),
            csrf,
            $"\"{version:D}\"");
        Assert.AreEqual(HttpStatusCode.OK, revoked.StatusCode);
        Assert.IsNotNull(revoked.Headers.ETag);
    }

    private static async Task<string> StrongAuthenticateAsync(
        BrowserSession browser,
        string csrf)
    {
        using HttpResponseMessage started = await browser.PostAsync(
            "/api/identity/step-up-attempts",
            new Dictionary<string, string>
            {
                ["purpose"] = StepUpPurposes.ManageOrganizationOwners,
            },
            csrf);
        Assert.AreEqual(HttpStatusCode.Created, started.StatusCode);
        using JsonDocument payload = await JsonDocument.ParseAsync(
            await started.Content.ReadAsStreamAsync());
        Guid attemptId = payload.RootElement.GetProperty("attemptId").GetGuid();
        using HttpResponseMessage completed = await browser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            csrf);
        Assert.AreEqual(HttpStatusCode.OK, completed.StatusCode);
        return await browser.GetAntiforgeryTokenAsync();
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.AreEqual(expectedStatus, response.StatusCode);
        using JsonDocument payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        Assert.AreEqual(expectedCode, payload.RootElement.GetProperty("code").GetString());
    }
}

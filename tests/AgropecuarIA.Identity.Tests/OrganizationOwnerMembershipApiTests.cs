using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrganizationOwnerMembershipApiTests
{
    private static readonly string[] RemovalFaultTargets = ["journal", "outbox"];

    [TestMethod]
    public async Task OwnerCanListAndRemoveCoOwnerWithReplaySafeConcurrencyContract()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "3",
            });
        using BrowserSession owner = scenario.CreateBrowser();
        using BrowserSession coOwner = scenario.CreateBrowser();
        using BrowserSession attacker = scenario.CreateBrowser();
        string ownerCsrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner-1");
        string coOwnerCsrf = await IdentityApiTestActions.SignInAsync(coOwner, "email-owner-2");
        string attackerCsrf = await IdentityApiTestActions.SignInAsync(attacker, "email-owner-3");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            owner,
            "La Remoción",
            "owner-removal-organization-key",
            ownerCsrf);
        ownerCsrf = await StrongAuthenticateAsync(owner, ownerCsrf);

        var (acceptedToken, _, _) = await CreateInvitationAsync(
            owner,
            organizationId,
            ownerCsrf,
            "owner-removal-accepted-invitation");
        using (HttpResponseMessage accepted = await coOwner.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = acceptedToken },
            coOwnerCsrf))
        {
            Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
        }

        JsonElement initialOwner = (await ListOwnerMembershipsAsync(coOwner, organizationId))
            .Single(item => !item.GetProperty("isCurrentUser").GetBoolean());
        using (HttpResponseMessage missingStepUp = await coOwner.DeleteWithConcurrencyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships/" +
                $"{initialOwner.GetProperty("membershipId").GetGuid():D}",
            coOwnerCsrf,
            $"\"{initialOwner.GetProperty("version").GetGuid():D}\"",
            "owner-removal-missing-step-up"))
        {
            await AssertProblemAsync(
                missingStepUp,
                HttpStatusCode.Forbidden,
                "identity.strong_authentication_required");
        }

        coOwnerCsrf = await StrongAuthenticateAsync(coOwner, coOwnerCsrf);
        var (pendingToken, _, _) = await CreateInvitationAsync(
            coOwner,
            organizationId,
            coOwnerCsrf,
            "removed-owner-pending-invitation");

        JsonElement[] listed = await ListOwnerMembershipsAsync(owner, organizationId);
        Assert.HasCount(2, listed);
        Assert.IsTrue(listed.All(item =>
            item.GetProperty("organizationId").GetGuid() == organizationId));
        Assert.AreEqual(1, listed.Count(item => item.GetProperty("isCurrentUser").GetBoolean()));
        Assert.IsFalse(listed.Any(item => item.TryGetProperty("userId", out _)));
        JsonElement target = listed.Single(item => !item.GetProperty("isCurrentUser").GetBoolean());
        Guid membershipId = target.GetProperty("membershipId").GetGuid();
        Guid version = target.GetProperty("version").GetGuid();
        string path = $"/api/identity/organizations/{organizationId:D}" +
            $"/owner-memberships/{membershipId:D}";

        using HttpResponseMessage foreignList = await attacker.GetAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships");
        await AssertProblemAsync(
            foreignList,
            HttpStatusCode.NotFound,
            "identity.organization_owner_membership_not_available");

        using HttpResponseMessage missingCsrf = await owner.DeleteWithConcurrencyAsync(
            path,
            antiforgeryToken: null,
            ifMatch: $"\"{version:D}\"",
            idempotencyKey: "owner-removal-missing-csrf");
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        using HttpResponseMessage missingVersion = await owner.DeleteWithConcurrencyAsync(
            path,
            ownerCsrf,
            ifMatch: null,
            idempotencyKey: "owner-removal-missing-version");
        await AssertProblemAsync(
            missingVersion,
            HttpStatusCode.BadRequest,
            "identity.invalid_owner_membership_version");

        using HttpResponseMessage stale = await owner.DeleteWithConcurrencyAsync(
            path,
            ownerCsrf,
            $"\"{Guid.NewGuid():D}\"",
            "owner-removal-stale-version");
        await AssertProblemAsync(
            stale,
            HttpStatusCode.PreconditionFailed,
            "identity.organization_owner_membership_version_mismatch");

        const string removalKey = "owner-removal-replay-key";
        JsonElement removed;
        using (HttpResponseMessage response = await owner.DeleteWithConcurrencyAsync(
            path,
            ownerCsrf,
            $"\"{version:D}\"",
            removalKey))
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Headers.CacheControl?.NoStore == true);
            Assert.IsNotNull(response.Headers.ETag);
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());
            removed = payload.RootElement.Clone();
        }

        Assert.AreEqual(membershipId, removed.GetProperty("membershipId").GetGuid());
        Assert.AreEqual(organizationId, removed.GetProperty("organizationId").GetGuid());
        Assert.AreEqual(OrganizationMembershipStatuses.Removed, removed.GetProperty("status").GetString());
        Assert.AreEqual(OrganizationMembershipRoles.Owner, removed.GetProperty("role").GetString());
        Assert.IsFalse(removed.GetProperty("isCurrentUser").GetBoolean());
        Assert.IsFalse(removed.GetProperty("isReplay").GetBoolean());
        Assert.IsFalse(removed.TryGetProperty("userId", out _));

        using (HttpResponseMessage replay = await owner.DeleteWithConcurrencyAsync(
            path,
            ownerCsrf,
            $"\"{version:D}\"",
            removalKey))
        {
            Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await replay.Content.ReadAsStreamAsync());
            Assert.IsTrue(payload.RootElement.GetProperty("isReplay").GetBoolean());
            Assert.AreEqual(
                removed.GetProperty("version").GetGuid(),
                payload.RootElement.GetProperty("version").GetGuid());
        }

        JsonElement[] afterRemoval = await ListOwnerMembershipsAsync(owner, organizationId);
        Assert.HasCount(1, afterRemoval);
        Assert.IsTrue(afterRemoval[0].GetProperty("isCurrentUser").GetBoolean());

        using HttpResponseMessage removedSession = await coOwner.GetAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships");
        await AssertProblemAsync(
            removedSession,
            HttpStatusCode.NotFound,
            "identity.organization_owner_membership_not_available");

        using HttpResponseMessage acceptedReplay = await coOwner.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = acceptedToken },
            coOwnerCsrf);
        await AssertProblemAsync(
            acceptedReplay,
            HttpStatusCode.NotFound,
            "identity.organization_owner_invitation_not_available");

        using HttpResponseMessage revokedBearer = await attacker.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = pendingToken },
            attackerCsrf);
        await AssertProblemAsync(
            revokedBearer,
            HttpStatusCode.NotFound,
            "identity.organization_owner_invitation_not_available");
    }

    [TestMethod]
    public async Task SelfRemovalIsNeutralAndHasNoEffect()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            owner,
            "Owner único",
            "self-removal-organization-key",
            csrf);
        csrf = await StrongAuthenticateAsync(owner, csrf);
        JsonElement ownMembership = (await ListOwnerMembershipsAsync(owner, organizationId)).Single();

        using HttpResponseMessage response = await owner.DeleteWithConcurrencyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships/" +
                $"{ownMembership.GetProperty("membershipId").GetGuid():D}",
            csrf,
            $"\"{ownMembership.GetProperty("version").GetGuid():D}\"",
            "self-removal-neutral-key");
        await AssertProblemAsync(
            response,
            HttpStatusCode.NotFound,
            "identity.organization_owner_membership_not_available");
        Assert.HasCount(1, await ListOwnerMembershipsAsync(owner, organizationId));
    }

    [TestMethod]
    public async Task NeutralTargetsAndIdempotencyConflictsHaveNoEffect()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "3",
            });
        using BrowserSession owner = scenario.CreateBrowser();
        using BrowserSession coOwner = scenario.CreateBrowser();
        using BrowserSession foreignOwner = scenario.CreateBrowser();
        string ownerCsrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner-1");
        string coOwnerCsrf = await IdentityApiTestActions.SignInAsync(coOwner, "email-owner-2");
        string foreignCsrf = await IdentityApiTestActions.SignInAsync(foreignOwner, "email-owner-3");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            owner,
            "Neutral removal",
            "neutral-removal-organization-key",
            ownerCsrf);
        Guid foreignOrganizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            foreignOwner,
            "Foreign removal",
            "foreign-removal-organization-key",
            foreignCsrf);
        ownerCsrf = await StrongAuthenticateAsync(owner, ownerCsrf);
        var (coOwnerToken, _, _) = await CreateInvitationAsync(
            owner,
            organizationId,
            ownerCsrf,
            "neutral-removal-co-owner-invitation");
        using (HttpResponseMessage accepted = await coOwner.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = coOwnerToken },
            coOwnerCsrf))
        {
            Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
        }

        JsonElement target = (await ListOwnerMembershipsAsync(owner, organizationId))
            .Single(item => !item.GetProperty("isCurrentUser").GetBoolean());
        JsonElement foreignMembership = (await ListOwnerMembershipsAsync(
            foreignOwner,
            foreignOrganizationId)).Single();
        string targetPath = $"/api/identity/organizations/{organizationId:D}/owner-memberships/" +
            $"{target.GetProperty("membershipId").GetGuid():D}";

        using (HttpResponseMessage absent = await owner.DeleteWithConcurrencyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships/{Guid.NewGuid():D}",
            ownerCsrf,
            $"\"{Guid.NewGuid():D}\"",
            "owner-removal-absent-target"))
        {
            await AssertProblemAsync(
                absent,
                HttpStatusCode.NotFound,
                "identity.organization_owner_membership_not_available");
        }

        using (HttpResponseMessage foreign = await owner.DeleteWithConcurrencyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships/" +
                $"{foreignMembership.GetProperty("membershipId").GetGuid():D}",
            ownerCsrf,
            $"\"{foreignMembership.GetProperty("version").GetGuid():D}\"",
            "owner-removal-foreign-target"))
        {
            await AssertProblemAsync(
                foreign,
                HttpStatusCode.NotFound,
                "identity.organization_owner_membership_not_available");
        }

        const string completedKey = "owner-removal-completed-key";
        Guid removedVersion;
        using (HttpResponseMessage removed = await owner.DeleteWithConcurrencyAsync(
            targetPath,
            ownerCsrf,
            $"\"{target.GetProperty("version").GetGuid():D}\"",
            completedKey))
        {
            Assert.AreEqual(HttpStatusCode.OK, removed.StatusCode);
            using JsonDocument payload = await JsonDocument.ParseAsync(
                await removed.Content.ReadAsStreamAsync());
            removedVersion = payload.RootElement.GetProperty("version").GetGuid();
        }

        using (HttpResponseMessage alreadyRemoved = await owner.DeleteWithConcurrencyAsync(
            targetPath,
            ownerCsrf,
            $"\"{removedVersion:D}\"",
            "owner-removal-already-removed"))
        {
            await AssertProblemAsync(
                alreadyRemoved,
                HttpStatusCode.NotFound,
                "identity.organization_owner_membership_not_available");
        }

        using (HttpResponseMessage fingerprintMismatch = await owner.DeleteWithConcurrencyAsync(
            targetPath,
            ownerCsrf,
            $"\"{Guid.NewGuid():D}\"",
            completedKey))
        {
            await AssertProblemAsync(
                fingerprintMismatch,
                HttpStatusCode.Conflict,
                "idempotency.key_reused");
        }

        var (inFlightToken, _, _) = await CreateInvitationAsync(
            owner,
            organizationId,
            ownerCsrf,
            "in-flight-removal-owner-invitation");
        using (HttpResponseMessage accepted = await foreignOwner.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = inFlightToken },
            foreignCsrf))
        {
            Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
        }

        JsonElement inFlightTarget = (await ListOwnerMembershipsAsync(owner, organizationId))
            .Single(item => !item.GetProperty("isCurrentUser").GetBoolean());
        const string inFlightKey = "owner-removal-in-flight-key";
        using JsonDocument ownerSession = await IdentityApiTestActions.GetSessionAsync(owner);
        await SeedInProgressOwnerRemovalAsync(
            scenario.ConnectionString,
            organizationId,
            ownerSession.RootElement.GetProperty("userId").GetGuid(),
            inFlightTarget.GetProperty("membershipId").GetGuid(),
            inFlightTarget.GetProperty("version").GetGuid(),
            inFlightKey);

        using (HttpResponseMessage inFlight = await owner.DeleteWithConcurrencyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships/" +
                $"{inFlightTarget.GetProperty("membershipId").GetGuid():D}",
            ownerCsrf,
            $"\"{inFlightTarget.GetProperty("version").GetGuid():D}\"",
            inFlightKey))
        {
            await AssertProblemAsync(inFlight, HttpStatusCode.Conflict, "idempotency.in_progress");
            Assert.IsTrue(inFlight.Headers.TryGetValues("Retry-After", out var retryAfter));
            Assert.AreEqual("1", retryAfter.Single());
        }

        Assert.HasCount(2, await ListOwnerMembershipsAsync(owner, organizationId));
    }

    [TestMethod]
    public async Task JournalAndOutboxFaultsRollBackOwnerRemovalCompletely()
    {
        foreach (string faultTarget in RemovalFaultTargets)
        {
            await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
                configuration: new Dictionary<string, string?>
                {
                    ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "2",
                });
            using BrowserSession owner = scenario.CreateBrowser();
            using BrowserSession coOwner = scenario.CreateBrowser();
            string ownerCsrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner-1");
            string coOwnerCsrf = await IdentityApiTestActions.SignInAsync(coOwner, "email-owner-2");
            Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
                owner,
                $"Removal fault {faultTarget}",
                $"owner-removal-{faultTarget}-organization",
                ownerCsrf);
            ownerCsrf = await StrongAuthenticateAsync(owner, ownerCsrf);
            var (acceptedToken, _, _) = await CreateInvitationAsync(
                owner,
                organizationId,
                ownerCsrf,
                $"owner-removal-{faultTarget}-accepted");
            using (HttpResponseMessage accepted = await coOwner.PostAsync(
                "/api/identity/owner-invitations/accept",
                new Dictionary<string, string> { ["token"] = acceptedToken },
                coOwnerCsrf))
            {
                Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
            }

            coOwnerCsrf = await StrongAuthenticateAsync(coOwner, coOwnerCsrf);
            var (_, pendingInvitationId, _) = await CreateInvitationAsync(
                coOwner,
                organizationId,
                coOwnerCsrf,
                $"owner-removal-{faultTarget}-pending");
            JsonElement target = (await ListOwnerMembershipsAsync(owner, organizationId))
                .Single(item => !item.GetProperty("isCurrentUser").GetBoolean());
            Guid membershipId = target.GetProperty("membershipId").GetGuid();
            Guid version = target.GetProperty("version").GetGuid();
            await InstallOwnerRemovalFaultAsync(scenario.ConnectionString, faultTarget);

            using HttpResponseMessage response = await owner.DeleteWithConcurrencyAsync(
                $"/api/identity/organizations/{organizationId:D}/owner-memberships/{membershipId:D}",
                ownerCsrf,
                $"\"{version:D}\"",
                $"owner-removal-{faultTarget}-rollback");
            await AssertProblemAsync(response, HttpStatusCode.InternalServerError, "server.unexpected");
            await AssertOwnerRemovalRolledBackAsync(
                scenario.ConnectionString,
                organizationId,
                membershipId,
                version,
                pendingInvitationId);
        }
    }

    [TestMethod]
    public async Task UnknownCommitFailsWithRetryableReconciliationAndNoFalseSuccess()
    {
        ToggleCommitBoundary boundary = new();
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "2",
            },
            configureServices: (services, _) =>
            {
                services.RemoveAll<IOrganizationCreationCommitBoundary>();
                services.AddSingleton<IOrganizationCreationCommitBoundary>(boundary);
            });
        using BrowserSession owner = scenario.CreateBrowser();
        using BrowserSession coOwner = scenario.CreateBrowser();
        string ownerCsrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner-1");
        string coOwnerCsrf = await IdentityApiTestActions.SignInAsync(coOwner, "email-owner-2");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            owner,
            "Commit ambiguo",
            "unknown-commit-organization-key",
            ownerCsrf);
        ownerCsrf = await StrongAuthenticateAsync(owner, ownerCsrf);
        var (token, _, _) = await CreateInvitationAsync(
            owner,
            organizationId,
            ownerCsrf,
            "unknown-commit-owner-invitation");
        using (HttpResponseMessage accepted = await coOwner.PostAsync(
            "/api/identity/owner-invitations/accept",
            new Dictionary<string, string> { ["token"] = token },
            coOwnerCsrf))
        {
            Assert.AreEqual(HttpStatusCode.OK, accepted.StatusCode);
        }

        JsonElement target = (await ListOwnerMembershipsAsync(owner, organizationId))
            .Single(item => !item.GetProperty("isCurrentUser").GetBoolean());
        string path = $"/api/identity/organizations/{organizationId:D}/owner-memberships/" +
            $"{target.GetProperty("membershipId").GetGuid():D}";
        string ifMatch = $"\"{target.GetProperty("version").GetGuid():D}\"";
        boundary.FailNextCommit = true;

        using HttpResponseMessage unknown = await owner.DeleteWithConcurrencyAsync(
            path,
            ownerCsrf,
            ifMatch,
            "unknown-commit-removal-key");
        await AssertProblemAsync(
            unknown,
            HttpStatusCode.ServiceUnavailable,
            "idempotency.reconciliation_required");

        Assert.HasCount(2, await ListOwnerMembershipsAsync(owner, organizationId));
        using HttpResponseMessage retry = await owner.DeleteWithConcurrencyAsync(
            path,
            ownerCsrf,
            ifMatch,
            "unknown-commit-removal-key");
        Assert.AreEqual(HttpStatusCode.OK, retry.StatusCode);
    }

    private static async Task<(string Token, Guid InvitationId, Guid Version)> CreateInvitationAsync(
        BrowserSession browser,
        Guid organizationId,
        string csrf,
        string idempotencyKey)
    {
        using HttpResponseMessage response = await browser.PostWithIdempotencyKeyAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-invitations",
            new Dictionary<string, string>(),
            csrf,
            idempotencyKey);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        JsonElement invitation = payload.RootElement.GetProperty("invitation");
        return (
            payload.RootElement.GetProperty("token").GetString()!,
            invitation.GetProperty("invitationId").GetGuid(),
            invitation.GetProperty("version").GetGuid());
    }

    private static async Task<JsonElement[]> ListOwnerMembershipsAsync(
        BrowserSession browser,
        Guid organizationId)
    {
        using HttpResponseMessage response = await browser.GetAsync(
            $"/api/identity/organizations/{organizationId:D}/owner-memberships");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        Assert.AreEqual(JsonValueKind.Array, payload.RootElement.ValueKind);
        return payload.RootElement.EnumerateArray().Select(item => item.Clone()).ToArray();
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

    private static async Task SeedInProgressOwnerRemovalAsync(
        string connectionString,
        Guid organizationId,
        Guid actorUserId,
        Guid membershipId,
        Guid expectedVersion,
        string idempotencyKey)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        Guid sessionId;
        Guid authorizationVersion;
        await using (NpgsqlCommand session = new(
            """
            SELECT "Id", "Version"
            FROM identity.sessions
            WHERE "UserId" = @actor AND "RevokedAtUtc" IS NULL
            ORDER BY "AuthenticatedAtUtc" DESC, "Id"
            LIMIT 1
            """,
            connection))
        {
            session.Parameters.AddWithValue("actor", actorUserId);
            await using NpgsqlDataReader reader = await session.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());
            sessionId = reader.GetGuid(0);
            authorizationVersion = reader.GetGuid(1);
        }

        byte[] fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(
            '|',
            "remove-owner-membership-v1",
            organizationId.ToString("D"),
            membershipId.ToString("D"),
            expectedVersion.ToString("D"))));
        byte[] hmacKey = Convert.FromBase64String(
            "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=");
        byte[] keyDigest = HMACSHA256.HashData(hmacKey, Encoding.ASCII.GetBytes(idempotencyKey));
        Guid ledgerId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
        await using NpgsqlCommand seed = new(
            """
            INSERT INTO identity.organization_owner_removal_ledgers
                ("Id", "OrganizationId", "ScopeKind", "Namespace", "Operation",
                 "ContractVersion", "CanonicalizationVersion", "ActorUserId", "SessionId",
                 "AuthorizationVersion", "MembershipId", "ExpectedMembershipVersion",
                 "RequestFingerprint", "State", "LeaseOwner", "FenceToken", "LeaseUntilUtc",
                 "StartedAtUtc", "Version")
            VALUES
                (@ledger, @organization, 'tenant', 'organization-owner-membership', 'remove_owner',
                 1, 1, @actor, @session, @authorization, @membership, @expected,
                 @fingerprint, 'in_progress', @leaseOwner, 1, @leaseUntil, @started, @version);
            INSERT INTO identity.organization_owner_removal_key_aliases
                ("Id", "LedgerId", "OrganizationId", "ScopeKind", "Namespace", "Operation",
                 "KeyVersion", "KeyDigest", "CreatedAtUtc")
            VALUES
                (@alias, @ledger, @organization, 'tenant', 'organization-owner-membership',
                 'remove_owner', 'test-v1', @digest, @started);
            """,
            connection);
        seed.Parameters.AddWithValue("ledger", ledgerId);
        seed.Parameters.AddWithValue("organization", organizationId);
        seed.Parameters.AddWithValue("actor", actorUserId);
        seed.Parameters.AddWithValue("session", sessionId);
        seed.Parameters.AddWithValue("authorization", authorizationVersion);
        seed.Parameters.AddWithValue("membership", membershipId);
        seed.Parameters.AddWithValue("expected", expectedVersion);
        seed.Parameters.AddWithValue("fingerprint", fingerprint);
        seed.Parameters.AddWithValue("leaseOwner", Guid.NewGuid());
        seed.Parameters.AddWithValue("leaseUntil", now.AddMinutes(1));
        seed.Parameters.AddWithValue("started", now);
        seed.Parameters.AddWithValue("version", Guid.NewGuid());
        seed.Parameters.AddWithValue("alias", Guid.NewGuid());
        seed.Parameters.AddWithValue("digest", keyDigest);
        Assert.AreEqual(2, await seed.ExecuteNonQueryAsync());
    }

    private static async Task InstallOwnerRemovalFaultAsync(
        string connectionString,
        string faultTarget)
    {
        string sql = faultTarget switch
        {
            "journal" =>
                """
                CREATE FUNCTION identity.test_fail_owner_removal_journal()
                RETURNS trigger LANGUAGE plpgsql AS $failure$
                BEGIN
                    IF NEW."Action" = 'organization_owner_membership_removed' THEN
                        RAISE EXCEPTION 'injected owner removal journal failure';
                    END IF;
                    RETURN NEW;
                END
                $failure$;
                CREATE TRIGGER test_fail_owner_removal_journal
                    BEFORE INSERT ON identity.audit_events
                    FOR EACH ROW EXECUTE FUNCTION identity.test_fail_owner_removal_journal();
                """,
            "outbox" =>
                """
                CREATE FUNCTION identity.test_fail_owner_removal_outbox()
                RETURNS trigger LANGUAGE plpgsql AS $failure$
                BEGIN
                    IF NEW."Type" = 'OrganizationOwnerMembershipRemoved' THEN
                        RAISE EXCEPTION 'injected owner removal outbox failure';
                    END IF;
                    RETURN NEW;
                END
                $failure$;
                CREATE TRIGGER test_fail_owner_removal_outbox
                    BEFORE INSERT ON identity.outbox_messages
                    FOR EACH ROW EXECUTE FUNCTION identity.test_fail_owner_removal_outbox();
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(faultTarget)),
        };
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertOwnerRemovalRolledBackAsync(
        string connectionString,
        Guid organizationId,
        Guid membershipId,
        Guid membershipVersion,
        Guid pendingInvitationId)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT
                (SELECT "Status" = 'active' AND "SecurityVersion" = 1
                        AND "RemovedAtUtc" IS NULL AND "RemovedByUserId" IS NULL
                        AND "Version" = @membershipVersion
                 FROM identity.memberships WHERE "Id" = @membership),
                EXISTS (
                    SELECT 1
                    FROM identity.organization_memberships AS legacy
                    JOIN identity.memberships AS membership
                      ON membership."OrganizationId" = legacy."OrganizationId"
                     AND membership."UserId" = legacy."UserId"
                    WHERE membership."Id" = @membership),
                (SELECT "Status" = 'pending' AND "RevokedAtUtc" IS NULL
                 FROM identity.organization_owner_invitations WHERE "Id" = @invitation),
                (SELECT count(*) FROM identity.organization_owner_removal_ledgers
                 WHERE "OrganizationId" = @organization AND "MembershipId" = @membership),
                (SELECT count(*) FROM identity.organization_owner_removal_key_aliases
                 WHERE "OrganizationId" = @organization),
                (SELECT count(*) FROM identity.audit_events
                 WHERE "Action" = 'organization_owner_membership_removed'),
                (SELECT count(*) FROM identity.outbox_messages
                 WHERE "Type" = 'OrganizationOwnerMembershipRemoved')
            """,
            connection);
        command.Parameters.AddWithValue("organization", organizationId);
        command.Parameters.AddWithValue("membership", membershipId);
        command.Parameters.AddWithValue("membershipVersion", membershipVersion);
        command.Parameters.AddWithValue("invitation", pendingInvitationId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.IsTrue(reader.GetBoolean(0), "The authoritative membership was not rolled back.");
        Assert.IsTrue(reader.GetBoolean(1), "The legacy membership projection was not rolled back.");
        Assert.IsTrue(reader.GetBoolean(2), "The pending invitation revocation was not rolled back.");
        for (int index = 3; index < 7; index++)
        {
            Assert.AreEqual(0L, reader.GetInt64(index), $"Rollback count {index} was not zero.");
        }
    }

    private sealed class ToggleCommitBoundary : IOrganizationCreationCommitBoundary
    {
        public bool FailNextCommit { get; set; }

        public Task CommitAsync(
            Func<CancellationToken, Task> commit,
            Func<CancellationToken, Task> rollback,
            CancellationToken cancellationToken)
        {
            if (FailNextCommit)
            {
                FailNextCommit = false;
                throw new OrganizationCommitOutcomeUnknownException(
                    "Injected unknown commit outcome.");
            }

            return commit(cancellationToken);
        }
    }
}

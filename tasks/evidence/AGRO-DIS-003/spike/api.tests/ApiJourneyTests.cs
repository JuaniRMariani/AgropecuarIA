using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace AgropecuarIA.IdentitySpike.Tests;

[TestClass]
public sealed class ApiJourneyTests
{
    private const string OrganizationA = "00000000-0000-0000-0000-00000000000a";
    private const string OrganizationB = "00000000-0000-0000-0000-00000000000b";
    private const string RecordA = "30000000-0000-0000-0000-00000000000a";
    private const string RecordB = "30000000-0000-0000-0000-00000000000b";
    private const string OneOrganizationUser = "10000000-0000-0000-0000-000000000001";

    [TestMethod]
    public async Task SessionResolutionCoversZeroOneAndManyMemberships()
    {
        await using var factory = new IdentitySpikeFactory();

        using (var signedOutClient = factory.CreateBrowserClient())
        {
            using var signedOut = await signedOutClient.GetAsync("/api/spike/session");
            await AssertJsonAsync(signedOut, HttpStatusCode.OK, "kind", "signed_out");
        }

        using (var zeroClient = factory.CreateBrowserClient())
        {
            await CreateSessionAsync(zeroClient, "zero");
            using var zero = await zeroClient.GetAsync("/api/spike/session");
            await AssertProblemAsync(zero, HttpStatusCode.Forbidden, "active-membership-required");
        }

        using (var oneClient = factory.CreateBrowserClient())
        {
            await CreateSessionAsync(oneClient, "one");
            using var one = await oneClient.GetAsync("/api/spike/session");
            await AssertJsonAsync(one, HttpStatusCode.OK, "kind", "active");
            using var document = await ReadJsonAsync(one);
            Assert.AreEqual(OrganizationA, document.RootElement.GetProperty("tenant").GetProperty("organizationId").GetString());
            Assert.AreEqual(
                "membership-v1",
                document.RootElement.GetProperty("authorizationVersion").GetString());
        }

        using (var manyClient = factory.CreateBrowserClient())
        {
            await CreateSessionAsync(manyClient, "many");
            using var many = await manyClient.GetAsync("/api/spike/session");
            await AssertJsonAsync(many, HttpStatusCode.OK, "kind", "selection_required");
            using var document = await ReadJsonAsync(many);
            JsonElement.ArrayEnumerator organizations =
                document.RootElement.GetProperty("organizations").EnumerateArray();
            Assert.IsTrue(organizations.MoveNext());
            Assert.AreEqual(OrganizationA, organizations.Current.GetProperty("organizationId").GetString());
            Assert.IsFalse(organizations.Current.TryGetProperty("permissions", out _));
            Assert.IsTrue(organizations.MoveNext());
            Assert.AreEqual(OrganizationB, organizations.Current.GetProperty("organizationId").GetString());
            Assert.IsFalse(organizations.Current.TryGetProperty("permissions", out _));
            Assert.IsFalse(organizations.MoveNext());
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task RevokedMembershipBetweenListingAndSwitchIsNeutralAndDoesNotRotateSession()
    {
        await using var factory = new IdentitySpikeFactory();
        using var client = factory.CreateBrowserClient();
        var originalSessionId = await CreateSessionAsync(client, "many");

        using (var listed = await client.GetAsync("/api/spike/session"))
        {
            await AssertJsonAsync(listed, HttpStatusCode.OK, "kind", "selection_required");
        }

        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        await SetSharedMembershipBStateAsync(factory.OwnerConnectionString, isActive: false, securityVersion: 2);
        try
        {
            using (var denied = await PostWithAntiforgeryAsync(
                client,
                "/api/spike/session/switch-organization",
                new { organizationId = OrganizationB },
                antiforgeryToken))
            {
                await AssertProblemAsync(denied, HttpStatusCode.NotFound, "resource-not-found");
            }

            using var current = await client.GetAsync("/api/spike/session");
            await AssertJsonAsync(current, HttpStatusCode.OK, "kind", "active");
            using var document = await ReadJsonAsync(current);
            Assert.AreEqual(
                originalSessionId,
                document.RootElement.GetProperty("session").GetProperty("sessionId").GetString());
            Assert.AreEqual(
                OrganizationA,
                document.RootElement.GetProperty("tenant").GetProperty("organizationId").GetString());
        }
        finally
        {
            await SetSharedMembershipBStateAsync(factory.OwnerConnectionString, isActive: true, securityVersion: 1);
        }
    }

    [TestMethod]
    public async Task OrganizationSwitchRequiresAntiforgeryRotatesSessionAndDeniesUnknownTenant()
    {
        await using var factory = new IdentitySpikeFactory();
        using var client = factory.CreateBrowserClient();
        var oldSessionId = await CreateSessionAsync(client, "many");

        using (var missingToken = await client.PostAsJsonAsync(
            "/api/spike/session/switch-organization",
            new { organizationId = OrganizationB }))
        {
            await AssertProblemAsync(missingToken, HttpStatusCode.BadRequest, "invalid-antiforgery-token");
        }

        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using (var denied = await PostWithAntiforgeryAsync(
            client,
            "/api/spike/session/switch-organization",
            new { organizationId = "00000000-0000-0000-0000-000000000099" },
            antiforgeryToken))
        {
            await AssertProblemAsync(denied, HttpStatusCode.NotFound, "resource-not-found");
        }

        using (var switched = await PostWithAntiforgeryAsync(
            client,
            "/api/spike/session/switch-organization",
            new { organizationId = OrganizationB },
            antiforgeryToken))
        {
            await AssertJsonAsync(switched, HttpStatusCode.OK, "kind", "active");
            using var document = await ReadJsonAsync(switched);
            Assert.AreEqual(OrganizationB, document.RootElement.GetProperty("tenant").GetProperty("organizationId").GetString());
            Assert.AreNotEqual(oldSessionId, document.RootElement.GetProperty("session").GetProperty("sessionId").GetString());
        }

        using var oldSessionClient = factory.CreateBrowserClient();
        using var oldRequest = new HttpRequestMessage(HttpMethod.Get, "/api/spike/session");
        oldRequest.Headers.TryAddWithoutValidation("Cookie", $"__Host-agro-dis003-session={oldSessionId}");
        using var oldSession = await oldSessionClient.SendAsync(oldRequest);
        await AssertJsonAsync(oldSession, HttpStatusCode.OK, "kind", "revoked");
    }

    [TestMethod]
    public async Task TenantRecordAccessIsIsolatedAndCrossTenantNotFoundIsNeutral()
    {
        await using var factory = new IdentitySpikeFactory();
        using var client = factory.CreateBrowserClient();
        await CreateSessionAsync(client, "one");

        using (var own = await client.GetAsync($"/api/spike/tenant-records/{RecordA}"))
        {
            Assert.AreEqual(HttpStatusCode.OK, own.StatusCode);
            using var document = await ReadJsonAsync(own);
            Assert.AreEqual("Registro A", document.RootElement.GetProperty("label").GetString());
            Assert.AreEqual(OrganizationA, document.RootElement.GetProperty("organizationId").GetString());
        }

        using (var foreign = await client.GetAsync($"/api/spike/tenant-records/{RecordB}"))
        {
            await AssertProblemAsync(foreign, HttpStatusCode.NotFound, "resource-not-found");
        }

        using (var absent = await client.GetAsync("/api/spike/tenant-records/30000000-0000-0000-0000-000000000099"))
        {
            await AssertProblemAsync(absent, HttpStatusCode.NotFound, "resource-not-found");
        }
    }

    [TestMethod]
    public async Task ActiveMembershipWithoutReadPermissionIsDeniedByDefault()
    {
        await using var factory = new IdentitySpikeFactory();
        using var client = factory.CreateBrowserClient();
        await CreateSessionAsync(client, "no-read");

        using var response = await client.GetAsync($"/api/spike/tenant-records/{RecordB}");
        await AssertProblemAsync(response, HttpStatusCode.NotFound, "resource-not-found");
    }

    [TestMethod]
    public async Task LinkingRequiresBothIdentityProofsAndDoesNotUseEmailAsAuthority()
    {
        await using var factory = new IdentitySpikeFactory();
        using var client = factory.CreateBrowserClient();
        await CreateSessionAsync(client, "one");
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        var candidateSubject = $"passkey-{Guid.NewGuid():N}";

        using (var invalidIssuer = await PostWithAntiforgeryAsync(
            client,
            "/api/spike/link-attempts",
            new { issuer = "http://not-secure.invalid", subject = candidateSubject, email = "same@example.invalid" },
            antiforgeryToken))
        {
            await AssertProblemAsync(invalidIssuer, HttpStatusCode.BadRequest, "invalid-external-identity");
        }

        var attemptId = await CreateLinkAttemptAsync(client, candidateSubject, antiforgeryToken);

        using (var premature = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{attemptId}/complete",
            new { },
            antiforgeryToken))
        {
            await AssertProblemAsync(premature, HttpStatusCode.Conflict, "link-state-conflict");
        }

        var currentProof = await IssueProofAsync(
            client,
            "https://fake-idp.invalid/email",
            "user-a",
            antiforgeryToken);
        using (var current = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{attemptId}/reauthenticate-current",
            new { proofId = currentProof },
            antiforgeryToken))
        {
            await AssertJsonAsync(current, HttpStatusCode.OK, "state", "current_reauthenticated");
        }

        var candidateProof = await IssueProofAsync(
            client,
            "https://fake-idp.invalid/passkey",
            candidateSubject,
            antiforgeryToken);
        using (var candidate = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{attemptId}/reauthenticate-candidate",
            new { proofId = candidateProof },
            antiforgeryToken))
        {
            await AssertJsonAsync(candidate, HttpStatusCode.OK, "state", "candidate_reauthenticated");
        }

        using var complete = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{attemptId}/complete",
            new { },
            antiforgeryToken);
        await AssertJsonAsync(complete, HttpStatusCode.OK, "state", "linked");
    }

    [TestMethod]
    public async Task LinkingRequiresCurrentStepUpAntiforgeryAndNullSafeIdentityInput()
    {
        await using var factory = new IdentitySpikeFactory();

        using (var expiredClient = factory.CreateBrowserClient())
        {
            await CreateSessionAsync(expiredClient, "one", stepUpExpired: true);
            var token = await GetAntiforgeryTokenAsync(expiredClient);
            using var expired = await PostWithAntiforgeryAsync(
                expiredClient,
                "/api/spike/link-attempts",
                new { issuer = "https://fake-idp.invalid/passkey", subject = "candidate" },
                token);
            await AssertProblemAsync(expired, HttpStatusCode.Forbidden, "step-up-required");
        }

        using (var validClient = factory.CreateBrowserClient())
        {
            await CreateSessionAsync(validClient, "one");
            using (var missingAntiforgery = await validClient.PostAsJsonAsync(
                "/api/spike/link-attempts",
                new { issuer = "https://fake-idp.invalid/passkey", subject = "candidate" }))
            {
                await AssertProblemAsync(
                    missingAntiforgery,
                    HttpStatusCode.BadRequest,
                    "invalid-antiforgery-token");
            }

            var token = await GetAntiforgeryTokenAsync(validClient);
            using var nullInput = await PostWithAntiforgeryAsync(
                validClient,
                "/api/spike/link-attempts",
                new { issuer = (string?)null, subject = (string?)null },
                token);
            await AssertProblemAsync(nullInput, HttpStatusCode.BadRequest, "invalid-external-identity");
        }
    }

    [TestMethod]
    public async Task LinkingRejectsProofReplayAndIdentityAlreadyOwnedByAnotherUser()
    {
        await using var factory = new IdentitySpikeFactory();
        using var client = factory.CreateBrowserClient();
        await CreateSessionAsync(client, "many", OrganizationB);
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        var firstAttempt = await CreateLinkAttemptAsync(client, $"first-{Guid.NewGuid():N}", antiforgeryToken);
        var secondAttempt = await CreateLinkAttemptAsync(client, $"second-{Guid.NewGuid():N}", antiforgeryToken);
        var currentProof = await IssueProofAsync(
            client,
            "https://fake-idp.invalid/google",
            "user-shared",
            antiforgeryToken);

        using (var consumed = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{firstAttempt}/reauthenticate-current",
            new { proofId = currentProof },
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.OK, consumed.StatusCode);
        }

        using (var replayed = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{secondAttempt}/reauthenticate-current",
            new { proofId = currentProof },
            antiforgeryToken))
        {
            await AssertProblemAsync(replayed, HttpStatusCode.Conflict, "reauthentication-replayed");
        }

        var conflictAttempt = await CreateLinkAttemptAsync(
            client,
            "already-linked",
            antiforgeryToken,
            "https://fake-idp.invalid/email");
        var newCurrentProof = await IssueProofAsync(
            client,
            "https://fake-idp.invalid/google",
            "user-shared",
            antiforgeryToken);
        using (var current = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{conflictAttempt}/reauthenticate-current",
            new { proofId = newCurrentProof },
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.OK, current.StatusCode);
        }

        var conflictProof = await IssueProofAsync(
            client,
            "https://fake-idp.invalid/email",
            "already-linked",
            antiforgeryToken);
        using (var candidate = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{conflictAttempt}/reauthenticate-candidate",
            new { proofId = conflictProof },
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.OK, candidate.StatusCode);
        }

        using var conflict = await PostWithAntiforgeryAsync(
            client,
            $"/api/spike/link-attempts/{conflictAttempt}/complete",
            new { },
            antiforgeryToken);
        await AssertProblemAsync(conflict, HttpStatusCode.Conflict, "identity-link-conflict");
    }

    [TestMethod]
    public async Task RecoveryStartIsEnumerationSafeAndCompletionRevokesExistingSessions()
    {
        await using var factory = new IdentitySpikeFactory();
        using var validClient = factory.CreateBrowserClient();
        var oldSession = await CreateSessionAsync(validClient, "one");

        using var valid = await validClient.PostAsJsonAsync(
            "/api/spike/recovery/start",
            new { email = "user-a@example.invalid" });
        using var unknown = await validClient.PostAsJsonAsync(
            "/api/spike/recovery/start",
            new { email = "unknown@example.invalid" });
        using var malformed = await validClient.PostAsJsonAsync(
            "/api/spike/recovery/start",
            new { email = "not-an-email" });

        Assert.AreEqual(HttpStatusCode.Accepted, valid.StatusCode);
        Assert.AreEqual(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.AreEqual(HttpStatusCode.Accepted, malformed.StatusCode);
        Assert.AreEqual(await valid.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.AreEqual(await valid.Content.ReadAsStringAsync(), await malformed.Content.ReadAsStringAsync());

        for (var requestNumber = 0; requestNumber < 3; requestNumber++)
        {
            using var limited = await validClient.PostAsJsonAsync(
                "/api/spike/recovery/start",
                new { email = "rate-limit@example.invalid" });
            Assert.AreEqual(HttpStatusCode.Accepted, limited.StatusCode);
        }

        using (var rateLimited = await validClient.PostAsJsonAsync(
            "/api/spike/recovery/start",
            new { email = "rate-limit@example.invalid" }))
        {
            Assert.AreEqual(HttpStatusCode.Accepted, rateLimited.StatusCode);
        }

        using (var globalEvents = await validClient.GetAsync("/__fixtures/audit-events/global"))
        {
            Assert.AreEqual(HttpStatusCode.OK, globalEvents.StatusCode);
            using var eventsDocument = await ReadJsonAsync(globalEvents);
            Assert.IsTrue(eventsDocument.RootElement.EnumerateArray().Any(
                item => item.GetProperty("reasonCode").GetString() == "rate_limited"));
        }

        var challengeId = await IssueRecoveryChallengeAsync(validClient, OneOrganizationUser, 120);

        using var completed = await validClient.PostAsJsonAsync(
            "/__fixtures/recovery/complete",
            new { userId = OneOrganizationUser, challengeId });
        Assert.AreEqual(HttpStatusCode.OK, completed.StatusCode);

        using (var replay = await validClient.PostAsJsonAsync(
            "/__fixtures/recovery/complete",
            new { userId = OneOrganizationUser, challengeId }))
        {
            await AssertProblemAsync(
                replay,
                HttpStatusCode.Conflict,
                "recovery-challenge-replayed");
        }

        var expiredChallengeId = await IssueRecoveryChallengeAsync(validClient, OneOrganizationUser, 0);
        using (var expired = await validClient.PostAsJsonAsync(
            "/__fixtures/recovery/complete",
            new { userId = OneOrganizationUser, challengeId = expiredChallengeId }))
        {
            await AssertProblemAsync(
                expired,
                HttpStatusCode.Gone,
                "recovery-challenge-expired");
        }

        using var oldSessionClient = factory.CreateBrowserClient();
        using var oldRequest = new HttpRequestMessage(HttpMethod.Get, "/api/spike/session");
        oldRequest.Headers.TryAddWithoutValidation("Cookie", $"__Host-agro-dis003-session={oldSession}");
        using var revoked = await oldSessionClient.SendAsync(oldRequest);
        await AssertJsonAsync(revoked, HttpStatusCode.OK, "kind", "revoked");
    }

    private static async Task<string> CreateSessionAsync(
        HttpClient client,
        string scenario,
        string? selectedOrganizationId = null,
        bool stepUpExpired = false)
    {
        using var response = await client.PostAsJsonAsync(
            "/__fixtures/sessions",
            new { scenario, selectedOrganizationId, stepUpExpired });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        return document.RootElement.GetProperty("sessionId").GetString()
            ?? throw new AssertFailedException("Fixture did not return a session ID.");
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/spike/antiforgery");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        return document.RootElement.GetProperty("requestToken").GetString()
            ?? throw new AssertFailedException("Antiforgery endpoint did not return a token.");
    }

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync(
        HttpClient client,
        string path,
        object body,
        string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    private static async Task<string> CreateLinkAttemptAsync(
        HttpClient client,
        string subject,
        string token,
        string issuer = "https://fake-idp.invalid/passkey")
    {
        using var response = await PostWithAntiforgeryAsync(
            client,
            "/api/spike/link-attempts",
            new { issuer, subject },
            token);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        return document.RootElement.GetProperty("attemptId").GetString()
            ?? throw new AssertFailedException("Link attempt did not return an ID.");
    }

    private static async Task<string> IssueProofAsync(
        HttpClient client,
        string issuer,
        string subject,
        string token)
    {
        using var response = await PostWithAntiforgeryAsync(
            client,
            "/__fixtures/reauthentication-proofs",
            new { issuer, subject },
            token);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        return document.RootElement.GetProperty("proofId").GetString()
            ?? throw new AssertFailedException("Fixture did not return a proof ID.");
    }

    private static async Task<string> IssueRecoveryChallengeAsync(
        HttpClient client,
        string userId,
        int expiresInSeconds)
    {
        using var response = await client.PostAsJsonAsync(
            "/__fixtures/recovery/challenges",
            new { userId, expiresInSeconds });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        return document.RootElement.GetProperty("challengeId").GetString()
            ?? throw new AssertFailedException("Fixture did not return a recovery challenge ID.");
    }

    private static async Task AssertJsonAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string propertyName,
        string expectedValue)
    {
        Assert.AreEqual(expectedStatus, response.StatusCode);
        using var document = await ReadJsonAsync(response);
        Assert.AreEqual(expectedValue, document.RootElement.GetProperty(propertyName).GetString());
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.AreEqual(expectedStatus, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = await ReadJsonAsync(response);
        Assert.AreEqual(expectedCode, document.RootElement.GetProperty("code").GetString());
        Assert.IsTrue(document.RootElement.TryGetProperty("correlationId", out _));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static async Task SetSharedMembershipBStateAsync(
        string ownerConnectionString,
        bool isActive,
        int securityVersion)
    {
        await using var dataSource = NpgsqlDataSource.Create(ownerConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            update identity_spike.membership
            set is_active = @is_active,
                security_version = @security_version
            where id = 'a2222222-2222-4222-8222-222222222222'
            """);
        command.Parameters.AddWithValue("is_active", isActive);
        command.Parameters.AddWithValue("security_version", securityVersion);
        Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
    }
}

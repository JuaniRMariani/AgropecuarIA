using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrganizationCreateApiIntegrationTests
{
    private const string Endpoint = "/api/identity/organizations";
    private static readonly string[] OrganizationCreatedPayloadProperties =
        ["organizationId", "ownerMembershipId", "createdAtUtc"];

    [TestMethod]
    public async Task CreatePersistsOwnerProtocolJournalOutboxAndSessionMembership()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        using HttpResponseMessage response = await browser.PostWithIdempotencyKeyAsync(
            Endpoint,
            new { displayName = "  Estancia Cafe\u0301  " },
            antiforgery,
            "organization-create-0001");

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNull(response.Headers.Location);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument json = await ParseAsync(response);
        JsonElement organization = json.RootElement.GetProperty("organization");
        JsonElement membership = json.RootElement.GetProperty("membership");
        Guid organizationId = organization.GetProperty("organizationId").GetGuid();
        Guid membershipId = membership.GetProperty("membershipId").GetGuid();
        Assert.AreEqual("Estancia Caf\u00e9", organization.GetProperty("displayName").GetString());
        Assert.AreEqual("active", organization.GetProperty("status").GetString());
        Assert.AreEqual("owner", membership.GetProperty("role").GetString());
        Assert.AreEqual("active", membership.GetProperty("status").GetString());
        Assert.AreEqual(1, membership.GetProperty("authorizationVersion").GetInt64());

        await using NpgsqlConnection connection = new(scenario.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM identity.organizations WHERE "Id" = @organization_id),
                (SELECT COUNT(*) FROM identity.memberships WHERE "Id" = @membership_id AND "OrganizationId" = @organization_id AND "Role" = 'owner' AND "Status" = 'active'),
                (SELECT COUNT(*) FROM identity.organization_memberships WHERE "OrganizationId" = @organization_id AND "Role" = 'owner'),
                (SELECT COUNT(*) FROM identity.organization_creation_ledgers WHERE "OrganizationId" = @organization_id AND "MembershipId" = @membership_id AND "State" = 'succeeded'),
                (SELECT COUNT(*) FROM identity.organization_creation_key_aliases aliases JOIN identity.organization_creation_ledgers ledgers ON ledgers."Id" = aliases."LedgerId" WHERE ledgers."OrganizationId" = @organization_id),
                (SELECT COUNT(*) FROM identity.audit_events WHERE "Action" = 'organization_created' AND "Outcome" = 'succeeded'),
                (SELECT COUNT(*) FROM identity.outbox_messages WHERE "Type" = 'OrganizationCreated' AND "AggregateId" = @organization_id),
                (SELECT "Payload"::text FROM identity.outbox_messages WHERE "Type" = 'OrganizationCreated' AND "AggregateId" = @organization_id)
            """;
        command.Parameters.AddWithValue("organization_id", organizationId);
        command.Parameters.AddWithValue("membership_id", membershipId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        for (int index = 0; index < 7; index++)
        {
            Assert.AreEqual(1L, reader.GetInt64(index), $"Persistence assertion {index} failed.");
        }

        using JsonDocument payload = JsonDocument.Parse(reader.GetString(7));
        CollectionAssert.AreEquivalent(
            OrganizationCreatedPayloadProperties,
            payload.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual(organizationId, payload.RootElement.GetProperty("organizationId").GetGuid());
        Assert.AreEqual(membershipId, payload.RootElement.GetProperty("ownerMembershipId").GetGuid());

        using JsonDocument session = await IdentityApiTestActions.GetSessionAsync(browser);
        JsonElement sessionMembership = session.RootElement.GetProperty("memberships")
            .EnumerateArray()
            .Single(item => item.GetProperty("organizationId").GetGuid() == organizationId);
        Assert.AreEqual("Estancia Caf\u00e9", sessionMembership.GetProperty("organizationName").GetString());
        Assert.AreEqual("owner", sessionMembership.GetProperty("role").GetString());
    }

    [TestMethod]
    public async Task SameKeyReplaysOnceMismatchConflictsAndDuplicateNamesAreAllowed()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        const string key = "organization-create-replay-01";

        using HttpResponseMessage first = await CreateAsync(browser, antiforgery, key, "Los Aromos");
        using HttpResponseMessage replay = await CreateAsync(browser, antiforgery, key, "Los Aromos");
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, replay.StatusCode);
        using JsonDocument firstJson = await ParseAsync(first);
        using JsonDocument replayJson = await ParseAsync(replay);
        Assert.AreEqual(firstJson.RootElement.GetRawText(), replayJson.RootElement.GetRawText());

        using HttpResponseMessage mismatch = await CreateAsync(browser, antiforgery, key, "Otro nombre");
        Assert.AreEqual(HttpStatusCode.Conflict, mismatch.StatusCode);
        await AssertProblemCodeAsync(mismatch, "idempotency.key_reused");

        using HttpResponseMessage duplicateName = await CreateAsync(
            browser,
            antiforgery,
            "organization-create-replay-02",
            "Los Aromos");
        Assert.AreEqual(HttpStatusCode.Created, duplicateName.StatusCode);
        using JsonDocument duplicateNameJson = await ParseAsync(duplicateName);
        Guid[] expectedMembershipOrder =
        [
            firstJson.RootElement.GetProperty("organization").GetProperty("organizationId").GetGuid(),
            duplicateNameJson.RootElement.GetProperty("organization").GetProperty("organizationId").GetGuid(),
        ];
        Array.Sort(expectedMembershipOrder);

        using JsonDocument session = await IdentityApiTestActions.GetSessionAsync(browser);
        Guid[] actualMembershipOrder = session.RootElement.GetProperty("memberships")
            .EnumerateArray()
            .Where(item => item.GetProperty("organizationName").GetString() == "Los Aromos")
            .Select(item => item.GetProperty("organizationId").GetGuid())
            .ToArray();
        CollectionAssert.AreEqual(expectedMembershipOrder, actualMembershipOrder);

        await using NpgsqlConnection connection = new(scenario.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT
                (SELECT COUNT(*) FROM identity.organizations WHERE "DisplayName" = 'Los Aromos'),
                (SELECT COUNT(*) FROM identity.organization_creation_ledgers),
                (SELECT COUNT(*) FROM identity.outbox_messages WHERE "Type" = 'OrganizationCreated')
            """,
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(2L, reader.GetInt64(0));
        Assert.AreEqual(2L, reader.GetInt64(1));
        Assert.AreEqual(2L, reader.GetInt64(2));
    }

    [TestMethod]
    public async Task CreateFailsClosedForCsrfKeyPayloadStaleOrUnverifiedSession()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        using HttpResponseMessage missingCsrf = await browser.PostWithIdempotencyKeyAsync(
            Endpoint,
            new { displayName = "La Esperanza" },
            null,
            "organization-create-security-01");
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        using HttpResponseMessage missingKey = await browser.PostAsync(
            Endpoint,
            new { displayName = "La Esperanza" },
            antiforgery);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingKey.StatusCode);
        await AssertProblemCodeAsync(missingKey, "identity.invalid_idempotency_key");

        using HttpResponseMessage invalidName = await CreateAsync(
            browser,
            antiforgery,
            "organization-create-security-02",
            " x ");
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidName.StatusCode);
        await AssertProblemCodeAsync(invalidName, "identity.invalid_organization_display_name");

        await SetSessionAssuranceAsync(scenario.ConnectionString, verified: false, age: TimeSpan.Zero);
        using HttpResponseMessage unverified = await CreateAsync(
            browser,
            antiforgery,
            "organization-create-security-03",
            "La Esperanza");
        Assert.AreEqual(HttpStatusCode.Forbidden, unverified.StatusCode);
        await AssertProblemCodeAsync(unverified, "identity.reauthentication_required");

        await SetSessionAssuranceAsync(scenario.ConnectionString, verified: true, age: TimeSpan.FromHours(1));
        using HttpResponseMessage stale = await CreateAsync(
            browser,
            antiforgery,
            "organization-create-security-04",
            "La Esperanza");
        Assert.AreEqual(HttpStatusCode.Forbidden, stale.StatusCode);
        await AssertProblemCodeAsync(stale, "identity.reauthentication_required");
    }

    [TestMethod]
    public async Task SameKeyFromAnotherActorIsANeutralConflictAndCreatesNoEffect()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        using BrowserSession operatorBrowser = scenario.CreateBrowser();
        string ownerAntiforgery = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        string operatorAntiforgery = await IdentityApiTestActions.SignInAsync(
            operatorBrowser,
            "identity-owned-by-another-user");
        const string key = "organization-create-actor-bound";

        using HttpResponseMessage created = await CreateAsync(owner, ownerAntiforgery, key, "El Ombu");
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        using HttpResponseMessage crossActor = await CreateAsync(
            operatorBrowser,
            operatorAntiforgery,
            key,
            "El Ombu");
        Assert.AreEqual(HttpStatusCode.Conflict, crossActor.StatusCode);
        await AssertProblemCodeAsync(crossActor, "idempotency.key_reused");

        await using NpgsqlConnection connection = new(scenario.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            "SELECT COUNT(*) FROM identity.organizations",
            connection);
        Assert.AreEqual(1L, (long)(await command.ExecuteScalarAsync())!);
    }

    [TestMethod]
    public async Task ConcurrentSameRequestConvergesOnOneOrganizationAndOneReplay()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession original = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(original, "email-owner");
        Dictionary<string, string> cookies = original.Cookies.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.Ordinal);
        using BrowserSession firstBrowser = scenario.CreateBrowser(cookies);
        using BrowserSession secondBrowser = scenario.CreateBrowser(cookies);

        Task<HttpResponseMessage> firstTask = CreateAsync(
            firstBrowser,
            antiforgery,
            "organization-create-concurrent",
            "La Concurrencia");
        Task<HttpResponseMessage> secondTask = CreateAsync(
            secondBrowser,
            antiforgery,
            "organization-create-concurrent",
            "La Concurrencia");
        HttpResponseMessage[] responses = await Task.WhenAll(firstTask, secondTask);
        using HttpResponseMessage first = responses[0];
        using HttpResponseMessage second = responses[1];
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, second.StatusCode);
        using JsonDocument firstJson = await ParseAsync(first);
        using JsonDocument secondJson = await ParseAsync(second);
        Assert.AreEqual(firstJson.RootElement.GetRawText(), secondJson.RootElement.GetRawText());

        await using NpgsqlConnection connection = new(scenario.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT
                (SELECT COUNT(*) FROM identity.organizations),
                (SELECT COUNT(*) FROM identity.organization_creation_ledgers),
                (SELECT COUNT(*) FROM identity.outbox_messages WHERE "Type" = 'OrganizationCreated')
            """,
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(1L, reader.GetInt64(0));
        Assert.AreEqual(1L, reader.GetInt64(1));
        Assert.AreEqual(1L, reader.GetInt64(2));
    }

    [TestMethod]
    public async Task InProgressReplayReturnsRetryableConflictAndRetryAfter()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        const string idempotencyKey = "organization-create-in-progress";
        const string displayName = "El Progreso";

        await SeedInProgressLedgerAsync(
            scenario.ConnectionString,
            idempotencyKey,
            displayName);

        using HttpResponseMessage response = await CreateAsync(
            browser,
            antiforgery,
            idempotencyKey,
            displayName);
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.IsTrue(response.Headers.TryGetValues("Retry-After", out IEnumerable<string>? values));
        Assert.AreEqual("1", values.Single());
        using JsonDocument problem = await ParseAsync(response);
        Assert.AreEqual("idempotency.in_progress", problem.RootElement.GetProperty("code").GetString());
        Assert.IsTrue(problem.RootElement.GetProperty("retryable").GetBoolean());
    }

    [TestMethod]
    public async Task AuditFailureRollsBackEveryProducerSideEffect()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        await using (NpgsqlConnection connection = new(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand trigger = new(
                """
                CREATE FUNCTION identity.test_reject_organization_audit()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $failure$
                BEGIN
                    IF NEW."Action" = 'organization_created' THEN
                        RAISE EXCEPTION 'injected organization audit failure';
                    END IF;
                    RETURN NEW;
                END
                $failure$;
                CREATE TRIGGER test_reject_organization_audit
                    BEFORE INSERT ON identity.audit_events
                    FOR EACH ROW
                    EXECUTE FUNCTION identity.test_reject_organization_audit();
                """,
                connection);
            await trigger.ExecuteNonQueryAsync();
        }

        using HttpResponseMessage response = await CreateAsync(
            browser,
            antiforgery,
            "organization-create-audit-failure",
            "El Retorno");
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertProblemCodeAsync(response, "server.unexpected");

        await using NpgsqlConnection verification = new(scenario.ConnectionString);
        await verification.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT
                (SELECT COUNT(*) FROM identity.organizations),
                (SELECT COUNT(*) FROM identity.memberships),
                (SELECT COUNT(*) FROM identity.organization_memberships),
                (SELECT COUNT(*) FROM identity.organization_creation_ledgers),
                (SELECT COUNT(*) FROM identity.organization_creation_key_aliases),
                (SELECT COUNT(*) FROM identity.audit_events WHERE "Action" = 'organization_created'),
                (SELECT COUNT(*) FROM identity.outbox_messages WHERE "Type" = 'OrganizationCreated')
            """,
            verification);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        for (int index = 0; index < 7; index++)
        {
            Assert.AreEqual(0L, reader.GetInt64(index), $"Rollback assertion {index} failed.");
        }
    }

    [TestMethod]
    public async Task FailedTerminalReplayReturnsExactConflictWithoutBusinessEffect()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        const string key = "organization-failed-terminal-01";
        await SeedLedgerWithoutEffectAsync(
            scenario.ConnectionString,
            key,
            "El Desvío",
            "failed_terminal");

        using HttpResponseMessage response = await CreateAsync(
            browser,
            antiforgery,
            key,
            "El Desvío");

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "idempotency.failed_terminal");
        await using NpgsqlConnection verification = new(scenario.ConnectionString);
        await verification.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT
                (SELECT COUNT(*) FROM identity.organizations) = 0
                AND (SELECT COUNT(*) FROM identity.memberships) = 0
                AND (SELECT COUNT(*) FROM identity.organization_creation_ledgers) = 1
                AND (SELECT COUNT(*) FROM identity.organization_creation_key_aliases) = 1
                AND (SELECT COUNT(*) FROM identity.audit_events WHERE "Action" = 'organization_created') = 0
                AND (SELECT COUNT(*) FROM identity.outbox_messages WHERE "Type" = 'OrganizationCreated') = 0
            """,
            verification);
        Assert.IsTrue((bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The failed terminal probe returned null.")));
    }

    [TestMethod]
    public async Task ResponseExpiredReplayRequiresReconciliationWithoutRepeatingTheEffect()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        const string key = "organization-response-expired-01";
        using HttpResponseMessage initial = await CreateAsync(
            browser,
            antiforgery,
            key,
            "La Respuesta");
        Assert.AreEqual(HttpStatusCode.Created, initial.StatusCode);

        await using (NpgsqlConnection connection = new(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand expire = new(
                """
                UPDATE identity.organization_creation_ledgers
                SET "State" = 'response_expired', "Version" = gen_random_uuid()
                WHERE "State" = 'succeeded'
                """,
                connection);
            Assert.AreEqual(1, await expire.ExecuteNonQueryAsync());
        }

        using HttpResponseMessage replay = await CreateAsync(
            browser,
            antiforgery,
            key,
            "La Respuesta");
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, replay.StatusCode);
        Assert.AreEqual("1", replay.Headers.RetryAfter?.ToString());
        using JsonDocument problem = await ParseAsync(replay);
        Assert.AreEqual(
            "idempotency.reconciliation_required",
            problem.RootElement.GetProperty("code").GetString());
        Assert.IsTrue(problem.RootElement.GetProperty("retryable").GetBoolean());

        await using NpgsqlConnection verification = new(scenario.ConnectionString);
        await verification.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT
                (SELECT COUNT(*) FROM identity.organizations) = 1
                AND (SELECT COUNT(*) FROM identity.memberships) = 1
                AND (SELECT COUNT(*) FROM identity.organization_creation_ledgers) = 1
                AND (SELECT COUNT(*) FROM identity.organization_creation_key_aliases) = 1
                AND (SELECT COUNT(*) FROM identity.audit_events WHERE "Action" = 'organization_created') = 1
                AND (SELECT COUNT(*) FROM identity.outbox_messages WHERE "Type" = 'OrganizationCreated') = 1
            """,
            verification);
        Assert.IsTrue((bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The response-expired probe returned null.")));
    }

    private static Task<HttpResponseMessage> CreateAsync(
        BrowserSession browser,
        string antiforgery,
        string idempotencyKey,
        string displayName) =>
        browser.PostWithIdempotencyKeyAsync(
            Endpoint,
            new { displayName },
            antiforgery,
            idempotencyKey);

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

    private static async Task AssertProblemCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        using JsonDocument problem = await ParseAsync(response);
        Assert.AreEqual(expectedCode, problem.RootElement.GetProperty("code").GetString());
    }

    private static async Task SetSessionAssuranceAsync(
        string connectionString,
        bool verified,
        TimeSpan age)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            UPDATE identity.sessions
            SET "IsAuthenticationAssuranceVerified" = @verified,
                "AuthenticatedAtUtc" = @authenticated_at
            WHERE "RevokedAtUtc" IS NULL
            """,
            connection);
        command.Parameters.AddWithValue("verified", verified);
        command.Parameters.AddWithValue("authenticated_at", DateTimeOffset.UtcNow.Subtract(age));
        Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
    }

    private static Task SeedInProgressLedgerAsync(
        string connectionString,
        string idempotencyKey,
        string displayName) =>
        SeedLedgerWithoutEffectAsync(
            connectionString,
            idempotencyKey,
            displayName,
            "in_progress");

    private static async Task SeedLedgerWithoutEffectAsync(
        string connectionString,
        string idempotencyKey,
        string displayName,
        string state)
    {
        if (state is not ("in_progress" or "failed_terminal"))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        Guid userId;
        Guid sessionId;
        Guid authorizationVersion;
        await using (NpgsqlCommand sessionCommand = new(
            """
            SELECT "UserId", "Id", "Version"
            FROM identity.sessions
            WHERE "RevokedAtUtc" IS NULL
            LIMIT 1
            """,
            connection))
        await using (NpgsqlDataReader reader = await sessionCommand.ExecuteReaderAsync())
        {
            Assert.IsTrue(await reader.ReadAsync());
            userId = reader.GetGuid(0);
            sessionId = reader.GetGuid(1);
            authorizationVersion = reader.GetGuid(2);
        }

        byte[] hmacKey = Convert.FromBase64String(
            "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=");
        byte[] keyDigest = HMACSHA256.HashData(hmacKey, Encoding.ASCII.GetBytes(idempotencyKey));
        string canonicalRequest = string.Join(
            '|',
            "create-organization-v1",
            userId.ToString("D"),
            sessionId.ToString("D"),
            authorizationVersion.ToString("D"),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(displayName)));
        byte[] fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest));
        Guid ledgerId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SET LOCAL ROLE agro_identity_app;
            SELECT set_config('app.current_actor_id', @actor_id, true);
            SELECT set_config('app.current_scope_kind', 'platform', true);
            INSERT INTO identity.organization_creation_ledgers
                ("Id", "ScopeKind", "Namespace", "Operation", "ContractVersion",
                 "CanonicalizationVersion", "ActorUserId", "SessionId", "AuthorizationVersion",
                 "RequestFingerprint", "State", "OrganizationId", "MembershipId", "LeaseOwner",
                 "FenceToken", "LeaseUntilUtc", "StartedAtUtc", "CompletedAtUtc", "Version")
            VALUES
                (@ledger_id, 'platform', 'organization-bootstrap', 'create_organization', 1,
                 1, @actor_uuid, @session_id, @authorization_version,
                 @fingerprint, @state, NULL, NULL, @lease_owner,
                 1, @lease_until, @started_at,
                 CASE WHEN @state = 'failed_terminal' THEN @started_at ELSE NULL END,
                 @version);
            INSERT INTO identity.organization_creation_key_aliases
                ("Id", "LedgerId", "ScopeKind", "Namespace", "Operation", "KeyVersion",
                 "KeyDigest", "CreatedAtUtc")
            VALUES
                (@alias_id, @ledger_id, 'platform', 'organization-bootstrap', 'create_organization',
                 'test-v1', @key_digest, @started_at);
            """;
        command.Parameters.AddWithValue("actor_id", userId.ToString("D"));
        command.Parameters.AddWithValue("actor_uuid", userId);
        command.Parameters.AddWithValue("session_id", sessionId);
        command.Parameters.AddWithValue("authorization_version", authorizationVersion);
        command.Parameters.AddWithValue("fingerprint", fingerprint);
        command.Parameters.AddWithValue("state", state);
        command.Parameters.AddWithValue("ledger_id", ledgerId);
        command.Parameters.AddWithValue("lease_owner", Guid.NewGuid());
        command.Parameters.AddWithValue("lease_until", now.AddMinutes(1));
        command.Parameters.AddWithValue("started_at", now);
        command.Parameters.AddWithValue("version", Guid.NewGuid());
        command.Parameters.AddWithValue("alias_id", Guid.NewGuid());
        command.Parameters.AddWithValue("key_digest", keyDigest);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
}

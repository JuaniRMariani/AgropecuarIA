using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OwnSessionInventoryApiTests
{
    [TestMethod]
    public async Task InventoryIsOwnActiveStableAndKeepsTotalOnAnEmptyPage()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession first = scenario.CreateBrowser();
        using BrowserSession second = scenario.CreateBrowser();
        using BrowserSession foreign = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(first, "email-owner");
        await IdentityApiTestActions.SignInAsync(second, "email-owner");
        await IdentityApiTestActions.SignInAsync(foreign, "identity-owned-by-another-user");

        using JsonDocument all = await GetInventoryAsync(first, offset: 0, limit: 50);
        JsonElement[] items = all.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.AreEqual(2L, all.RootElement.GetProperty("total").GetInt64());
        Assert.HasCount(2, items);
        Assert.AreEqual(1, items.Count(item => item.GetProperty("isCurrent").GetBoolean()));
        Assert.IsTrue(items.All(item => item.EnumerateObject().Select(property => property.Name)
            .SequenceEqual(
                ["sessionId", "authenticatedAtUtc", "expiresAtUtc", "isCurrent", "version"])),
            "Session inventory must remain closed and contain no token or device metadata.");

        using JsonDocument firstPage = await GetInventoryAsync(first, offset: 0, limit: 1);
        using JsonDocument secondPage = await GetInventoryAsync(first, offset: 1, limit: 1);
        Assert.AreEqual(
            items[0].GetProperty("sessionId").GetGuid(),
            firstPage.RootElement.GetProperty("items")[0].GetProperty("sessionId").GetGuid());
        Assert.AreEqual(
            items[1].GetProperty("sessionId").GetGuid(),
            secondPage.RootElement.GetProperty("items")[0].GetProperty("sessionId").GetGuid());

        using JsonDocument emptyPage = await GetInventoryAsync(first, offset: 200, limit: 20);
        Assert.AreEqual(2L, emptyPage.RootElement.GetProperty("total").GetInt64());
        Assert.AreEqual(0, emptyPage.RootElement.GetProperty("items").GetArrayLength());

        using JsonDocument foreignPage = await GetInventoryAsync(foreign, offset: 0, limit: 20);
        Guid foreignSessionId = foreignPage.RootElement.GetProperty("items")[0]
            .GetProperty("sessionId")
            .GetGuid();
        Assert.IsFalse(items.Any(item => item.GetProperty("sessionId").GetGuid() == foreignSessionId));

        await AssertProblemAsync(
            await first.GetAsync("/api/identity/sessions?offset=-1"),
            HttpStatusCode.BadRequest,
            "identity.invalid_session_page");
        await AssertProblemAsync(
            await first.GetAsync("/api/identity/sessions?limit=51"),
            HttpStatusCode.BadRequest,
            "identity.invalid_session_page");
    }

    [TestMethod]
    public async Task RevokeRequiresExactPurposeAndIsConcurrentReplaySafeJournalOnce()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession actor = scenario.CreateBrowser();
        using BrowserSession target = scenario.CreateBrowser();
        using BrowserSession expired = scenario.CreateBrowser();
        using BrowserSession foreign = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(actor, "email-owner");
        await IdentityApiTestActions.SignInAsync(target, "email-owner");
        await IdentityApiTestActions.SignInAsync(expired, "email-owner");
        await IdentityApiTestActions.SignInAsync(foreign, "identity-owned-by-another-user");

        OwnSession targetSession = await GetCurrentSessionAsync(target);
        OwnSession expiredSession = await GetCurrentSessionAsync(expired);
        OwnSession foreignSession = await GetCurrentSessionAsync(foreign);

        using (HttpResponseMessage missingCsrf = await actor.DeleteWithConcurrencyAsync(
            $"/api/identity/sessions/{targetSession.SessionId:D}",
            antiforgeryToken: null,
            ifMatch: Quote(targetSession.Version),
            idempotencyKey: null))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        }

        await AssertProblemAsync(
            await actor.DeleteWithConcurrencyAsync(
                $"/api/identity/sessions/{targetSession.SessionId:D}",
                antiforgery,
                Quote(targetSession.Version),
                idempotencyKey: null),
            HttpStatusCode.Forbidden,
            "identity.strong_authentication_required");

        antiforgery = await CompleteManageSessionsStepUpAsync(actor, antiforgery);
        OwnSession current = await GetCurrentSessionAsync(actor);

        await AssertProblemAsync(
            await actor.DeleteWithConcurrencyAsync(
                $"/api/identity/sessions/{current.SessionId:D}",
                antiforgery,
                Quote(current.Version),
                idempotencyKey: null),
            HttpStatusCode.Conflict,
            "identity.current_session_requires_logout");
        await AssertProblemAsync(
            await actor.DeleteWithConcurrencyAsync(
                $"/api/identity/sessions/{foreignSession.SessionId:D}",
                antiforgery,
                Quote(foreignSession.Version),
                idempotencyKey: null),
            HttpStatusCode.NotFound,
            "identity.session_not_available");
        await AssertProblemAsync(
            await actor.DeleteWithConcurrencyAsync(
                $"/api/identity/sessions/{targetSession.SessionId:D}",
                antiforgery,
                Quote(Guid.NewGuid()),
                idempotencyKey: null),
            HttpStatusCode.PreconditionFailed,
            "identity.session_version_mismatch");

        await ExpireSessionAsync(scenario.ConnectionString, expiredSession.SessionId);
        await AssertProblemAsync(
            await actor.DeleteWithConcurrencyAsync(
                $"/api/identity/sessions/{expiredSession.SessionId:D}",
                antiforgery,
                Quote(expiredSession.Version),
                idempotencyKey: null),
            HttpStatusCode.NotFound,
            "identity.session_not_available");

        IReadOnlyDictionary<string, string> actorCookies = actor.Cookies.ToDictionary(
            cookie => cookie.Key,
            cookie => cookie.Value,
            StringComparer.Ordinal);
        using BrowserSession concurrent = scenario.CreateBrowser(actorCookies);
        Task<HttpResponseMessage> firstRevoke = actor.DeleteWithConcurrencyAsync(
            $"/api/identity/sessions/{targetSession.SessionId:D}",
            antiforgery,
            Quote(targetSession.Version),
            idempotencyKey: null);
        Task<HttpResponseMessage> secondRevoke = concurrent.DeleteWithConcurrencyAsync(
            $"/api/identity/sessions/{targetSession.SessionId:D}",
            antiforgery,
            Quote(targetSession.Version),
            idempotencyKey: null);
        HttpResponseMessage[] revocations = await Task.WhenAll(firstRevoke, secondRevoke);
        try
        {
            Assert.IsTrue(revocations.All(
                response => response.StatusCode == HttpStatusCode.NoContent));
        }
        finally
        {
            foreach (HttpResponseMessage response in revocations)
            {
                response.Dispose();
            }
        }

        using (HttpResponseMessage replay = await actor.DeleteWithConcurrencyAsync(
            $"/api/identity/sessions/{targetSession.SessionId:D}",
            antiforgery,
            Quote(targetSession.Version),
            idempotencyKey: null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, replay.StatusCode);
        }

        using (HttpResponseMessage revokedTarget = await target.GetAsync("/api/identity/session"))
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, revokedTarget.StatusCode);
        }

        Assert.AreEqual(
            1L,
            await CountSuccessfulRevocationsAsync(
                scenario.ConnectionString,
                targetSession.SessionId));
    }

    [TestMethod]
    public async Task OtherSupportedStepUpPurposesCannotRevokeAnotherSession()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession actor = scenario.CreateBrowser();
        using BrowserSession target = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(actor, "email-owner");
        await IdentityApiTestActions.SignInAsync(target, "email-owner");
        OwnSession targetSession = await GetCurrentSessionAsync(target);

        string[] wrongPurposes =
        [
            StepUpPurposes.ManageOrganizationOwners,
            StepUpPurposes.ManageAuthenticationMethods,
        ];
        foreach (string purpose in wrongPurposes)
        {
            antiforgery = await CompleteStepUpAsync(actor, antiforgery, purpose);
            await AssertProblemAsync(
                await actor.DeleteWithConcurrencyAsync(
                    $"/api/identity/sessions/{targetSession.SessionId:D}",
                    antiforgery,
                    Quote(targetSession.Version),
                    idempotencyKey: null),
                HttpStatusCode.Forbidden,
                "identity.strong_authentication_required");
            await AssertProblemAsync(
                await actor.DeleteWithConcurrencyAsync(
                    "/api/identity/sessions/others",
                    antiforgery,
                    ifMatch: null,
                    idempotencyKey: null),
                HttpStatusCode.Forbidden,
                "identity.strong_authentication_required");

            SessionPersistenceState state = await ReadSessionPersistenceStateAsync(
                scenario.ConnectionString,
                targetSession.SessionId);
            Assert.IsNull(state.RevokedAtUtc);
            Assert.AreEqual(targetSession.Version, state.Version);
            Assert.AreEqual(0L, state.SuccessfulJournalCount);
        }
    }

    [TestMethod]
    public async Task AuditJournalFailureReturnsUnavailableAndRollsBackRevocation()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession actor = scenario.CreateBrowser();
        using BrowserSession target = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(actor, "email-owner");
        await IdentityApiTestActions.SignInAsync(target, "email-owner");
        OwnSession targetSession = await GetCurrentSessionAsync(target);
        antiforgery = await CompleteManageSessionsStepUpAsync(actor, antiforgery);

        await InstallFailingRevocationJournalTriggerAsync(scenario.ConnectionString);
        try
        {
            await AssertProblemAsync(
                await actor.DeleteWithConcurrencyAsync(
                    $"/api/identity/sessions/{targetSession.SessionId:D}",
                    antiforgery,
                    Quote(targetSession.Version),
                    idempotencyKey: null),
                HttpStatusCode.ServiceUnavailable,
                "identity.session_management_unavailable");
        }
        finally
        {
            await RemoveFailingRevocationJournalTriggerAsync(scenario.ConnectionString);
        }

        SessionPersistenceState state = await ReadSessionPersistenceStateAsync(
            scenario.ConnectionString,
            targetSession.SessionId);
        Assert.IsNull(state.RevokedAtUtc, "The target session revocation must roll back.");
        Assert.AreEqual(
            targetSession.Version,
            state.Version,
            "The target session version must roll back with the revocation.");
        Assert.AreEqual(
            0L,
            state.SuccessfulJournalCount,
            "The failed journal insert must not leave a partial audit record.");
        using HttpResponseMessage targetStillAuthenticated = await target.GetAsync(
            "/api/identity/session");
        Assert.AreEqual(HttpStatusCode.OK, targetStillAuthenticated.StatusCode);
    }

    [TestMethod]
    public async Task RevokeAllOthersIsAtomicIdempotentAndKeepsCurrentAndForeignSessions()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession actor = scenario.CreateBrowser();
        using BrowserSession firstTarget = scenario.CreateBrowser();
        using BrowserSession secondTarget = scenario.CreateBrowser();
        using BrowserSession foreign = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(actor, "email-owner");
        await IdentityApiTestActions.SignInAsync(firstTarget, "email-owner");
        await IdentityApiTestActions.SignInAsync(secondTarget, "email-owner");
        await IdentityApiTestActions.SignInAsync(foreign, "identity-owned-by-another-user");
        OwnSession firstTargetSession = await GetCurrentSessionAsync(firstTarget);
        OwnSession secondTargetSession = await GetCurrentSessionAsync(secondTarget);
        OwnSession foreignSession = await GetCurrentSessionAsync(foreign);

        using (HttpResponseMessage missingCsrf = await actor.DeleteWithConcurrencyAsync(
            "/api/identity/sessions/others",
            antiforgeryToken: null,
            ifMatch: null,
            idempotencyKey: null))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        }

        await AssertProblemAsync(
            await actor.DeleteWithConcurrencyAsync(
                "/api/identity/sessions/others",
                antiforgery,
                ifMatch: null,
                idempotencyKey: null),
            HttpStatusCode.Forbidden,
            "identity.strong_authentication_required");

        antiforgery = await CompleteManageSessionsStepUpAsync(actor, antiforgery);
        using (HttpResponseMessage revoked = await actor.DeleteWithConcurrencyAsync(
            "/api/identity/sessions/others",
            antiforgery,
            ifMatch: null,
            idempotencyKey: null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, revoked.StatusCode);
            Assert.AreEqual(string.Empty, await revoked.Content.ReadAsStringAsync());
        }

        using (HttpResponseMessage replay = await actor.DeleteWithConcurrencyAsync(
            "/api/identity/sessions/others",
            antiforgery,
            ifMatch: null,
            idempotencyKey: null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, replay.StatusCode);
            Assert.AreEqual(string.Empty, await replay.Content.ReadAsStringAsync());
        }

        using HttpResponseMessage actorStillAuthenticated = await actor.GetAsync(
            "/api/identity/session");
        Assert.AreEqual(HttpStatusCode.OK, actorStillAuthenticated.StatusCode);
        using HttpResponseMessage firstTargetRevoked = await firstTarget.GetAsync(
            "/api/identity/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, firstTargetRevoked.StatusCode);
        using HttpResponseMessage secondTargetRevoked = await secondTarget.GetAsync(
            "/api/identity/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, secondTargetRevoked.StatusCode);
        using HttpResponseMessage foreignStillAuthenticated = await foreign.GetAsync(
            "/api/identity/session");
        Assert.AreEqual(HttpStatusCode.OK, foreignStillAuthenticated.StatusCode);

        using JsonDocument inventory = await GetInventoryAsync(actor, offset: 0, limit: 50);
        Assert.AreEqual(1L, inventory.RootElement.GetProperty("total").GetInt64());
        Assert.IsTrue(inventory.RootElement.GetProperty("items")[0]
            .GetProperty("isCurrent")
            .GetBoolean());
        Assert.AreEqual(
            1L,
            await CountSuccessfulRevocationsAsync(
                scenario.ConnectionString,
                firstTargetSession.SessionId));
        Assert.AreEqual(
            1L,
            await CountSuccessfulRevocationsAsync(
                scenario.ConnectionString,
                secondTargetSession.SessionId));
        Assert.AreEqual(
            0L,
            await CountSuccessfulRevocationsAsync(
                scenario.ConnectionString,
                foreignSession.SessionId));
    }

    [TestMethod]
    public async Task BulkAuditJournalFailureReturnsUnavailableAndRollsBackEverySession()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession actor = scenario.CreateBrowser();
        using BrowserSession firstTarget = scenario.CreateBrowser();
        using BrowserSession secondTarget = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(actor, "email-owner");
        await IdentityApiTestActions.SignInAsync(firstTarget, "email-owner");
        await IdentityApiTestActions.SignInAsync(secondTarget, "email-owner");
        OwnSession firstTargetSession = await GetCurrentSessionAsync(firstTarget);
        OwnSession secondTargetSession = await GetCurrentSessionAsync(secondTarget);
        antiforgery = await CompleteManageSessionsStepUpAsync(actor, antiforgery);

        await InstallFailingRevocationJournalTriggerAsync(scenario.ConnectionString);
        try
        {
            await AssertProblemAsync(
                await actor.DeleteWithConcurrencyAsync(
                    "/api/identity/sessions/others",
                    antiforgery,
                    ifMatch: null,
                    idempotencyKey: null),
                HttpStatusCode.ServiceUnavailable,
                "identity.session_management_unavailable");
        }
        finally
        {
            await RemoveFailingRevocationJournalTriggerAsync(scenario.ConnectionString);
        }

        foreach ((BrowserSession target, OwnSession original) in new[]
        {
            (firstTarget, firstTargetSession),
            (secondTarget, secondTargetSession),
        })
        {
            SessionPersistenceState state = await ReadSessionPersistenceStateAsync(
                scenario.ConnectionString,
                original.SessionId);
            Assert.IsNull(state.RevokedAtUtc, "Every target revocation must roll back.");
            Assert.AreEqual(original.Version, state.Version);
            Assert.AreEqual(0L, state.SuccessfulJournalCount);
            using HttpResponseMessage stillAuthenticated = await target.GetAsync(
                "/api/identity/session");
            Assert.AreEqual(HttpStatusCode.OK, stillAuthenticated.StatusCode);
        }
    }

    [TestMethod]
    public async Task RevokedSessionCannotUseProductiveEndpoint()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        await ApplyProductiveCoreMigrationsAsync(scenario.ConnectionString);
        using BrowserSession actor = scenario.CreateBrowser();
        using BrowserSession target = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(actor, "email-owner");
        await IdentityApiTestActions.SignInAsync(target, "email-owner");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            actor,
            "Sesiones productivas",
            "session-revocation-productive-journey",
            antiforgery);
        OwnSession targetSession = await GetCurrentSessionAsync(target);

        using (HttpResponseMessage beforeRevocation = await target.GetAsync(
            $"/api/organizations/{organizationId:D}/fields"))
        {
            Assert.AreEqual(HttpStatusCode.OK, beforeRevocation.StatusCode);
        }

        antiforgery = await CompleteManageSessionsStepUpAsync(actor, antiforgery);
        using (HttpResponseMessage revoked = await actor.DeleteWithConcurrencyAsync(
            $"/api/identity/sessions/{targetSession.SessionId:D}",
            antiforgery,
            Quote(targetSession.Version),
            idempotencyKey: null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, revoked.StatusCode);
        }

        await AssertProblemAsync(
            await target.GetAsync($"/api/organizations/{organizationId:D}/fields"),
            HttpStatusCode.Unauthorized,
            "identity.session_required");
        using HttpResponseMessage actorStillAuthorized = await actor.GetAsync(
            $"/api/organizations/{organizationId:D}/fields");
        Assert.AreEqual(HttpStatusCode.OK, actorStillAuthorized.StatusCode);
    }

    [TestMethod]
    public void DomainRevocationIsTerminalAndDoesNotRotateVersionTwice()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        UserSession session = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new byte[32],
            now,
            now.AddHours(1),
            isAuthenticationAssuranceVerified: true);

        Assert.IsTrue(session.Revoke(now.AddMinutes(1)));
        Guid revokedVersion = session.Version;
        DateTimeOffset? revokedAtUtc = session.RevokedAtUtc;

        Assert.IsFalse(session.Revoke(now.AddMinutes(2)));
        Assert.AreEqual(revokedVersion, session.Version);
        Assert.AreEqual(revokedAtUtc, session.RevokedAtUtc);
    }

    [TestMethod]
    public async Task DatabaseConnectionFailureReturnsTypedSessionManagementUnavailable()
    {
        await using IdentityDbContext dbContext = new(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(
                    "Host=127.0.0.1;Port=1;Database=unavailable;Username=none;Password=none;" +
                    "Timeout=1;Command Timeout=1")
                .Options);
        await using ServiceProvider services = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        IdentityApplicationService service = new(
            dbContext,
            new IdentityTokenService(),
            new IdentityTelemetry(services.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()),
            TimeProvider.System,
            Options.Create(new IdentityRuntimeOptions()),
            Options.Create(new OrganizationBootstrapOptions()));
        Guid userId = Guid.NewGuid();
        AuthenticatedSession current = new(
            Guid.NewGuid(),
            userId,
            DateTimeOffset.UtcNow,
            true,
            null,
            null,
            Guid.NewGuid());

        IdentityOperationException error = await Assert.ThrowsExactlyAsync<IdentityOperationException>(
            () => service.ListOwnActiveSessionsAsync(
                current,
                offset: 0,
                limit: 20,
                IdentityRequestContext.ForPlatform("database-failure", userId),
                CancellationToken.None));

        Assert.AreEqual("identity.session_management_unavailable", error.Code);
        Assert.AreEqual(503, error.StatusCode);
        Assert.IsTrue(error.Retryable);
    }

    [TestMethod]
    public async Task InvalidActorSessionContextCannotBecomeAnEmptySuccessfulInventory()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        (Guid userId, Guid sessionId, DateTimeOffset authenticatedAtUtc) =
            await ReadActiveSessionAsync(scenario.ConnectionString);

        await using IdentityDbContext dbContext = new(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(scenario.ConnectionString)
                .Options);
        await using ServiceProvider services = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        IdentityApplicationService service = new(
            dbContext,
            new IdentityTokenService(),
            new IdentityTelemetry(services.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()),
            TimeProvider.System,
            Options.Create(new IdentityRuntimeOptions()),
            Options.Create(new OrganizationBootstrapOptions()));
        AuthenticatedSession staleContext = new(
            sessionId,
            userId,
            authenticatedAtUtc,
            true,
            null,
            null,
            Guid.NewGuid());

        IdentityOperationException error = await Assert.ThrowsExactlyAsync<IdentityOperationException>(
            () => service.ListOwnActiveSessionsAsync(
                staleContext,
                offset: 0,
                limit: 20,
                IdentityRequestContext.ForPlatform("stale-session-context", userId),
                CancellationToken.None));

        Assert.AreEqual("identity.session_management_unavailable", error.Code);
        Assert.AreEqual(503, error.StatusCode);
    }

    private static async Task<string> CompleteManageSessionsStepUpAsync(
        BrowserSession browser,
        string antiforgery) =>
        await CompleteStepUpAsync(browser, antiforgery, StepUpPurposes.ManageSessions);

    private static async Task<string> CompleteStepUpAsync(
        BrowserSession browser,
        string antiforgery,
        string purpose)
    {
        using HttpResponseMessage started = await browser.PostAsync(
            "/api/identity/step-up-attempts",
            new Dictionary<string, string> { ["purpose"] = purpose },
            antiforgery);
        Assert.AreEqual(HttpStatusCode.Created, started.StatusCode);
        using JsonDocument payload = await JsonDocument.ParseAsync(
            await started.Content.ReadAsStreamAsync());
        Guid attemptId = payload.RootElement.GetProperty("attemptId").GetGuid();

        using HttpResponseMessage completed = await browser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            antiforgery);
        Assert.AreEqual(HttpStatusCode.OK, completed.StatusCode);
        using JsonDocument session = await JsonDocument.ParseAsync(
            await completed.Content.ReadAsStreamAsync());
        Assert.AreEqual(
            purpose,
            session.RootElement.GetProperty("authentication").GetProperty("purpose").GetString());
        return await browser.GetAntiforgeryTokenAsync();
    }

    private static async Task<JsonDocument> GetInventoryAsync(
        BrowserSession browser,
        int offset,
        int limit)
    {
        using HttpResponseMessage response = await browser.GetAsync(
            $"/api/identity/sessions?offset={offset}&limit={limit}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

    private static async Task<OwnSession> GetCurrentSessionAsync(BrowserSession browser)
    {
        using JsonDocument inventory = await GetInventoryAsync(browser, offset: 0, limit: 50);
        JsonElement current = inventory.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(item => item.GetProperty("isCurrent").GetBoolean());
        return new OwnSession(
            current.GetProperty("sessionId").GetGuid(),
            current.GetProperty("version").GetGuid());
    }

    private static async Task ExpireSessionAsync(string connectionString, Guid sessionId)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            UPDATE identity.sessions
            SET "AuthenticatedAtUtc" = now() - interval '2 days',
                "ExpiresAtUtc" = now() - interval '1 day'
            WHERE "Id" = @sessionId
            """,
            connection);
        command.Parameters.AddWithValue("sessionId", sessionId);
        Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
    }

    private static async Task<long> CountSuccessfulRevocationsAsync(
        string connectionString,
        Guid targetSessionId)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT count(*)
            FROM identity.audit_events
            WHERE "SessionId" = @sessionId
              AND "Action" = 'session_revoked'
              AND "Outcome" = 'succeeded'
            """,
            connection);
        command.Parameters.AddWithValue("sessionId", targetSessionId);
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new AssertFailedException("Revocation journal count was null."));
    }

    private static async Task InstallFailingRevocationJournalTriggerAsync(
        string connectionString)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            CREATE FUNCTION identity.test_fail_session_revocation_journal()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $trigger$
            BEGIN
                IF NEW."Action" = 'session_revoked' THEN
                    RAISE EXCEPTION 'injected session revocation journal failure';
                END IF;
                RETURN NEW;
            END;
            $trigger$;

            CREATE TRIGGER test_fail_session_revocation_journal
            BEFORE INSERT ON identity.audit_events
            FOR EACH ROW
            EXECUTE FUNCTION identity.test_fail_session_revocation_journal();
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ApplyProductiveCoreMigrationsAsync(string connectionString)
    {
        await using ProductiveCoreDbContext dbContext = new(
            new DbContextOptionsBuilder<ProductiveCoreDbContext>()
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        "productive_core"))
                .Options);
        await dbContext.Database.MigrateAsync();
    }

    private static async Task RemoveFailingRevocationJournalTriggerAsync(
        string connectionString)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            DROP TRIGGER IF EXISTS test_fail_session_revocation_journal
            ON identity.audit_events;
            DROP FUNCTION IF EXISTS identity.test_fail_session_revocation_journal();
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<SessionPersistenceState> ReadSessionPersistenceStateAsync(
        string connectionString,
        Guid targetSessionId)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT session."RevokedAtUtc",
                   session."Version",
                   (
                       SELECT count(*)
                       FROM identity.audit_events audit
                       WHERE audit."SessionId" = session."Id"
                         AND audit."Action" = 'session_revoked'
                         AND audit."Outcome" = 'succeeded'
                   )
            FROM identity.sessions session
            WHERE session."Id" = @sessionId
            """,
            connection);
        command.Parameters.AddWithValue("sessionId", targetSessionId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return new SessionPersistenceState(
            reader.IsDBNull(0) ? null : reader.GetFieldValue<DateTimeOffset>(0),
            reader.GetGuid(1),
            reader.GetInt64(2));
    }

    private static async Task<(Guid UserId, Guid SessionId, DateTimeOffset AuthenticatedAtUtc)>
        ReadActiveSessionAsync(string connectionString)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT "UserId", "Id", "AuthenticatedAtUtc"
            FROM identity.sessions
            WHERE "RevokedAtUtc" IS NULL
              AND "ExpiresAtUtc" > now()
            ORDER BY "AuthenticatedAtUtc" DESC, "Id"
            LIMIT 1
            """,
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return (reader.GetGuid(0), reader.GetGuid(1), reader.GetFieldValue<DateTimeOffset>(2));
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string code)
    {
        using (response)
        {
            Assert.AreEqual(status, response.StatusCode);
            Assert.AreEqual(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            using JsonDocument problem = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());
            Assert.AreEqual(code, problem.RootElement.GetProperty("code").GetString());
        }
    }

    private static string Quote(Guid version) => $"\"{version:D}\"";

    private sealed record OwnSession(Guid SessionId, Guid Version);

    private sealed record SessionPersistenceState(
        DateTimeOffset? RevokedAtUtc,
        Guid Version,
        long SuccessfulJournalCount);
}

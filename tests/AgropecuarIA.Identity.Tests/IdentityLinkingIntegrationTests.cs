using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class IdentityLinkingIntegrationTests
{
    private static readonly string[] ExpectedIdentityLinkedPayloadProperties =
        ["connection", "identityId", "linkedAtUtc", "userId"];

    [TestMethod]
    public async Task ConcurrentSignInsForTheSameIdentityConvergeOnOneUser()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var firstBrowser = scenario.CreateBrowser();
        using var secondBrowser = scenario.CreateBrowser();
        var firstAntiforgeryToken = await firstBrowser.GetAntiforgeryTokenAsync();
        var secondAntiforgeryToken = await secondBrowser.GetAntiforgeryTokenAsync();

        var responses = await Task.WhenAll(
            firstBrowser.PostAsync(
                "/api/development/identity/sign-in",
                new Dictionary<string, string> { ["fixture"] = "email-owner" },
                firstAntiforgeryToken),
            secondBrowser.PostAsync(
                "/api/development/identity/sign-in",
                new Dictionary<string, string> { ["fixture"] = "email-owner" },
                secondAntiforgeryToken));

        try
        {
            Assert.IsTrue(
                responses.All(response => response.StatusCode == HttpStatusCode.NoContent),
                $"Concurrent sign-ins returned: {string.Join(", ", responses.Select(response => response.StatusCode))}.");
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var firstSession = await IdentityApiTestActions.GetSessionAsync(firstBrowser);
        using var secondSession = await IdentityApiTestActions.GetSessionAsync(secondBrowser);
        Assert.AreEqual(
            firstSession.RootElement.GetProperty("userId").GetGuid(),
            secondSession.RootElement.GetProperty("userId").GetGuid());
        Assert.AreEqual(1L, await CountRowsAsync(scenario.ConnectionString, "identity.users"));
        Assert.AreEqual(1L, await CountRowsAsync(scenario.ConnectionString, "identity.external_identities"));
    }

    [TestMethod]
    public async Task TwoVerifiedCredentialsConvergeOnOneUserAndPreserveMemberships()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var emailBrowser = scenario.CreateBrowser();
        var antiforgeryToken = await IdentityApiTestActions.SignInAsync(emailBrowser, "email-owner");
        using var before = await IdentityApiTestActions.GetSessionAsync(emailBrowser);
        var userId = before.RootElement.GetProperty("userId").GetGuid();
        var memberships = IdentityApiTestActions.Memberships(before.RootElement);
        Assert.IsNotEmpty(memberships, "The owner fixture must exercise membership preservation.");

        var attemptId = await IdentityApiTestActions.StartLinkAsync(
            emailBrowser,
            "google",
            antiforgeryToken);
        await IdentityApiTestActions.VerifyCandidateAsync(
            emailBrowser,
            attemptId,
            "google-owner",
            antiforgeryToken);

        using (var completion = await emailBrowser.PostWithoutBodyAsync(
            $"/api/identity/link-attempts/{attemptId:D}/complete",
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.OK, completion.StatusCode);
        }

        using var linked = await IdentityApiTestActions.GetSessionAsync(emailBrowser);
        Assert.AreEqual(userId, linked.RootElement.GetProperty("userId").GetGuid());
        Assert.HasCount(2, linked.RootElement.GetProperty("identities").EnumerateArray());
        CollectionAssert.AreEqual(memberships, IdentityApiTestActions.Memberships(linked.RootElement));
        Guid linkedIdentityId = linked.RootElement
            .GetProperty("identities")
            .EnumerateArray()
            .Single(identity =>
                identity.GetProperty("connection").GetString() == IdentityConnections.Google)
            .GetProperty("identityId")
            .GetGuid();

        using var googleBrowser = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(googleBrowser, "google-owner");
        using var signedInThroughGoogle = await IdentityApiTestActions.GetSessionAsync(googleBrowser);
        Assert.AreEqual(userId, signedInThroughGoogle.RootElement.GetProperty("userId").GetGuid());
        CollectionAssert.AreEqual(
            memberships,
            IdentityApiTestActions.Memberships(signedInThroughGoogle.RootElement));

        IdentityLinkedEnvelope envelope = await ReadIdentityLinkedEnvelopeAsync(
            scenario.ConnectionString);
        IdentityIntegrationEventDefinition definition = IdentityIntegrationEvents.IdentityLinked;
        Assert.AreEqual(definition.Type, envelope.Type);
        Assert.AreEqual(definition.MajorVersion, envelope.Version);
        Assert.AreEqual(definition.SchemaVersion, envelope.SchemaVersion);
        Assert.AreEqual(definition.Source, envelope.Source);
        Assert.AreEqual(definition.Scope, envelope.ScopeKind);
        Assert.IsNull(envelope.TenantId);
        Assert.AreEqual(envelope.OccurredAtUtc, envelope.EffectiveAtUtc);
        Assert.AreEqual(envelope.OccurredAtUtc, envelope.RecordedAtUtc);
        Assert.AreEqual(userId, envelope.ActorId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.CorrelationId));
        Assert.AreEqual(attemptId, envelope.CausationId);
        Assert.AreEqual(definition.AggregateType, envelope.AggregateType);
        Assert.AreEqual(userId, envelope.AggregateId);
        Assert.AreEqual(1L, envelope.AggregateVersion);
        AssertIdentityLinkedPayload(envelope, userId, linkedIdentityId);
    }

    [TestMethod]
    public async Task LinkCompletionIsOneShotAndRejectsReplay()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        var antiforgeryToken = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        var attemptId = await IdentityApiTestActions.StartLinkAsync(browser, "google", antiforgeryToken);
        await IdentityApiTestActions.VerifyCandidateAsync(
            browser,
            attemptId,
            "google-owner",
            antiforgeryToken);

        using (var first = await browser.PostWithoutBodyAsync(
            $"/api/identity/link-attempts/{attemptId:D}/complete",
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        }
        Assert.AreEqual(1L, await CountIdentityLinkedOutboxMessagesAsync(scenario.ConnectionString));

        using var replay = await browser.PostWithoutBodyAsync(
            $"/api/identity/link-attempts/{attemptId:D}/complete",
            antiforgeryToken);
        Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.AreEqual(1L, await CountIdentityLinkedOutboxMessagesAsync(scenario.ConnectionString));
        Assert.AreEqual(
            1L,
            await CountSecurityJournalEntriesAsync(
                scenario.ConnectionString,
                "identity_linked",
                "rejected"),
            "A replay rejection must remain observable without duplicating the domain event.");
    }

    [TestMethod]
    public async Task IdentityOwnedByAnotherUserIsNotReassigned()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using (var otherOwner = scenario.CreateBrowser())
        {
            await IdentityApiTestActions.SignInAsync(
                otherOwner,
                "identity-owned-by-another-user");
        }

        using var browser = scenario.CreateBrowser();
        var antiforgeryToken = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        using var before = await IdentityApiTestActions.GetSessionAsync(browser);
        var userId = before.RootElement.GetProperty("userId").GetGuid();
        var attemptId = await IdentityApiTestActions.StartLinkAsync(browser, "google", antiforgeryToken);
        await IdentityApiTestActions.VerifyCandidateAsync(
            browser,
            attemptId,
            "identity-owned-by-another-user",
            antiforgeryToken);

        using (var completion = await browser.PostWithoutBodyAsync(
            $"/api/identity/link-attempts/{attemptId:D}/complete",
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.Conflict, completion.StatusCode);
        }

        using var after = await IdentityApiTestActions.GetSessionAsync(browser);
        Assert.AreEqual(userId, after.RootElement.GetProperty("userId").GetGuid());
        Assert.HasCount(1, after.RootElement.GetProperty("identities").EnumerateArray());
        Assert.AreEqual(0L, await CountIdentityLinkedOutboxMessagesAsync(scenario.ConnectionString));
    }

    [TestMethod]
    public async Task UnverifiedEmailCannotCreateASession()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        var antiforgeryToken = await browser.GetAntiforgeryTokenAsync();

        using (var signIn = await browser.PostAsync(
            "/api/development/identity/sign-in",
            new Dictionary<string, string> { ["fixture"] = "unverified-email" },
            antiforgeryToken))
        {
            Assert.IsFalse(signIn.IsSuccessStatusCode);
        }

        using var session = await browser.GetAsync("/api/identity/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, session.StatusCode);
    }

    [TestMethod]
    public async Task ProviderFailureDoesNotAttachCandidateIdentity()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        var antiforgeryToken = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        var attemptId = await IdentityApiTestActions.StartLinkAsync(browser, "google", antiforgeryToken);

        using var verification = await browser.PostAsync(
            $"/api/development/identity/link-attempts/{attemptId:D}/verify",
            new Dictionary<string, string> { ["fixture"] = "provider-down" },
            antiforgeryToken);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, verification.StatusCode);

        using var after = await IdentityApiTestActions.GetSessionAsync(browser);
        Assert.HasCount(1, after.RootElement.GetProperty("identities").EnumerateArray());
    }

    private static async Task<long> CountIdentityLinkedOutboxMessagesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*)
            FROM identity.outbox_messages
            WHERE "Type" = @type
            """,
            connection);
        command.Parameters.AddWithValue("type", IdentityIntegrationEvents.IdentityLinked.Type);
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The outbox count returned null."));
    }

    private static async Task<long> CountRowsAsync(string connectionString, string qualifiedTableName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {qualifiedTableName}",
            connection);
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The row count returned null."));
    }

    private static async Task<IdentityLinkedEnvelope> ReadIdentityLinkedEnvelopeAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT "Type", "Version", "SchemaVersion", "Source", "ScopeKind", "TenantId", "OccurredAtUtc",
                   "EffectiveAtUtc", "RecordedAtUtc", "ActorId", "CorrelationId", "CausationId",
                   "AggregateType", "AggregateId", "AggregateVersion", "Payload"
            FROM identity.outbox_messages
            WHERE "Type" = @type
            """,
            connection);
        command.Parameters.AddWithValue("type", IdentityIntegrationEvents.IdentityLinked.Type);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return new IdentityLinkedEnvelope(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetGuid(5),
            reader.GetFieldValue<DateTimeOffset>(6),
            reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
            reader.GetFieldValue<DateTimeOffset>(8),
            reader.GetGuid(9),
            reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetGuid(11),
            reader.GetString(12),
            reader.GetGuid(13),
            reader.GetInt64(14),
            reader.GetString(15));
    }

    private static void AssertIdentityLinkedPayload(
        IdentityLinkedEnvelope envelope,
        Guid userId,
        Guid linkedIdentityId)
    {
        using JsonDocument payload = JsonDocument.Parse(envelope.Payload);
        JsonElement root = payload.RootElement;
        CollectionAssert.AreEqual(
            ExpectedIdentityLinkedPayloadProperties,
            root.EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal)
                .ToArray());
        Assert.AreEqual(userId, root.GetProperty("userId").GetGuid());
        Assert.AreEqual(linkedIdentityId, root.GetProperty("identityId").GetGuid());
        Assert.AreEqual(IdentityConnections.Google, root.GetProperty("connection").GetString());
        TimeSpan timestampDelta = (envelope.OccurredAtUtc -
            root.GetProperty("linkedAtUtc").GetDateTimeOffset()).Duration();
        Assert.IsTrue(
            timestampDelta < TimeSpan.FromMicroseconds(1),
            $"Payload and envelope timestamps differ by {timestampDelta.TotalMicroseconds} microseconds.");
        Assert.IsFalse(envelope.Payload.Contains("issuer", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(envelope.Payload.Contains("subject", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(envelope.Payload.Contains("label", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<long> CountSecurityJournalEntriesAsync(
        string connectionString,
        string action,
        string outcome)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*)
            FROM identity.audit_events
            WHERE "Action" = @action AND "Outcome" = @outcome
            """,
            connection);
        command.Parameters.AddWithValue("action", action);
        command.Parameters.AddWithValue("outcome", outcome);
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The audit count returned null."));
    }

    private sealed record IdentityLinkedEnvelope(
        string Type,
        int Version,
        string SchemaVersion,
        string Source,
        string ScopeKind,
        Guid? TenantId,
        DateTimeOffset OccurredAtUtc,
        DateTimeOffset? EffectiveAtUtc,
        DateTimeOffset RecordedAtUtc,
        Guid ActorId,
        string CorrelationId,
        Guid? CausationId,
        string AggregateType,
        Guid AggregateId,
        long AggregateVersion,
        string Payload);
}

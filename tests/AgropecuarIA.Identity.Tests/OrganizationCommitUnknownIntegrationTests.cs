using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrganizationCommitUnknownIntegrationTests
{
    private const string Endpoint = "/api/identity/organizations";
    private static readonly string[] OrganizationTelemetryTags =
    [
        "contract.version",
        "contract.consumer",
        "identity.operation",
        "identity.outcome",
    ];

    [TestMethod]
    public async Task ConfirmedCommitIsReconciledFromFreshContextAndReplaysExactResult()
    {
        var commitBoundary = new ControlledCommitBoundary(commitBeforeUnknown: true);
        TrackingRecoveryContextFactory? recoveryFactory = null;
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configureServices: (services, connectionString) =>
            {
                recoveryFactory = ReplaceRecoveryServices(
                    services,
                    connectionString,
                    commitBoundary,
                    failRecovery: false);
            });
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        const string key = "organization-commit-unknown-confirmed";

        using HttpResponseMessage recovered = await CreateAsync(
            browser,
            antiforgery,
            key,
            "La Confirmada");
        using HttpResponseMessage replayed = await CreateAsync(
            browser,
            antiforgery,
            key,
            "La Confirmada");

        Assert.AreEqual(HttpStatusCode.Created, recovered.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, replayed.StatusCode);
        Assert.AreEqual(
            await recovered.Content.ReadAsStringAsync(),
            await replayed.Content.ReadAsStringAsync());
        Assert.AreEqual(1, commitBoundary.CommitAttempts);
        Assert.AreEqual(1, recoveryFactory?.CreatedContexts);
        await AssertProducerEffectCountAsync(scenario.ConnectionString, expected: 1);
    }

    [TestMethod]
    public async Task ConfirmedRollbackRetriesExactlyOnceFromFreshContext()
    {
        var commitBoundary = new ControlledCommitBoundary(commitBeforeUnknown: false);
        TrackingRecoveryContextFactory? recoveryFactory = null;
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configureServices: (services, connectionString) =>
            {
                recoveryFactory = ReplaceRecoveryServices(
                    services,
                    connectionString,
                    commitBoundary,
                    failRecovery: false);
            });
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        using HttpResponseMessage response = await CreateAsync(
            browser,
            antiforgery,
            "organization-commit-unknown-rollback",
            "La Reintentada");

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.AreEqual(2, commitBoundary.CommitAttempts);
        Assert.AreEqual(1, recoveryFactory?.CreatedContexts);
        await AssertProducerEffectCountAsync(scenario.ConnectionString, expected: 1);
    }

    [TestMethod]
    public async Task TransientReconciliationFailureReturns503WithoutSecondEffectAndBoundedTelemetry()
    {
        var measurements = new ConcurrentQueue<IReadOnlyDictionary<string, object?>>();
        using var listener = CreateOrganizationTelemetryListener(measurements);
        var commitBoundary = new ControlledCommitBoundary(commitBeforeUnknown: false);
        TrackingRecoveryContextFactory? recoveryFactory = null;
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(
            configureServices: (services, connectionString) =>
            {
                recoveryFactory = ReplaceRecoveryServices(
                    services,
                    connectionString,
                    commitBoundary,
                    failRecovery: true);
            });
        using BrowserSession browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        const string key = "organization-commit-unknown-transient";

        using HttpResponseMessage response = await CreateAsync(
            browser,
            antiforgery,
            key,
            "La Incierta");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(
            "idempotency.reconciliation_required",
            problem.RootElement.GetProperty("code").GetString());
        Assert.IsTrue(problem.RootElement.GetProperty("retryable").GetBoolean());
        Assert.AreEqual(1, commitBoundary.CommitAttempts);
        Assert.AreEqual(1, recoveryFactory?.CreatedContexts);
        await AssertProducerEffectCountAsync(scenario.ConnectionString, expected: 0);

        IReadOnlyDictionary<string, object?> telemetry = measurements.Single(tags =>
            Equals(tags["identity.operation"], "organization_create"));
        Assert.AreEqual("reconciliation_required", telemetry["identity.outcome"]);
        CollectionAssert.AreEquivalent(
            OrganizationTelemetryTags,
            telemetry.Keys.ToArray());
        string serializedTags = string.Join('|', telemetry.Values);
        Assert.IsFalse(serializedTags.Contains(key, StringComparison.Ordinal));
        Assert.IsFalse(serializedTags.Contains("La Incierta", StringComparison.Ordinal));
    }

    private static TrackingRecoveryContextFactory ReplaceRecoveryServices(
        IServiceCollection services,
        string connectionString,
        ControlledCommitBoundary commitBoundary,
        bool failRecovery)
    {
        var recoveryFactory = new TrackingRecoveryContextFactory(
            connectionString,
            failRecovery);
        services.RemoveAll<IOrganizationCreationCommitBoundary>();
        services.RemoveAll<IOrganizationCreationRecoveryContextFactory>();
        services.AddSingleton<IOrganizationCreationCommitBoundary>(commitBoundary);
        services.AddSingleton<IOrganizationCreationRecoveryContextFactory>(recoveryFactory);
        return recoveryFactory;
    }

    private static MeterListener CreateOrganizationTelemetryListener(
        ConcurrentQueue<IReadOnlyDictionary<string, object?>> measurements)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == IdentityTelemetry.SourceName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var measurement = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                measurement[tag.Key] = tag.Value;
            }

            if (Equals(measurement.GetValueOrDefault("identity.operation"), "organization_create"))
            {
                measurements.Enqueue(measurement);
            }
        });
        listener.Start();
        return listener;
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

    private static async Task AssertProducerEffectCountAsync(
        string connectionString,
        long expected)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
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
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        for (int index = 0; index < 7; index++)
        {
            Assert.AreEqual(expected, reader.GetInt64(index), $"Producer count {index} differed.");
        }
    }

    private sealed class ControlledCommitBoundary(bool commitBeforeUnknown) :
        IOrganizationCreationCommitBoundary
    {
        private int commitAttempts;

        public int CommitAttempts => Volatile.Read(ref commitAttempts);

        public async Task CommitAsync(
            Func<CancellationToken, Task> commit,
            Func<CancellationToken, Task> rollback,
            CancellationToken cancellationToken)
        {
            int attempt = Interlocked.Increment(ref commitAttempts);
            if (attempt == 1)
            {
                if (commitBeforeUnknown)
                {
                    await commit(cancellationToken);
                }
                else
                {
                    await rollback(cancellationToken);
                }

                throw new OrganizationCommitOutcomeUnknownException(
                    "Injected post-commit acknowledgement loss.");
            }

            await commit(cancellationToken);
        }
    }

    private sealed class TrackingRecoveryContextFactory(
        string connectionString,
        bool failRecovery) : IOrganizationCreationRecoveryContextFactory
    {
        private int createdContexts;

        public int CreatedContexts => Volatile.Read(ref createdContexts);

        public ValueTask<IdentityDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref createdContexts);
            if (failRecovery)
            {
                throw new NpgsqlException(
                    "Injected transient reconciliation connection failure.",
                    new TimeoutException("Injected recovery timeout."));
            }

            cancellationToken.ThrowIfCancellationRequested();
            DbContextOptions<IdentityDbContext> options =
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;
            return ValueTask.FromResult(new IdentityDbContext(options));
        }
    }
}

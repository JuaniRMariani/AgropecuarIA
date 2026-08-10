using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class StepUpApplicationIntegrationTests
{
    private static readonly string[] ExpectedStepUpPayloadProperties =
        ["completedAtUtc", "previousSessionId", "purpose", "sessionId", "userId"];

    private static readonly string[] ExpectedOutboxFactoryNames =
        ["CreateIdentityLinked", "CreateIdentityStepUpCompleted"];

    private static readonly DateTimeOffset Now =
        new(2026, 8, 10, 18, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task CompletionConsumesAttemptAndRotatesSessionWithoutExtendingAbsoluteExpiry()
    {
        await using StepUpServiceScenario scenario = await StepUpServiceScenario.CreateAsync(Now);
        SeededIdentity seeded = await scenario.SeedAsync();
        IdentityRequestContext context = IdentityRequestContext.ForPlatform(
            "step-up-complete",
            seeded.UserId);
        StartedStepUpAttempt started = await scenario.Service.StartStepUpAttemptAsync(
            seeded.Session,
            StepUpPurposes.ManageAuthenticationMethods,
            context,
            CancellationToken.None);

        StepUpAttemptValidation validation = await scenario.Service.ValidateStepUpAttemptAsync(
            started.AttemptId,
            seeded.Session,
            context,
            CancellationToken.None);
        IssuedSession issued = await scenario.Service.CompleteStepUpAttemptAsync(
            started.AttemptId,
            seeded.Session,
            new VerifiedStepUpProof(
                seeded.Issuer,
                seeded.Subject,
                Now.AddMinutes(1),
                IsStrongAuthentication: true),
            context,
            CancellationToken.None);

        Assert.AreEqual(StepUpPurposes.ManageAuthenticationMethods, validation.Purpose);
        Assert.AreEqual(seeded.AbsoluteExpiresAtUtc, issued.ExpiresAtUtc);
        Assert.AreNotEqual(seeded.Session.SessionId, issued.SessionId);
        Assert.IsNull(await scenario.Service.AuthenticateAsync(
            seeded.Token,
            CancellationToken.None));

        AuthenticatedSession? authenticated = await scenario.Service.AuthenticateAsync(
            issued.Token,
            CancellationToken.None);
        Assert.IsNotNull(authenticated);
        Assert.AreEqual(Now.AddMinutes(1), authenticated.StrongAuthenticatedAtUtc);
        Assert.AreEqual(
            StepUpPurposes.ManageAuthenticationMethods,
            authenticated.StrongAuthenticationPurpose);

        StepUpAttempt attempt = await scenario.DbContext.StepUpAttempts
            .AsNoTracking()
            .SingleAsync(item => item.Id == started.AttemptId);
        Assert.AreEqual(Now, attempt.ConsumedAtUtc);

        IdentityOutboxMessage message = await scenario.DbContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync(item =>
                item.Type == IdentityIntegrationEvents.IdentityStepUpCompleted.Type);
        AssertStepUpCompletedEnvelope(
            message,
            seeded.UserId,
            seeded.Session.SessionId,
            issued.SessionId,
            started.AttemptId,
            context.CorrelationId);

        IdentityOperationException replay = await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
            scenario.Service.CompleteStepUpAttemptAsync(
                started.AttemptId,
                seeded.Session,
                new VerifiedStepUpProof(
                    seeded.Issuer,
                    seeded.Subject,
                    Now.AddMinutes(1),
                    IsStrongAuthentication: true),
                context,
                CancellationToken.None));
        Assert.AreEqual("identity.step_up_attempt_conflict", replay.Code);
        Assert.AreEqual(1, await scenario.DbContext.OutboxMessages.CountAsync(item =>
            item.Type == IdentityIntegrationEvents.IdentityStepUpCompleted.Type));
        Assert.AreEqual(1, await scenario.DbContext.SecurityJournalEntries.CountAsync(
            entry => entry.Action == "step_up_completed" && entry.Outcome == "succeeded"));
    }

    [TestMethod]
    public void OutboxRejectsScopeThatDoesNotMatchThePublishedDefinition()
    {
        Guid userId = Guid.NewGuid();
        Assert.ThrowsExactly<ArgumentException>(() =>
            IdentityOutboxMessage.CreateIdentityStepUpCompleted(
                CreateEnvelope(RequestScope.ForTenant(Guid.NewGuid()), userId, "scope-mismatch"),
                new IdentityStepUpCompletedIntegrationEventPayload(
                    userId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    StepUpPurposes.ManageAuthenticationMethods,
                    Now)));
    }

    [TestMethod]
    public void OutboxRejectsAnUnregisteredEventKind()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            IdentityIntegrationEvents.GetRequired((IdentityIntegrationEventKind)int.MaxValue));
    }

    [TestMethod]
    public void IntegrationEventCatalogExhaustivelyMapsEveryKnownKind()
    {
        IdentityIntegrationEventKind[] kinds = Enum.GetValues<IdentityIntegrationEventKind>();

        Assert.HasCount(kinds.Length, IdentityIntegrationEvents.All);
        for (int index = 0; index < kinds.Length; index++)
        {
            Assert.AreSame(
                IdentityIntegrationEvents.GetRequired(kinds[index]),
                IdentityIntegrationEvents.All[index]);
        }
    }

    [TestMethod]
    public void OutboxPublicApiOnlyExposesTypedEventFactories()
    {
        ConstructorInfo[] publicConstructors = typeof(IdentityOutboxMessage)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.HasCount(0, publicConstructors);

        MethodInfo[] factories = typeof(IdentityOutboxMessage)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name.StartsWith("CreateIdentity", StringComparison.Ordinal))
            .ToArray();
        CollectionAssert.AreEquivalent(
            ExpectedOutboxFactoryNames,
            factories.Select(factory => factory.Name).ToArray());
        Assert.IsFalse(factories.Any(factory => factory.GetParameters().Any(parameter =>
            parameter.ParameterType == typeof(IdentityIntegrationEventKind) ||
            parameter.ParameterType == typeof(string))));
        Assert.IsNull(typeof(IdentityIntegrationEventEnvelope).GetProperty("AggregateType"));
    }

    [TestMethod]
    public async Task CompletionRejectsWeakOrForeignProofWithoutConsumingAttempt()
    {
        await using StepUpServiceScenario scenario = await StepUpServiceScenario.CreateAsync(Now);
        SeededIdentity seeded = await scenario.SeedAsync();
        IdentityRequestContext context = IdentityRequestContext.ForPlatform(
            "step-up-rejected",
            seeded.UserId);
        StartedStepUpAttempt started = await scenario.Service.StartStepUpAttemptAsync(
            seeded.Session,
            StepUpPurposes.ManageAuthenticationMethods,
            context,
            CancellationToken.None);

        IdentityOperationException weak = await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
            scenario.Service.CompleteStepUpAttemptAsync(
                started.AttemptId,
                seeded.Session,
                new VerifiedStepUpProof(
                    seeded.Issuer,
                    seeded.Subject,
                    Now.AddMinutes(1),
                    IsStrongAuthentication: false),
                context,
                CancellationToken.None));
        Assert.AreEqual("identity.strong_authentication_required", weak.Code);

        IdentityOperationException foreign = await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
            scenario.Service.CompleteStepUpAttemptAsync(
                started.AttemptId,
                seeded.Session,
                new VerifiedStepUpProof(
                    seeded.Issuer,
                    "another-subject",
                    Now.AddMinutes(1),
                    IsStrongAuthentication: true),
                context,
                CancellationToken.None));
        Assert.AreEqual("identity.step_up_attempt_conflict", foreign.Code);

        StepUpAttempt attempt = await scenario.DbContext.StepUpAttempts
            .AsNoTracking()
            .SingleAsync(item => item.Id == started.AttemptId);
        Assert.IsNull(attempt.ConsumedAtUtc);
        Assert.IsNotNull(await scenario.Service.AuthenticateAsync(
            seeded.Token,
            CancellationToken.None));
    }

    [TestMethod]
    public async Task SessionAssuranceFallsBackToPrimaryAfterStrongWindow()
    {
        await using StepUpServiceScenario scenario = await StepUpServiceScenario.CreateAsync(Now);
        SeededIdentity seeded = await scenario.SeedAsync(
            strongAuthenticatedAtUtc: Now,
            strongAuthenticationPurpose: StepUpPurposes.ManageAuthenticationMethods);

        IdentitySessionResult strong = await scenario.Service.GetSessionAsync(
            seeded.Session with
            {
                StrongAuthenticatedAtUtc = Now,
                StrongAuthenticationPurpose = StepUpPurposes.ManageAuthenticationMethods,
            },
            CancellationToken.None);
        Assert.AreEqual("strong", strong.AuthenticationAssurance.Level);
        Assert.AreEqual(Now.AddMinutes(5), strong.AuthenticationAssurance.ExpiresAtUtc);

        scenario.TimeProvider.Advance(TimeSpan.FromMinutes(5));
        IdentitySessionResult primary = await scenario.Service.GetSessionAsync(
            seeded.Session with
            {
                StrongAuthenticatedAtUtc = Now,
                StrongAuthenticationPurpose = StepUpPurposes.ManageAuthenticationMethods,
            },
            CancellationToken.None);
        Assert.AreEqual("primary", primary.AuthenticationAssurance.Level);
        Assert.IsNull(primary.AuthenticationAssurance.Purpose);
        Assert.IsNull(primary.AuthenticationAssurance.StrongAuthenticatedAtUtc);
        Assert.IsNull(primary.AuthenticationAssurance.ExpiresAtUtc);
    }

    private static void AssertStepUpCompletedEnvelope(
        IdentityOutboxMessage message,
        Guid userId,
        Guid previousSessionId,
        Guid sessionId,
        Guid attemptId,
        string correlationId)
    {
        IdentityIntegrationEventDefinition definition =
            IdentityIntegrationEvents.IdentityStepUpCompleted;
        Assert.AreEqual(definition.Type, message.Type);
        Assert.AreEqual(definition.MajorVersion, message.Version);
        Assert.AreEqual(definition.SchemaVersion, message.SchemaVersion);
        Assert.AreEqual(definition.Source, message.Source);
        Assert.AreEqual(definition.Scope, message.ScopeKind);
        Assert.IsNull(message.TenantId);
        Assert.AreEqual(Now, message.OccurredAtUtc);
        Assert.AreEqual(Now, message.EffectiveAtUtc);
        Assert.AreEqual(Now, message.RecordedAtUtc);
        Assert.AreEqual(userId, message.ActorId);
        Assert.AreEqual(correlationId, message.CorrelationId);
        Assert.AreEqual(attemptId, message.CausationId);
        Assert.AreEqual(definition.AggregateType, message.AggregateType);
        Assert.AreEqual(userId, message.AggregateId);
        Assert.AreEqual(1L, message.AggregateVersion);
        Assert.IsNull(message.DispatchedAtUtc);

        using JsonDocument payload = JsonDocument.Parse(message.Payload);
        JsonElement root = payload.RootElement;
        CollectionAssert.AreEqual(
            ExpectedStepUpPayloadProperties,
            root.EnumerateObject()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal)
                .ToArray());
        Assert.AreEqual(
            StepUpPurposes.ManageAuthenticationMethods,
            root.GetProperty("purpose").GetString());
        Assert.AreEqual(Now, root.GetProperty("completedAtUtc").GetDateTimeOffset());
        Assert.AreEqual(userId, root.GetProperty("userId").GetGuid());
        Assert.AreEqual(previousSessionId, root.GetProperty("previousSessionId").GetGuid());
        Assert.AreEqual(sessionId, root.GetProperty("sessionId").GetGuid());
    }

    private static IdentityIntegrationEventEnvelope CreateEnvelope(
        RequestScope scope,
        Guid userId,
        string correlationId) =>
        new(
            Guid.NewGuid(),
            scope,
            Now,
            Now,
            Now,
            userId,
            correlationId,
            Guid.NewGuid(),
            userId,
            1);

    private sealed class StepUpServiceScenario : IAsyncDisposable
    {
        private readonly PostgreSqlTestServer _postgresql;
        private readonly ServiceProvider _services;
        private readonly string _connectionString;

        private StepUpServiceScenario(
            PostgreSqlTestServer postgresql,
            string connectionString,
            IdentityDbContext dbContext,
            IdentityApplicationService service,
            AdjustableTimeProvider timeProvider,
            ServiceProvider services)
        {
            _postgresql = postgresql;
            _connectionString = connectionString;
            DbContext = dbContext;
            Service = service;
            TimeProvider = timeProvider;
            _services = services;
        }

        public IdentityDbContext DbContext { get; }

        public IdentityApplicationService Service { get; }

        public AdjustableTimeProvider TimeProvider { get; }

        public static async Task<StepUpServiceScenario> CreateAsync(DateTimeOffset now)
        {
            PostgreSqlTestServer postgresql = IdentityTestAssembly.PostgreSql
                ?? throw new AssertFailedException(
                    "PostgreSQL integration fixture could not start: "
                    + IdentityTestAssembly.StartupError?.Message);
            string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
            DbContextOptions<IdentityDbContext> dbOptions =
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;
            IdentityDbContext dbContext = new(dbOptions);
            await dbContext.Database.EnsureCreatedAsync();

            ServiceProvider services = new ServiceCollection()
                .AddMetrics()
                .BuildServiceProvider();
            AdjustableTimeProvider timeProvider = new(now);
            IdentityApplicationService service = new(
                dbContext,
                new IdentityTokenService(),
                new IdentityTelemetry(services.GetRequiredService<IMeterFactory>()),
                timeProvider,
                Options.Create(new IdentityRuntimeOptions()));
            return new StepUpServiceScenario(
                postgresql,
                connectionString,
                dbContext,
                service,
                timeProvider,
                services);
        }

        public async Task<SeededIdentity> SeedAsync(
            DateTimeOffset? strongAuthenticatedAtUtc = null,
            string? strongAuthenticationPurpose = null)
        {
            Guid userId = Guid.NewGuid();
            Guid sessionId = Guid.NewGuid();
            string token = "test-session-" + Guid.NewGuid().ToString("N");
            DateTimeOffset absoluteExpiresAtUtc = TimeProvider.GetUtcNow().AddHours(6);
            const string issuer = "https://identity.example.test/";
            const string subject = "provider-user-123";

            DbContext.Users.Add(new PlatformUser(userId, "Test User", TimeProvider.GetUtcNow()));
            DbContext.ExternalIdentities.Add(new ExternalIdentity(
                Guid.NewGuid(),
                userId,
                IdentityConnections.Email,
                issuer,
                subject,
                "masked@example.test",
                TimeProvider.GetUtcNow()));
            DbContext.Sessions.Add(new UserSession(
                sessionId,
                userId,
                IdentityTokenService.HashToken(token),
                TimeProvider.GetUtcNow(),
                absoluteExpiresAtUtc,
                isAuthenticationAssuranceVerified: true,
                strongAuthenticatedAtUtc,
                strongAuthenticationPurpose));
            await DbContext.SaveChangesAsync();

            return new SeededIdentity(
                userId,
                token,
                issuer,
                subject,
                absoluteExpiresAtUtc,
                new AuthenticatedSession(
                    sessionId,
                    userId,
                    TimeProvider.GetUtcNow(),
                    IsAuthenticationAssuranceVerified: true,
                    strongAuthenticatedAtUtc,
                    strongAuthenticationPurpose));
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _services.DisposeAsync();
            await _postgresql.DropDatabaseAsync(_connectionString, CancellationToken.None);
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }

    private sealed record SeededIdentity(
        Guid UserId,
        string Token,
        string Issuer,
        string Subject,
        DateTimeOffset AbsoluteExpiresAtUtc,
        AuthenticatedSession Session);
}

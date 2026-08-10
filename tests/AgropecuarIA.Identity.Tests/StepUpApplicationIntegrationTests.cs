using System.Diagnostics.Metrics;
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
        Assert.AreEqual(1, await scenario.DbContext.OutboxMessages.CountAsync(
            message => message.Type == "IdentityStepUpCompleted"));
        Assert.AreEqual(1, await scenario.DbContext.SecurityJournalEntries.CountAsync(
            entry => entry.Action == "step_up_completed" && entry.Outcome == "succeeded"));
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

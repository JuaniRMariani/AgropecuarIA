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
public sealed class OrganizationOwnerInvitationApplicationTests
{
    private static readonly string[] AcceptanceFaultTargets = ["journal", "outbox"];

    private static readonly DateTimeOffset Now = CurrentTestTime();

    [TestMethod]
    public async Task CreateAndAcceptAreReplaySafeAndDualWriteMemberships()
    {
        await RunScenarioAsync(async scenario =>
        {
            CreatedOrganizationOwnerInvitationResult created = await scenario.CreateAsync(
                "owner-invitation-idempotency-key");
            CreatedOrganizationOwnerInvitationResult createReplay = await scenario.CreateAsync(
                "owner-invitation-idempotency-key");

            Assert.IsFalse(created.IsReplay);
            Assert.IsNotNull(created.Token);
            Assert.AreEqual(43, created.Token.Length);
            Assert.IsTrue(createReplay.IsReplay);
            Assert.IsNull(createReplay.Token);
            Assert.AreEqual(created.InvitationId, createReplay.InvitationId);

            AcceptedOrganizationOwnerInvitationResult accepted =
                await scenario.AcceptAsync(created.Token, scenario.Invitee);
            AcceptedOrganizationOwnerInvitationResult acceptReplay =
                await scenario.AcceptAsync(created.Token, scenario.Invitee);

            Assert.IsFalse(accepted.IsReplay);
            Assert.IsTrue(acceptReplay.IsReplay);
            Assert.AreEqual(accepted.MembershipId, acceptReplay.MembershipId);
            Assert.AreEqual(OrganizationMembershipRoles.Owner, accepted.MembershipRole);

            await using IdentityDbContext verification = scenario.CreateDbContext();
            Assert.AreEqual(
                1,
                await verification.OrganizationOwnerInvitations.CountAsync());
            Assert.AreEqual(
                1,
                await verification.AuthoritativeMemberships.CountAsync(item =>
                    item.OrganizationId == scenario.OrganizationId &&
                    item.UserId == scenario.Invitee.UserId));
            Assert.AreEqual(
                1,
                await verification.Memberships.CountAsync(item =>
                    item.OrganizationId == scenario.OrganizationId &&
                    item.UserId == scenario.Invitee.UserId));
            Assert.AreEqual(
                1,
                await verification.OutboxMessages.CountAsync(item =>
                    item.Type == "OrganizationOwnerInvited"));
            Assert.AreEqual(
                1,
                await verification.OutboxMessages.CountAsync(item =>
                    item.Type == "OrganizationOwnerInvitationAccepted"));

            IdentityOperationException foreignReplay =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.AcceptAsync(created.Token, scenario.Attacker));
            Assert.AreEqual(
                "identity.organization_owner_invitation_not_available",
                foreignReplay.Code);
        });
    }

    [TestMethod]
    public async Task ConcurrentCreateWithSameKeyProducesOneInvitationAndOneBearerDisclosure()
    {
        await RunScenarioAsync(async scenario =>
        {
            Task<CreatedOrganizationOwnerInvitationResult>[] attempts =
            [
                scenario.CreateAsync("owner-invitation-concurrent-key"),
                scenario.CreateAsync("owner-invitation-concurrent-key"),
            ];

            CreatedOrganizationOwnerInvitationResult[] results = await Task.WhenAll(attempts);

            Assert.AreEqual(1, results.Select(item => item.InvitationId).Distinct().Count());
            Assert.AreEqual(1, results.Count(item => item.Token is not null));
            Assert.AreEqual(1, results.Count(item => item.IsReplay));
            await using IdentityDbContext verification = scenario.CreateDbContext();
            Assert.AreEqual(1, await verification.OrganizationOwnerInvitations.CountAsync());
            Assert.AreEqual(
                1,
                await verification.OutboxMessages.CountAsync(item =>
                    item.Type == "OrganizationOwnerInvited"));
        });
    }

    [TestMethod]
    public async Task RevocationWinsAndTokenCannotCreateMembership()
    {
        await RunScenarioAsync(async scenario =>
        {
            CreatedOrganizationOwnerInvitationResult created = await scenario.CreateAsync(
                "owner-invitation-revoke-key");
            OrganizationOwnerInvitationSummaryResult revoked = await scenario.RevokeAsync(
                created.InvitationId,
                created.Version);

            Assert.AreEqual(OrganizationOwnerInvitationStatuses.Revoked, revoked.Status);
            Assert.IsNotNull(revoked.RevokedAtUtc);
            IdentityOperationException error =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.AcceptAsync(created.Token!, scenario.Invitee));
            Assert.AreEqual(
                "identity.organization_owner_invitation_not_available",
                error.Code);

            await using IdentityDbContext verification = scenario.CreateDbContext();
            Assert.AreEqual(
                0,
                await verification.AuthoritativeMemberships.CountAsync(item =>
                    item.OrganizationId == scenario.OrganizationId &&
                    item.UserId == scenario.Invitee.UserId));
            Assert.AreEqual(
                1,
                await verification.OutboxMessages.CountAsync(item =>
                    item.Type == "OrganizationOwnerInvitationRevoked"));
        });
    }

    [TestMethod]
    public async Task StrongAuthenticationBoundaryAndForeignOwnerFailClosed()
    {
        await RunScenarioAsync(async scenario =>
        {
            await using (IdentityDbContext dbContext = scenario.CreateDbContext())
            {
                Assert.AreEqual(
                    1,
                    await dbContext.Sessions
                        .Where(item => item.Id == scenario.Owner.SessionId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(
                            item => item.StrongAuthenticatedAtUtc,
                            Now.AddMinutes(-5))));
            }
            IdentityOperationException staleError =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.CreateAsync("owner-invitation-stale-key", scenario.Owner));
            Assert.AreEqual("identity.strong_authentication_required", staleError.Code);

            IdentityOperationException foreignError =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.ListAsync(scenario.Attacker));
            Assert.AreEqual(
                "identity.organization_owner_invitation_not_available",
                foreignError.Code);
        });
    }

    [TestMethod]
    public async Task RetiringAReferencedHmacKeyFailsClosedWithoutMembershipEffect()
    {
        await RunScenarioAsync(async scenario =>
        {
            CreatedOrganizationOwnerInvitationResult created = await scenario.CreateAsync(
                "owner-invitation-rotation-key");
            OrganizationOwnerInvitationOptions retiredV1 = new()
            {
                Enabled = true,
                Lifetime = TimeSpan.FromDays(7),
                CurrentKeyVersion = "test-v2",
                HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["test-v2"] = Convert.ToBase64String(
                        Enumerable.Repeat((byte)0x6b, 32).ToArray()),
                },
            };

            IdentityOperationException error =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.AcceptAsync(created.Token!, scenario.Invitee, retiredV1));

            Assert.AreEqual("identity.organization_owner_invitation_unavailable", error.Code);
            await using IdentityDbContext verification = scenario.CreateDbContext();
            Assert.AreEqual(
                0,
                await verification.AuthoritativeMemberships.CountAsync(item =>
                    item.OrganizationId == scenario.OrganizationId &&
                    item.UserId == scenario.Invitee.UserId));
        });
    }

    [TestMethod]
    public async Task AcceptAndRevokeRaceProducesExactlyOneTerminalEffect()
    {
        await RunScenarioAsync(async scenario =>
        {
            CreatedOrganizationOwnerInvitationResult created = await scenario.CreateAsync(
                "owner-invitation-terminal-race-key");
            Task<(bool Succeeded, Exception? Error)> accept = CaptureAsync(() =>
                scenario.AcceptAsync(created.Token!, scenario.Invitee));
            Task<(bool Succeeded, Exception? Error)> revoke = CaptureAsync(() =>
                scenario.RevokeAsync(created.InvitationId, created.Version));

            (bool Succeeded, Exception? Error)[] outcomes = await Task.WhenAll(accept, revoke);
            Assert.AreEqual(1, outcomes.Count(outcome => outcome.Succeeded));
            Exception loser = outcomes.Single(outcome => !outcome.Succeeded).Error!;
            Assert.IsTrue(loser is IdentityOperationException or DbUpdateConcurrencyException ||
                loser is Npgsql.PostgresException postgres &&
                postgres.SqlState is "40001" or "40P01");

            await using IdentityDbContext verification = scenario.CreateDbContext();
            OrganizationOwnerInvitation invitation = await verification.OrganizationOwnerInvitations
                .SingleAsync(item => item.Id == created.InvitationId);
            Assert.IsTrue(invitation.Status is
                OrganizationOwnerInvitationStatuses.Accepted or
                OrganizationOwnerInvitationStatuses.Revoked);
            int membershipCount = await verification.AuthoritativeMemberships.CountAsync(item =>
                item.OrganizationId == scenario.OrganizationId &&
                item.UserId == scenario.Invitee.UserId);
            Assert.IsTrue(membershipCount is 0 or 1);
            Assert.AreEqual(
                invitation.Status == OrganizationOwnerInvitationStatuses.Accepted ? 1 : 0,
                membershipCount);
            Assert.AreEqual(
                1,
                await verification.OutboxMessages.CountAsync(item =>
                    item.Type == "OrganizationOwnerInvitationAccepted" ||
                    item.Type == "OrganizationOwnerInvitationRevoked"));
            Assert.AreEqual(
                1,
                await verification.SecurityJournalEntries.CountAsync(item =>
                    item.Action == "organization_owner_invitation_accepted" ||
                    item.Action == "organization_owner_invitation_revoked"));
        });
    }

    [TestMethod]
    public async Task ExactExpiryBoundaryIsUnavailableWithoutMembershipEffect()
    {
        await RunScenarioAsync(async scenario =>
        {
            CreatedOrganizationOwnerInvitationResult created = await scenario.CreateAsync(
                "owner-invitation-expiry-boundary-key");
            await using (IdentityDbContext dbContext = scenario.CreateDbContext())
            {
                Assert.AreEqual(
                    1,
                    await dbContext.Sessions
                        .Where(item => item.Id == scenario.Invitee.SessionId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(
                            item => item.AuthenticatedAtUtc,
                            created.ExpiresAtUtc)));
            }
            AuthenticatedSession freshlyAuthenticatedInvitee = scenario.Invitee with
            {
                AuthenticatedAtUtc = created.ExpiresAtUtc,
            };

            IdentityOperationException error =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.AcceptAsync(
                        created.Token!,
                        freshlyAuthenticatedInvitee,
                        operationNow: created.ExpiresAtUtc));

            Assert.AreEqual(
                "identity.organization_owner_invitation_not_available",
                error.Code);
            await using IdentityDbContext verification = scenario.CreateDbContext();
            Assert.AreEqual(
                OrganizationOwnerInvitationStatuses.Pending,
                (await verification.OrganizationOwnerInvitations.SingleAsync(item =>
                    item.Id == created.InvitationId)).Status);
            Assert.AreEqual(
                0,
                await verification.AuthoritativeMemberships.CountAsync(item =>
                    item.OrganizationId == scenario.OrganizationId &&
                    item.UserId == scenario.Invitee.UserId));
        });
    }

    [TestMethod]
    public async Task JournalAndOutboxFaultsRollBackAcceptanceCompletely()
    {
        await RunScenarioAsync(async scenario =>
        {
            foreach (string faultTarget in AcceptanceFaultTargets)
            {
                CreatedOrganizationOwnerInvitationResult created = await scenario.CreateAsync(
                    $"owner-invitation-{faultTarget}-fault-key");
                await scenario.InstallAcceptanceFaultAsync(faultTarget);

                await Assert.ThrowsExactlyAsync<DbUpdateException>(() =>
                    scenario.AcceptAsync(created.Token!, scenario.Invitee));

                await scenario.RemoveAcceptanceFaultAsync(faultTarget);
                await using IdentityDbContext verification = scenario.CreateDbContext();
                OrganizationOwnerInvitation invitation = await verification
                    .OrganizationOwnerInvitations
                    .SingleAsync(item => item.Id == created.InvitationId);
                Assert.AreEqual(OrganizationOwnerInvitationStatuses.Pending, invitation.Status);
                Assert.AreEqual(
                    0,
                    await verification.AuthoritativeMemberships.CountAsync(item =>
                        item.OrganizationId == scenario.OrganizationId &&
                        item.UserId == scenario.Invitee.UserId));
                Assert.AreEqual(
                    0,
                    await verification.Memberships.CountAsync(item =>
                        item.OrganizationId == scenario.OrganizationId &&
                        item.UserId == scenario.Invitee.UserId));
                Assert.AreEqual(
                    0,
                    await verification.SecurityJournalEntries.CountAsync(item =>
                        item.Action == "organization_owner_invitation_accepted"));
                Assert.AreEqual(
                    0,
                    await verification.OutboxMessages.CountAsync(item =>
                        item.Type == "OrganizationOwnerInvitationAccepted"));
            }
        });
    }

    [TestMethod]
    public async Task DisabledFeatureFailsClosedForListAndRevokeWithoutEffect()
    {
        await RunScenarioAsync(async scenario =>
        {
            CreatedOrganizationOwnerInvitationResult created = await scenario.CreateAsync(
                "owner-invitation-disabled-key");
            OrganizationOwnerInvitationOptions disabled = InvitationScenario.InvitationOptions();
            disabled.Enabled = false;

            IdentityOperationException listError =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.ListAsync(scenario.Owner, disabled));
            IdentityOperationException revokeError =
                await Assert.ThrowsExactlyAsync<IdentityOperationException>(() =>
                    scenario.RevokeAsync(created.InvitationId, created.Version, disabled));

            Assert.AreEqual("identity.organization_owner_invitation_unavailable", listError.Code);
            Assert.AreEqual("identity.organization_owner_invitation_unavailable", revokeError.Code);
            await using IdentityDbContext verification = scenario.CreateDbContext();
            OrganizationOwnerInvitation persisted = await verification
                .OrganizationOwnerInvitations
                .SingleAsync(item => item.Id == created.InvitationId);
            Assert.AreEqual(OrganizationOwnerInvitationStatuses.Pending, persisted.Status);
            Assert.AreEqual(
                0,
                await verification.OutboxMessages.CountAsync(item =>
                    item.Type == "OrganizationOwnerInvitationRevoked"));
        });
    }

    private static async Task RunScenarioAsync(Func<InvitationScenario, Task> test)
    {
        PostgreSqlTestServer postgresql = IdentityTestAssembly.PostgreSql
            ?? throw new AssertFailedException(
                "PostgreSQL integration fixture could not start: " +
                IdentityTestAssembly.StartupError?.Message);
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            InvitationScenario scenario = await InvitationScenario.CreateAsync(connectionString);
            await test(scenario);
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    private sealed class InvitationScenario(
        string connectionString,
        Guid organizationId,
        AuthenticatedSession owner,
        AuthenticatedSession invitee,
        AuthenticatedSession attacker)
    {
        public Guid OrganizationId { get; } = organizationId;

        public AuthenticatedSession Owner { get; } = owner;

        public AuthenticatedSession Invitee { get; } = invitee;

        public AuthenticatedSession Attacker { get; } = attacker;

        public static async Task<InvitationScenario> CreateAsync(string connectionString)
        {
            await using IdentityDbContext dbContext = CreateDbContextFor(connectionString);
            await dbContext.Database.MigrateAsync();
            Guid organizationId = Guid.NewGuid();
            AuthenticatedSession owner = CreateSession(Now, strong: true);
            AuthenticatedSession invitee = CreateSession(Now, strong: false);
            AuthenticatedSession attacker = CreateSession(Now, strong: true);
            dbContext.Users.AddRange(
                new PlatformUser(owner.UserId, "Owner", Now.AddDays(-1)),
                new PlatformUser(invitee.UserId, "Invitee", Now.AddDays(-1)),
                new PlatformUser(attacker.UserId, "Attacker", Now.AddDays(-1)));
            dbContext.Sessions.AddRange(
                ToStoredSession(owner, 0x11),
                ToStoredSession(invitee, 0x22),
                ToStoredSession(attacker, 0x33));
            dbContext.Organizations.Add(new OrganizationDirectoryEntry(
                organizationId,
                "La Invitación",
                owner.UserId,
                Now.AddDays(-1)));
            dbContext.AuthoritativeMemberships.Add(new OrganizationMembershipAssignment(
                Guid.NewGuid(),
                organizationId,
                owner.UserId,
                Now.AddDays(-1)));
            dbContext.Memberships.Add(new OrganizationMembership(
                owner.UserId,
                organizationId,
                "La Invitación",
                OrganizationMembershipRoles.Owner));
            await dbContext.SaveChangesAsync();
            return new InvitationScenario(
                connectionString,
                organizationId,
                owner,
                invitee,
                attacker);
        }

        public async Task<CreatedOrganizationOwnerInvitationResult> CreateAsync(
            string idempotencyKey,
            AuthenticatedSession? actingSession = null) =>
            await WithServiceAsync((service, cancellationToken) =>
                service.CreateOrganizationOwnerInvitationAsync(
                    new CreateOrganizationOwnerInvitationCommand(
                        OrganizationId,
                        idempotencyKey),
                    actingSession ?? Owner,
                    IdentityRequestContext.ForTenant(
                        "invitation-create-test",
                        (actingSession ?? Owner).UserId,
                        OrganizationId),
                    cancellationToken));

        public async Task<AcceptedOrganizationOwnerInvitationResult> AcceptAsync(
            string token,
            AuthenticatedSession actingSession,
            OrganizationOwnerInvitationOptions? invitationOptions = null,
            DateTimeOffset? operationNow = null) =>
            await WithServiceAsync((service, cancellationToken) =>
                service.AcceptOrganizationOwnerInvitationAsync(
                    new AcceptOrganizationOwnerInvitationCommand(token),
                    actingSession,
                    IdentityRequestContext.ForPlatform(
                        "invitation-accept-test",
                        actingSession.UserId),
                    cancellationToken),
                invitationOptions,
                operationNow);

        public async Task<OrganizationOwnerInvitationSummaryResult> RevokeAsync(
            Guid invitationId,
            Guid version,
            OrganizationOwnerInvitationOptions? invitationOptions = null) =>
            await WithServiceAsync((service, cancellationToken) =>
                service.RevokeOrganizationOwnerInvitationAsync(
                    new RevokeOrganizationOwnerInvitationCommand(
                        OrganizationId,
                        invitationId,
                        version),
                    Owner,
                    IdentityRequestContext.ForTenant(
                        "invitation-revoke-test",
                        Owner.UserId,
                        OrganizationId),
                    cancellationToken),
                invitationOptions);

        public async Task<IReadOnlyList<OrganizationOwnerInvitationSummaryResult>> ListAsync(
            AuthenticatedSession actingSession,
            OrganizationOwnerInvitationOptions? invitationOptions = null) =>
            await WithServiceAsync((service, cancellationToken) =>
                service.ListOrganizationOwnerInvitationsAsync(
                    OrganizationId,
                    actingSession,
                    IdentityRequestContext.ForTenant(
                        "invitation-list-test",
                        actingSession.UserId,
                        OrganizationId),
                    cancellationToken),
                invitationOptions);

        public IdentityDbContext CreateDbContext() => CreateDbContextFor(connectionString);

        public async Task InstallAcceptanceFaultAsync(string faultTarget)
        {
            string sql = faultTarget switch
            {
                "journal" =>
                    """
                    CREATE OR REPLACE FUNCTION identity.test_fail_journal_acceptance()
                    RETURNS trigger
                    LANGUAGE plpgsql
                    AS $function$
                    BEGIN
                        IF NEW."Action" = 'organization_owner_invitation_accepted' THEN
                            RAISE EXCEPTION 'injected acceptance failure';
                        END IF;
                        RETURN NEW;
                    END
                    $function$;
                    CREATE TRIGGER test_fail_journal_acceptance
                    BEFORE INSERT ON identity.audit_events
                    FOR EACH ROW EXECUTE FUNCTION identity.test_fail_journal_acceptance();
                    """,
                "outbox" =>
                    """
                    CREATE OR REPLACE FUNCTION identity.test_fail_outbox_acceptance()
                    RETURNS trigger
                    LANGUAGE plpgsql
                    AS $function$
                    BEGIN
                        IF NEW."Type" = 'OrganizationOwnerInvitationAccepted' THEN
                            RAISE EXCEPTION 'injected acceptance failure';
                        END IF;
                        RETURN NEW;
                    END
                    $function$;
                    CREATE TRIGGER test_fail_outbox_acceptance
                    BEFORE INSERT ON identity.outbox_messages
                    FOR EACH ROW EXECUTE FUNCTION identity.test_fail_outbox_acceptance();
                    """,
                _ => throw new ArgumentOutOfRangeException(nameof(faultTarget)),
            };
            await using IdentityDbContext dbContext = CreateDbContext();
            await dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task RemoveAcceptanceFaultAsync(string faultTarget)
        {
            string sql = faultTarget switch
            {
                "journal" =>
                    """
                    DROP TRIGGER IF EXISTS test_fail_journal_acceptance
                        ON identity.audit_events;
                    DROP FUNCTION IF EXISTS identity.test_fail_journal_acceptance();
                    """,
                "outbox" =>
                    """
                    DROP TRIGGER IF EXISTS test_fail_outbox_acceptance
                        ON identity.outbox_messages;
                    DROP FUNCTION IF EXISTS identity.test_fail_outbox_acceptance();
                    """,
                _ => throw new ArgumentOutOfRangeException(nameof(faultTarget)),
            };
            await using IdentityDbContext dbContext = CreateDbContext();
            await dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        private async Task<TResult> WithServiceAsync<TResult>(
            Func<IdentityApplicationService, CancellationToken, Task<TResult>> operation,
            OrganizationOwnerInvitationOptions? invitationOptions = null,
            DateTimeOffset? operationNow = null)
        {
            await using IdentityDbContext dbContext = CreateDbContext();
            await using ServiceProvider services = new ServiceCollection()
                .AddMetrics()
                .BuildServiceProvider();
            IdentityApplicationService service = new(
                dbContext,
                new IdentityTokenService(),
                new IdentityTelemetry(services.GetRequiredService<IMeterFactory>()),
                new FixedTimeProvider(operationNow ?? Now),
                Options.Create(new IdentityRuntimeOptions()),
                Options.Create(new OrganizationBootstrapOptions()),
                organizationOwnerInvitationOptions: Options.Create(
                    invitationOptions ?? InvitationOptions()));
            return await operation(service, CancellationToken.None);
        }

        private static IdentityDbContext CreateDbContextFor(string connectionString) =>
            new(new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(connectionString)
                .Options);

        private static AuthenticatedSession CreateSession(DateTimeOffset now, bool strong)
        {
            Guid userId = Guid.NewGuid();
            Guid sessionId = Guid.NewGuid();
            return new AuthenticatedSession(
                sessionId,
                userId,
                now,
                true,
                strong ? now : null,
                strong ? StepUpPurposes.ManageOrganizationOwners : null);
        }

        private static UserSession ToStoredSession(AuthenticatedSession session, byte tokenByte) =>
            new(
                session.SessionId,
                session.UserId,
                Enumerable.Repeat(tokenByte, 32).ToArray(),
                session.AuthenticatedAtUtc,
                Now.AddDays(30),
                session.IsAuthenticationAssuranceVerified,
                session.StrongAuthenticatedAtUtc,
                session.StrongAuthenticationPurpose);

        public static OrganizationOwnerInvitationOptions InvitationOptions() =>
            new()
            {
                Enabled = true,
                Lifetime = TimeSpan.FromDays(7),
                CurrentKeyVersion = "test-v1",
                HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["test-v1"] = Convert.ToBase64String(
                        Enumerable.Repeat((byte)0x5a, 32).ToArray()),
                },
            };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static DateTimeOffset CurrentTestTime()
    {
        DateTimeOffset value = DateTimeOffset.UtcNow;
        return value.AddTicks(-(value.Ticks % TimeSpan.TicksPerSecond));
    }

    private static async Task<(bool Succeeded, Exception? Error)> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
    }
}

using System.Diagnostics.Metrics;
using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrganizationCreateApplicationTests
{
    private const string RotationKey = "organization-create-rotation-01";
    private static readonly string[] RotationKeyVersions = ["v1", "v2"];

    [TestMethod]
    public async Task AuthenticationExactlyFifteenMinutesOldIsRejectedBeforeLedgerAccess()
    {
        PostgreSqlTestServer postgresql = IdentityTestAssembly.PostgreSql
            ?? throw new AssertFailedException(
                "PostgreSQL integration fixture could not start: "
                + IdentityTestAssembly.StartupError?.Message);
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            DbContextOptions<IdentityDbContext> dbOptions =
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;
            await using IdentityDbContext dbContext = new(dbOptions);
            await dbContext.Database.MigrateAsync();
            DateTimeOffset now = new(2026, 8, 11, 1, 0, 0, TimeSpan.Zero);
            Guid userId = Guid.NewGuid();
            Guid sessionId = Guid.NewGuid();
            dbContext.Users.Add(new PlatformUser(userId, "Owner", now.AddDays(-1)));
            dbContext.Sessions.Add(new UserSession(
                sessionId,
                userId,
                Enumerable.Repeat((byte)7, 32).ToArray(),
                now.AddMinutes(-15),
                now.AddHours(1),
                isAuthenticationAssuranceVerified: true));
            await dbContext.SaveChangesAsync();

            await using ServiceProvider services = new ServiceCollection()
                .AddMetrics()
                .BuildServiceProvider();
            IdentityApplicationService service = new(
                dbContext,
                new IdentityTokenService(),
                new IdentityTelemetry(services.GetRequiredService<IMeterFactory>()),
                new FixedTimeProvider(now),
                Options.Create(new IdentityRuntimeOptions()),
                Options.Create(new OrganizationBootstrapOptions
                {
                    Enabled = true,
                    CurrentKeyVersion = "test-v1",
                    IdempotencyHmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=",
                    },
                }));

            IdentityOperationException error = await Assert.ThrowsExactlyAsync<IdentityOperationException>(
                () => service.CreateOrganizationAsync(
                    new CreateOrganizationCommand("La Esquina", "organization-create-boundary"),
                    new AuthenticatedSession(sessionId, userId, now.AddMinutes(-15), true),
                    IdentityRequestContext.ForPlatform("boundary-test", userId),
                    CancellationToken.None));

            Assert.AreEqual("identity.reauthentication_required", error.Code);
            Assert.AreEqual(0, await dbContext.OrganizationCreationLedgers.CountAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task MissingApplicationRoleMembershipFailsClosedBeforeLedgerOrBusinessEffects()
    {
        PostgreSqlTestServer postgresql = IdentityTestAssembly.PostgreSql
            ?? throw new AssertFailedException(
                "PostgreSQL integration fixture could not start: "
                + IdentityTestAssembly.StartupError?.Message);
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        string loginRole = $"agro_unprivileged_{Guid.NewGuid():N}";
        const string password = "test-only-role-password-32-characters";
        try
        {
            DbContextOptions<IdentityDbContext> adminOptions =
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;
            await using IdentityDbContext adminDbContext = new(adminOptions);
            await adminDbContext.Database.MigrateAsync();
            await using (NpgsqlConnection adminConnection = new(connectionString))
            {
                await adminConnection.OpenAsync();
                await using NpgsqlCommand createRole = adminConnection.CreateCommand();
                createRole.CommandText = $"CREATE ROLE {loginRole} " +
                    $"LOGIN PASSWORD '{password}' " +
                    "NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS";
                await createRole.ExecuteNonQueryAsync();
            }

            NpgsqlConnectionStringBuilder restrictedConnection = new(connectionString)
            {
                Username = loginRole,
                Password = password,
                Pooling = false,
            };
            DbContextOptions<IdentityDbContext> restrictedOptions =
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseNpgsql(restrictedConnection.ConnectionString)
                    .Options;
            await using IdentityDbContext restrictedDbContext = new(restrictedOptions);
            await using ServiceProvider services = new ServiceCollection()
                .AddMetrics()
                .BuildServiceProvider();
            DateTimeOffset now = new(2026, 8, 11, 1, 0, 0, TimeSpan.Zero);
            Guid userId = Guid.NewGuid();
            Guid sessionId = Guid.NewGuid();
            IdentityApplicationService service = new(
                restrictedDbContext,
                new IdentityTokenService(),
                new IdentityTelemetry(services.GetRequiredService<IMeterFactory>()),
                new FixedTimeProvider(now),
                Options.Create(new IdentityRuntimeOptions()),
                Options.Create(EnabledBootstrapOptions()));

            IdentityOperationException error = await Assert.ThrowsExactlyAsync<IdentityOperationException>(
                () => service.CreateOrganizationAsync(
                    new CreateOrganizationCommand("La Esquina", "organization-create-no-role"),
                    new AuthenticatedSession(sessionId, userId, now, true),
                    IdentityRequestContext.ForPlatform("role-test", userId),
                    CancellationToken.None));

            Assert.AreEqual("identity.organization_creation_unavailable", error.Code);
            Assert.AreEqual(0, await adminDbContext.OrganizationCreationLedgers.CountAsync());
            Assert.AreEqual(0, await adminDbContext.Organizations.CountAsync());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using NpgsqlConnection adminConnection = new(connectionString);
            await adminConnection.OpenAsync();
            await using NpgsqlCommand dropRole = adminConnection.CreateCommand();
            dropRole.CommandText = $"DROP ROLE IF EXISTS {loginRole}";
            await dropRole.ExecuteNonQueryAsync();
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task ReplayBackfillsCurrentAliasBeforeRetiringThePreviousKey()
    {
        PostgreSqlTestServer postgresql = IdentityTestAssembly.PostgreSql
            ?? throw new AssertFailedException(
                "PostgreSQL integration fixture could not start: "
                + IdentityTestAssembly.StartupError?.Message);
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            DateTimeOffset now = new(2026, 8, 11, 2, 0, 0, TimeSpan.Zero);
            (Guid userId, Guid sessionId) = await SeedAuthorizedSessionAsync(
                connectionString,
                now);
            AuthenticatedSession session = new(sessionId, userId, now, true);
            IdentityRequestContext requestContext = IdentityRequestContext.ForPlatform(
                "rotation-test",
                userId);

            CreatedOrganizationResult initial = await CreateOrganizationAsync(
                connectionString,
                now,
                session,
                requestContext,
                BootstrapOptions("v1", ("v1", 0x11)));
            CreatedOrganizationResult rotatedReplay = await CreateOrganizationAsync(
                connectionString,
                now,
                session,
                requestContext,
                BootstrapOptions("v2", ("v1", 0x11), ("v2", 0x22)));
            CreatedOrganizationResult retiredReplay = await CreateOrganizationAsync(
                connectionString,
                now,
                session,
                requestContext,
                BootstrapOptions("v2", ("v2", 0x22)));

            Assert.AreEqual(initial, rotatedReplay);
            Assert.AreEqual(initial, retiredReplay);
            await using IdentityDbContext verification = CreateDbContext(connectionString);
            Assert.AreEqual(1, await verification.Organizations.CountAsync());
            Assert.AreEqual(1, await verification.OrganizationCreationLedgers.CountAsync());
            Assert.AreEqual(2, await verification.OrganizationCreationKeyAliases.CountAsync());
            CollectionAssert.AreEquivalent(
                RotationKeyVersions,
                await verification.OrganizationCreationKeyAliases
                    .Select(alias => alias.KeyVersion)
                    .ToArrayAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task RetiringAnUncoveredKeyFailsClosedWithoutASecondEffect()
    {
        PostgreSqlTestServer postgresql = IdentityTestAssembly.PostgreSql
            ?? throw new AssertFailedException(
                "PostgreSQL integration fixture could not start: "
                + IdentityTestAssembly.StartupError?.Message);
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            DateTimeOffset now = new(2026, 8, 11, 2, 30, 0, TimeSpan.Zero);
            (Guid userId, Guid sessionId) = await SeedAuthorizedSessionAsync(
                connectionString,
                now);
            AuthenticatedSession session = new(sessionId, userId, now, true);
            IdentityRequestContext requestContext = IdentityRequestContext.ForPlatform(
                "early-retirement-test",
                userId);
            _ = await CreateOrganizationAsync(
                connectionString,
                now,
                session,
                requestContext,
                BootstrapOptions("v1", ("v1", 0x11)));

            IdentityOperationException error = await Assert.ThrowsExactlyAsync<IdentityOperationException>(
                () => CreateOrganizationAsync(
                    connectionString,
                    now,
                    session,
                    requestContext,
                    BootstrapOptions("v2", ("v2", 0x22))));

            Assert.AreEqual("identity.organization_creation_unavailable", error.Code);
            await using IdentityDbContext verification = CreateDbContext(connectionString);
            Assert.AreEqual(1, await verification.Organizations.CountAsync());
            Assert.AreEqual(1, await verification.OrganizationCreationLedgers.CountAsync());
            Assert.AreEqual(1, await verification.OrganizationCreationKeyAliases.CountAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    private static async Task<CreatedOrganizationResult> CreateOrganizationAsync(
        string connectionString,
        DateTimeOffset now,
        AuthenticatedSession session,
        IdentityRequestContext requestContext,
        OrganizationBootstrapOptions bootstrapOptions)
    {
        await using IdentityDbContext dbContext = CreateDbContext(connectionString);
        await using ServiceProvider services = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        IdentityApplicationService service = new(
            dbContext,
            new IdentityTokenService(),
            new IdentityTelemetry(services.GetRequiredService<IMeterFactory>()),
            new FixedTimeProvider(now),
            Options.Create(new IdentityRuntimeOptions()),
            Options.Create(bootstrapOptions));
        return await service.CreateOrganizationAsync(
            new CreateOrganizationCommand("La Rotación", RotationKey),
            session,
            requestContext,
            CancellationToken.None);
    }

    private static async Task<(Guid UserId, Guid SessionId)> SeedAuthorizedSessionAsync(
        string connectionString,
        DateTimeOffset now)
    {
        await using IdentityDbContext dbContext = CreateDbContext(connectionString);
        await dbContext.Database.MigrateAsync();
        Guid userId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        dbContext.Users.Add(new PlatformUser(userId, "Rotation owner", now.AddDays(-1)));
        dbContext.Sessions.Add(new UserSession(
            sessionId,
            userId,
            Enumerable.Repeat((byte)0x5a, 32).ToArray(),
            now,
            now.AddHours(1),
            isAuthenticationAssuranceVerified: true));
        await dbContext.SaveChangesAsync();
        return (userId, sessionId);
    }

    private static IdentityDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static OrganizationBootstrapOptions BootstrapOptions(
        string currentVersion,
        params (string Version, byte Value)[] keys) =>
        new()
        {
            Enabled = true,
            CurrentKeyVersion = currentVersion,
            IdempotencyHmacKeys = keys.ToDictionary(
                item => item.Version,
                item => Convert.ToBase64String(
                    Enumerable.Repeat(item.Value, 32).ToArray()),
                StringComparer.Ordinal),
        };

    private static OrganizationBootstrapOptions EnabledBootstrapOptions() =>
        new()
        {
            Enabled = true,
            CurrentKeyVersion = "test-v1",
            IdempotencyHmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=",
            },
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

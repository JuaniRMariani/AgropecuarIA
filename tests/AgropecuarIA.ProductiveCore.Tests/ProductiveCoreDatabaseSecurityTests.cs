using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using AgropecuarIA.ProductiveCore.Infrastructure;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace AgropecuarIA.ProductiveCore.Tests;

#pragma warning disable CA1861 // Database probes intentionally use compact inline arguments.

[TestClass]
[DoNotParallelize]
public sealed class ProductiveCoreDatabaseSecurityTests
{
    private const string IdentityBeforeAuthorizationPort =
        "20260818153846_AddOrganizationOwnerRemoval";

    [TestMethod]
    public async Task MigrationRequiresIdentityPortAndSupportsEmptyToNAndEphemeralRollbackForward()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using var identity = CreateIdentityDbContext(connectionString);
            IMigrator identityMigrator = identity.Database.GetService<IMigrator>();
            await identityMigrator.MigrateAsync(IdentityBeforeAuthorizationPort);

            await using var productive = CreateProductiveDbContext(connectionString);
            IMigrator productiveMigrator = productive.Database.GetService<IMigrator>();
            PostgresException missingPort = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await productiveMigrator.MigrateAsync());
            Assert.AreEqual(PostgresErrorCodes.UndefinedFunction, missingPort.SqlState);

            await identityMigrator.MigrateAsync();
            await productiveMigrator.MigrateAsync();
            await using (var expanded = new NpgsqlConnection(connectionString))
            {
                await expanded.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT to_regclass('productive_core.management_units') IS NOT NULL
                       AND to_regclass('productive_core.management_unit_creation_ledgers') IS NOT NULL
                       AND to_regclass('productive_core.management_unit_creation_key_aliases') IS NOT NULL
                       AND to_regclass('productive_core.journal_entries') IS NOT NULL
                       AND to_regclass('productive_core.outbox_messages') IS NOT NULL
                    """));
            }

            PostgresException wrongRollbackOrder =
                await Assert.ThrowsExactlyAsync<PostgresException>(
                    async () => await identityMigrator.MigrateAsync(
                        IdentityBeforeAuthorizationPort));
            Assert.AreEqual(PostgresErrorCodes.DependentObjectsStillExist, wrongRollbackOrder.SqlState);

            await productiveMigrator.MigrateAsync(Migration.InitialDatabase);
            await identityMigrator.MigrateAsync(IdentityBeforeAuthorizationPort);
            await using (var rolledBack = new NpgsqlConnection(connectionString))
            {
                await rolledBack.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regclass('productive_core.management_units') IS NOT NULL"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regprocedure('identity.authorize_productive_owner()') IS NOT NULL"));
            }

            await identityMigrator.MigrateAsync();
            await productiveMigrator.MigrateAsync();
            Assert.IsEmpty(await identity.Database.GetPendingMigrationsAsync());
            Assert.IsEmpty(await productive.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task RolesGrantsRlsAndLiveAuthorizationIsolateTenantsAndResetPoolContext()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();

        Assert.IsTrue(await ScalarBooleanAsync(
            admin,
            """
            SELECT bool_and(NOT rolcanlogin AND NOT rolinherit AND NOT rolsuper
                            AND NOT rolcreatedb AND NOT rolcreaterole
                            AND NOT rolreplication AND NOT rolbypassrls)
            FROM pg_roles
            WHERE rolname IN ('agro_productive_owner', 'agro_productive_migrator',
                              'agro_productive_app', 'agro_productive_job')
            """));
        Assert.IsTrue(await ScalarBooleanAsync(
            admin,
            """
            SELECT bool_and(c.relrowsecurity AND c.relforcerowsecurity
                            AND pg_get_userbyid(c.relowner) = 'agro_productive_owner')
            FROM pg_class AS c
            JOIN pg_namespace AS n ON n.oid = c.relnamespace
            WHERE n.nspname = 'productive_core'
              AND c.relname IN ('management_units',
                                'management_unit_creation_ledgers',
                                'management_unit_creation_key_aliases',
                                'journal_entries', 'outbox_messages')
            """));
        Assert.IsFalse(await ScalarBooleanAsync(
            admin,
            """
            SELECT has_table_privilege('agro_productive_job',
                                       'productive_core.management_units', 'SELECT')
                OR has_table_privilege('agro_productive_app',
                                       'identity.sessions', 'SELECT')
                OR has_table_privilege('agro_productive_app',
                                       'identity.memberships', 'SELECT')
                OR has_table_privilege('agro_productive_app',
                                       'identity.organizations', 'SELECT')
            """));

        var poolBuilder = new NpgsqlConnectionStringBuilder(scenario.RuntimeConnectionString)
        {
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 1,
        };
        await using var pooled = new NpgsqlConnection(poolBuilder.ConnectionString);
        await pooled.OpenAsync();

        await using (NpgsqlTransaction transaction = await BeginAuthorizedAsync(
            pooled,
            scenario.FirstActorId,
            scenario.FirstOrganizationId,
            scenario.FirstSessionId,
            scenario.FirstAuthorizationVersion))
        {
            await InsertManagementUnitAsync(
                pooled,
                transaction,
                Guid.NewGuid(),
                scenario.FirstOrganizationId,
                "North field");
            await InsertManagementUnitAsync(
                pooled,
                transaction,
                Guid.NewGuid(),
                scenario.FirstOrganizationId,
                "North field");
            Assert.AreEqual(2L, await ScalarInt64Async(
                pooled,
                transaction,
                "SELECT count(*) FROM productive_core.management_units"));
            await transaction.CommitAsync();
        }

        await using (NpgsqlTransaction noContext = await pooled.BeginTransactionAsync())
        {
            await SetRoleAsync(pooled, noContext, "agro_productive_app");
            Assert.AreEqual(0L, await ScalarInt64Async(
                pooled,
                noContext,
                "SELECT count(*) FROM productive_core.management_units"));
            await noContext.RollbackAsync();
        }

        await using (NpgsqlTransaction foreignTenant = await BeginAuthorizedAsync(
            pooled,
            scenario.SecondActorId,
            scenario.SecondOrganizationId,
            scenario.SecondSessionId,
            scenario.SecondAuthorizationVersion))
        {
            Assert.AreEqual(0L, await ScalarInt64Async(
                pooled,
                foreignTenant,
                "SELECT count(*) FROM productive_core.management_units"));
            PostgresException crossTenantInsert =
                await Assert.ThrowsExactlyAsync<PostgresException>(
                    async () => await InsertManagementUnitAsync(
                        pooled,
                        foreignTenant,
                        Guid.NewGuid(),
                        scenario.FirstOrganizationId,
                        "Cross tenant field"));
            Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, crossTenantInsert.SqlState);
            await foreignTenant.RollbackAsync();
        }

        await using (NpgsqlTransaction job = await admin.BeginTransactionAsync())
        {
            await SetRoleAsync(admin, job, "agro_productive_job");
            PostgresException denied = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await ScalarInt64Async(
                    admin,
                    job,
                    "SELECT count(*) FROM productive_core.management_units"));
            Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            await job.RollbackAsync();
        }

        await ExecuteAsync(
            scenario.ConnectionString,
            """
            UPDATE identity.memberships
            SET "Status" = 'removed', "RemovedAtUtc" = now(),
                "RemovedByUserId" = @actor,
                "SecurityVersion" = "SecurityVersion" + 1,
                "Version" = gen_random_uuid()
            WHERE "OrganizationId" = @organization AND "UserId" = @actor
            """,
            ("actor", scenario.FirstActorId),
            ("organization", scenario.FirstOrganizationId));

        await using (NpgsqlTransaction removed = await pooled.BeginTransactionAsync())
        {
            await SetRoleAndBaseContextAsync(
                pooled,
                removed,
                "agro_productive_app",
                scenario.FirstActorId,
                scenario.FirstOrganizationId,
                scenario.FirstSessionId);
            Assert.IsNull(await ExecuteAuthorizationAsync(pooled, removed));
            Assert.AreEqual(0L, await ScalarInt64Async(
                pooled,
                removed,
                "SELECT count(*) FROM productive_core.management_units"));
            await removed.RollbackAsync();
        }
    }

    [TestMethod]
    public async Task IdempotencyCoverageAndAtomicWritesAreTenantBoundAndJournalIsAppendOnly()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid managementUnitId = Guid.NewGuid();
        Guid managementUnitVersion = Guid.NewGuid();
        Guid ledgerId = Guid.NewGuid();
        Guid leaseOwner = Guid.NewGuid();
        byte[] fingerprint = Enumerable.Repeat((byte)0x41, 32).ToArray();
        byte[] keyDigest = Enumerable.Repeat((byte)0x51, 32).ToArray();

        await using var connection = new NpgsqlConnection(scenario.RuntimeConnectionString);
        await connection.OpenAsync();
        await using (NpgsqlTransaction transaction = await BeginAuthorizedAsync(
            connection,
            scenario.FirstActorId,
            scenario.FirstOrganizationId,
            scenario.FirstSessionId,
            scenario.FirstAuthorizationVersion))
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO productive_core.management_units
                    ("Id", "OrganizationId", "DisplayName", "UnitType", "Status",
                     "SpatialStatus", "CreatedAtUtc", "Version")
                VALUES (@unit, @organization, 'Atomic field', 'field', 'draft',
                        'not_configured', now(), @unitVersion);
                INSERT INTO productive_core.management_unit_creation_ledgers
                    ("Id", "OrganizationId", "ScopeKind", "Namespace", "Operation",
                     "ContractVersion", "CanonicalizationVersion", "ActorUserId",
                     "SessionId", "AuthorizationVersion", "RequestFingerprint", "State",
                     "ManagementUnitId", "ResultVersion", "LeaseOwner", "FenceToken",
                     "LeaseUntilUtc", "StartedAtUtc", "CompletedAtUtc", "Version")
                VALUES (@ledger, @organization, 'tenant', 'management_unit', 'create_field',
                        1, 1, @actor, @session, @authorizationVersion, @fingerprint,
                        'succeeded', @unit, @unitVersion, @leaseOwner, 1,
                        now() + interval '1 minute', now(), now(), @ledgerVersion);
                INSERT INTO productive_core.management_unit_creation_key_aliases
                    ("Id", "LedgerId", "OrganizationId", "ScopeKind", "Namespace",
                     "Operation", "KeyVersion", "KeyDigest", "CreatedAtUtc")
                VALUES (gen_random_uuid(), @ledger, @organization, 'tenant',
                        'management_unit', 'create_field', 'v1', @keyDigest, now());
                INSERT INTO productive_core.journal_entries
                    ("Id", "OrganizationId", "ActorUserId", "SessionId", "Action",
                     "Outcome", "CorrelationId", "OccurredAtUtc")
                VALUES (gen_random_uuid(), @organization, @actor, @session,
                        'management_unit_created', 'succeeded', 'db-security', now());
                INSERT INTO productive_core.outbox_messages
                    ("Id", "OrganizationId", "EventType", "SchemaVersion", "Source", "Scope",
                     "AggregateType", "AggregateId", "AggregateVersion", "CorrelationId",
                     "OccurredAtUtc", "AvailableAtUtc", "PayloadJson")
                VALUES (gen_random_uuid(), @organization, 'ManagementUnitCreated', '1.0.0',
                        'productive-core', 'tenant',
                        'ManagementUnit', @unit, 1, 'db-security', now(), now(),
                        jsonb_build_object('organizationId', @organization,
                                           'managementUnitId', @unit));
                """,
                connection,
                transaction);
            command.Parameters.AddWithValue("unit", managementUnitId);
            command.Parameters.AddWithValue("organization", scenario.FirstOrganizationId);
            command.Parameters.AddWithValue("unitVersion", managementUnitVersion);
            command.Parameters.AddWithValue("ledger", ledgerId);
            command.Parameters.AddWithValue("actor", scenario.FirstActorId);
            command.Parameters.AddWithValue("session", scenario.FirstSessionId);
            command.Parameters.AddWithValue("authorizationVersion", scenario.FirstAuthorizationVersion);
            command.Parameters.AddWithValue("fingerprint", fingerprint);
            command.Parameters.AddWithValue("leaseOwner", leaseOwner);
            command.Parameters.AddWithValue("ledgerVersion", Guid.NewGuid());
            command.Parameters.AddWithValue("keyDigest", keyDigest);
            Assert.AreEqual(5, await command.ExecuteNonQueryAsync());

            Assert.IsTrue(await KeyCoverageAsync(connection, transaction, "v1"));
            Assert.IsFalse(await KeyCoverageAsync(connection, transaction, "v2"));

            PostgresException duplicate = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () =>
                {
                    await using var duplicateAlias = new NpgsqlCommand(
                        """
                        INSERT INTO productive_core.management_unit_creation_key_aliases
                            ("Id", "LedgerId", "OrganizationId", "ScopeKind", "Namespace",
                             "Operation", "KeyVersion", "KeyDigest", "CreatedAtUtc")
                        VALUES (gen_random_uuid(), @ledger, @organization, 'tenant',
                                'management_unit', 'create_field', 'v1', @keyDigest, now())
                        """,
                        connection,
                        transaction);
                    duplicateAlias.Parameters.AddWithValue("ledger", ledgerId);
                    duplicateAlias.Parameters.AddWithValue("organization", scenario.FirstOrganizationId);
                    duplicateAlias.Parameters.AddWithValue("keyDigest", keyDigest);
                    await duplicateAlias.ExecuteNonQueryAsync();
                });
            Assert.AreEqual(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
            await transaction.RollbackAsync();
        }

        await using (var admin = new NpgsqlConnection(scenario.ConnectionString))
        {
            await admin.OpenAsync();
            Assert.AreEqual(0L, await ScalarInt64Async(
                admin,
                null,
                "SELECT count(*) FROM productive_core.management_units"));
        }

        await using (NpgsqlTransaction committed = await BeginAuthorizedAsync(
            connection,
            scenario.FirstActorId,
            scenario.FirstOrganizationId,
            scenario.FirstSessionId,
            scenario.FirstAuthorizationVersion))
        {
            await InsertManagementUnitAsync(
                connection,
                committed,
                managementUnitId,
                scenario.FirstOrganizationId,
                "Committed field");
            await using var journal = new NpgsqlCommand(
                """
                INSERT INTO productive_core.journal_entries
                    ("Id", "OrganizationId", "ActorUserId", "SessionId", "Action",
                     "Outcome", "CorrelationId", "OccurredAtUtc")
                VALUES (gen_random_uuid(), @organization, @actor, @session,
                        'management_unit_created', 'succeeded', 'append-only', now())
                """,
                connection,
                committed);
            journal.Parameters.AddWithValue("organization", scenario.FirstOrganizationId);
            journal.Parameters.AddWithValue("actor", scenario.FirstActorId);
            journal.Parameters.AddWithValue("session", scenario.FirstSessionId);
            Assert.AreEqual(1, await journal.ExecuteNonQueryAsync());
            await committed.CommitAsync();
        }

        await using (var admin = new NpgsqlConnection(scenario.ConnectionString))
        {
            await admin.OpenAsync();
            PostgresException immutable = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await ExecuteOnConnectionAsync(
                    admin,
                    "UPDATE productive_core.journal_entries SET \"Outcome\" = 'changed'"));
            Assert.AreEqual(PostgresErrorCodes.RaiseException, immutable.SqlState);
        }
    }

    [TestMethod]
    [DataRow("management_unit_creation_ledgers")]
    [DataRow("journal_entries")]
    [DataRow("outbox_messages")]
    public async Task CreateFieldRollsBackEveryProductiveRowWhenACriticalInsertFails(
        string failingTable)
    {
        if (failingTable is not (
            "management_unit_creation_ledgers" or
            "journal_entries" or
            "outbox_messages"))
        {
            throw new ArgumentOutOfRangeException(nameof(failingTable));
        }

        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        string triggerName = $"test_fail_{failingTable}_insert";
        string functionName = $"productive_core.{triggerName}";
        await ExecuteAsync(
            scenario.ConnectionString,
            $"""
            CREATE FUNCTION {functionName}()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                RAISE EXCEPTION 'Productive Core test fault at {failingTable}';
            END;
            $function$;
            CREATE TRIGGER {triggerName}
            BEFORE INSERT ON productive_core.{failingTable}
            FOR EACH ROW EXECUTE FUNCTION {functionName}();
            """);

        using ServiceProvider metrics = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        var service = new ProductiveCoreApplicationService(
            new PostgresProductiveCoreUnitOfWorkFactory(
                new TestProductiveDbContextFactory(scenario.RuntimeConnectionString)),
            new ProductiveCoreTelemetry(
                metrics.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()),
            TimeProvider.System,
            Options.Create(new ManagementUnitCreationOptions
            {
                Enabled = true,
                CurrentKeyVersion = "v1",
                HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["v1"] = Convert.ToBase64String(
                        Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
                },
            }));
        var requestContext = new ProductiveRequestContext(
            $"fault-{failingTable}",
            scenario.FirstActorId,
            scenario.FirstSessionId,
            scenario.FirstOrganizationId);

        ProductiveCoreOperationException failure =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                service.CreateFieldAsync(
                    new CreateFieldCommand(
                        scenario.FirstOrganizationId,
                        "Atomic rollback field",
                        new string(failingTable[0], 32)),
                    requestContext,
                    CancellationToken.None));
        Assert.AreEqual("productive_core.management_unit_unavailable", failure.Code);

        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        foreach (string table in new[]
                 {
                     "management_units",
                     "management_unit_creation_ledgers",
                     "management_unit_creation_key_aliases",
                     "journal_entries",
                     "outbox_messages",
                 })
        {
            Assert.AreEqual(
                0L,
                await ScalarInt64Async(
                    admin,
                    null,
                    $"SELECT count(*) FROM productive_core.{table}"),
                $"{table} must be empty after the injected {failingTable} failure.");
        }
    }

    [TestMethod]
    public async Task RepositoryReturnsOverflowSentinelInsteadOfSilentlyTruncating()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        await using (var connection = new NpgsqlConnection(scenario.RuntimeConnectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlTransaction transaction = await BeginAuthorizedAsync(
                connection,
                scenario.FirstActorId,
                scenario.FirstOrganizationId,
                scenario.FirstSessionId,
                scenario.FirstAuthorizationVersion);
            for (int index = 0; index < 101; index++)
            {
                await InsertManagementUnitAsync(
                    connection,
                    transaction,
                    Guid.NewGuid(),
                    scenario.FirstOrganizationId,
                    string.Concat(
                        "Overflow field ",
                        index.ToString("D3", CultureInfo.InvariantCulture)));
            }

            await transaction.CommitAsync();
        }

        var factory = new PostgresProductiveCoreUnitOfWorkFactory(
            new TestProductiveDbContextFactory(scenario.RuntimeConnectionString));
        await using IProductiveCoreUnitOfWork unitOfWork = await factory.BeginAsync(
            ProductiveTransactionMode.Read,
            CancellationToken.None);
        var requestContext = new ProductiveRequestContext(
            "overflow-sentinel",
            scenario.FirstActorId,
            scenario.FirstSessionId,
            scenario.FirstOrganizationId);
        Assert.AreEqual(
            scenario.FirstAuthorizationVersion,
            await unitOfWork.AuthorizeOwnerAsync(requestContext, CancellationToken.None));
        IReadOnlyList<ManagementUnit> rows = await unitOfWork.ListManagementUnitsAsync(
            scenario.FirstOrganizationId,
            CancellationToken.None);
        Assert.AreEqual(101, rows.Count);
        await unitOfWork.RollbackAsync(CancellationToken.None);

        ProductiveCoreApplicationService service = CreateApplicationService(factory);
        ProductiveCoreOperationException unavailable =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                service.ListFieldsAsync(
                    scenario.FirstOrganizationId,
                    requestContext,
                    CancellationToken.None));
        Assert.AreEqual("productive_core.management_unit_unavailable", unavailable.Code);
        Assert.AreEqual(503, unavailable.StatusCode);
    }

    [TestMethod]
    public async Task ConcurrentCreatesAtCapacityCommitExactlyOneCompleteSlice()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        await using (var connection = new NpgsqlConnection(scenario.RuntimeConnectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlTransaction transaction = await BeginAuthorizedAsync(
                connection,
                scenario.FirstActorId,
                scenario.FirstOrganizationId,
                scenario.FirstSessionId,
                scenario.FirstAuthorizationVersion);
            for (int index = 0; index < 99; index++)
            {
                await InsertManagementUnitAsync(
                    connection,
                    transaction,
                    Guid.NewGuid(),
                    scenario.FirstOrganizationId,
                    string.Concat(
                        "Capacity field ",
                        index.ToString("D3", CultureInfo.InvariantCulture)));
            }

            await transaction.CommitAsync();
        }

        ProductiveCoreApplicationService service = CreateApplicationService(
            new PostgresProductiveCoreUnitOfWorkFactory(
                new TestProductiveDbContextFactory(scenario.RuntimeConnectionString)));
        var requestContext = new ProductiveRequestContext(
            "capacity-boundary",
            scenario.FirstActorId,
            scenario.FirstSessionId,
            scenario.FirstOrganizationId);
        Task<CreateAttempt> first = AttemptCreateAsync(
            service,
            new CreateFieldCommand(
                scenario.FirstOrganizationId,
                "Boundary field one",
                new string('a', 32)),
            requestContext);
        Task<CreateAttempt> second = AttemptCreateAsync(
            service,
            new CreateFieldCommand(
                scenario.FirstOrganizationId,
                "Boundary field two",
                new string('b', 32)),
            requestContext);
        CreateAttempt[] attempts = await Task.WhenAll(first, second);

        Assert.AreEqual(1, attempts.Count(attempt => attempt.Created));
        Assert.AreEqual(
            1,
            attempts.Count(attempt =>
                attempt.ErrorCode == "productive_core.management_unit_capacity_reached"));
        IReadOnlyList<ManagementUnitResult> fields = await service.ListFieldsAsync(
            scenario.FirstOrganizationId,
            requestContext,
            CancellationToken.None);
        Assert.AreEqual(100, fields.Count);

        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.AreEqual(
            100L,
            await ScalarInt64Async(
                admin,
                null,
                "SELECT count(*) FROM productive_core.management_units"));
        foreach (string table in new[]
                 {
                     "management_unit_creation_ledgers",
                     "management_unit_creation_key_aliases",
                     "journal_entries",
                     "outbox_messages",
                 })
        {
            Assert.AreEqual(
                1L,
                await ScalarInt64Async(
                    admin,
                    null,
                    $"SELECT count(*) FROM productive_core.{table}"));
        }
    }

    [TestMethod]
    public async Task CancelledProductiveQueryRollsBackAndPoolReuseHasNoTenantContext()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        var poolBuilder = new NpgsqlConnectionStringBuilder(scenario.RuntimeConnectionString)
        {
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 1,
            ApplicationName = $"productive-cancel-{Guid.NewGuid():N}",
        };

        await using (var cancelledConnection = new NpgsqlConnection(poolBuilder.ConnectionString))
        {
            await cancelledConnection.OpenAsync();
            await using NpgsqlTransaction cancelledTransaction = await BeginAuthorizedAsync(
                cancelledConnection,
                scenario.FirstActorId,
                scenario.FirstOrganizationId,
                scenario.FirstSessionId,
                scenario.FirstAuthorizationVersion);
            await InsertManagementUnitAsync(
                cancelledConnection,
                cancelledTransaction,
                Guid.NewGuid(),
                scenario.FirstOrganizationId,
                "Cancelled field");
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await using var command = new NpgsqlCommand(
                "SELECT pg_sleep(10)",
                cancelledConnection,
                cancelledTransaction);
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await command.ExecuteNonQueryAsync(cancellation.Token));
            await cancelledTransaction.RollbackAsync(CancellationToken.None);
        }

        await using var reusedConnection = new NpgsqlConnection(poolBuilder.ConnectionString);
        await reusedConnection.OpenAsync();
        await using NpgsqlTransaction noContext = await reusedConnection.BeginTransactionAsync();
        await SetRoleAsync(reusedConnection, noContext, "agro_productive_app");
        Assert.AreEqual(
            0L,
            await ScalarInt64Async(
                reusedConnection,
                noContext,
                "SELECT count(*) FROM productive_core.management_units"));
        await noContext.RollbackAsync();
    }

    private static ProductiveCoreApplicationService CreateApplicationService(
        IProductiveCoreUnitOfWorkFactory factory)
    {
        ServiceProvider metrics = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        return new ProductiveCoreApplicationService(
            factory,
            new ProductiveCoreTelemetry(
                metrics.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()),
            TimeProvider.System,
            Options.Create(new ManagementUnitCreationOptions
            {
                Enabled = true,
                CurrentKeyVersion = "v1",
                HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["v1"] = Convert.ToBase64String(
                        Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
                },
            }));
    }

    private static async Task<CreateAttempt> AttemptCreateAsync(
        ProductiveCoreApplicationService service,
        CreateFieldCommand command,
        ProductiveRequestContext requestContext)
    {
        try
        {
            await service.CreateFieldAsync(command, requestContext, CancellationToken.None);
            return new CreateAttempt(true, null);
        }
        catch (ProductiveCoreOperationException exception)
        {
            return new CreateAttempt(false, exception.Code);
        }
    }

    private sealed record CreateAttempt(bool Created, string? ErrorCode);

    private static IdentityDbContext CreateIdentityDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static ProductiveCoreDbContext CreateProductiveDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<ProductiveCoreDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    "productive_core"))
            .Options);

    private sealed class TestProductiveDbContextFactory(string connectionString)
        : IDbContextFactory<ProductiveCoreDbContext>
    {
        public ProductiveCoreDbContext CreateDbContext() =>
            CreateProductiveDbContext(connectionString);

        public Task<ProductiveCoreDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }

    private static PostgreSqlTestServer RequirePostgreSql() =>
        IdentityTestAssembly.PostgreSql
        ?? throw new AssertFailedException(
            "PostgreSQL integration fixture could not start: "
            + IdentityTestAssembly.StartupError?.Message);

    private static async Task<NpgsqlTransaction> BeginAuthorizedAsync(
        NpgsqlConnection connection,
        Guid actorId,
        Guid organizationId,
        Guid sessionId,
        Guid expectedAuthorizationVersion)
    {
        NpgsqlTransaction transaction = await connection.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        await SetRoleAndBaseContextAsync(
            connection,
            transaction,
            "agro_productive_app",
            actorId,
            organizationId,
            sessionId);
        Guid? authorizationVersion = await ExecuteAuthorizationAsync(connection, transaction);
        Assert.AreEqual(expectedAuthorizationVersion, authorizationVersion);
        await using var context = new NpgsqlCommand(
            "SELECT set_config('app.current_authorization_version', @version, true)",
            connection,
            transaction);
        context.Parameters.AddWithValue("version", authorizationVersion!.Value.ToString("D"));
        await context.ExecuteNonQueryAsync();
        return transaction;
    }

    private static async Task SetRoleAndBaseContextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        Guid actorId,
        Guid organizationId,
        Guid sessionId)
    {
        await SetRoleAsync(connection, transaction, role);
        await using var command = new NpgsqlCommand(
            """
            SELECT set_config('app.current_scope_kind', 'tenant', true),
                   set_config('app.current_actor_id', @actor, true),
                   set_config('app.current_organization_id', @organization, true),
                   set_config('app.current_session_id', @session, true)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("actor", actorId.ToString("D"));
        command.Parameters.AddWithValue("organization", organizationId.ToString("D"));
        command.Parameters.AddWithValue("session", sessionId.ToString("D"));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role)
    {
        if (role is not ("agro_productive_app" or "agro_productive_job"))
        {
            throw new ArgumentException("Unexpected Productive Core role.", nameof(role));
        }

        await using var command = new NpgsqlCommand($"SET LOCAL ROLE {role}", connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<Guid?> ExecuteAuthorizationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand(
            "SELECT identity.authorize_productive_owner()",
            connection,
            transaction);
        object? value = await command.ExecuteScalarAsync();
        return value is Guid version ? version : null;
    }

    private static async Task InsertManagementUnitAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid id,
        Guid organizationId,
        string displayName)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO productive_core.management_units
                ("Id", "OrganizationId", "DisplayName", "UnitType", "Status",
                 "SpatialStatus", "CreatedAtUtc", "Version")
            VALUES (@id, @organization, @displayName, 'field', 'draft',
                    'not_configured', now(), gen_random_uuid())
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("organization", organizationId);
        command.Parameters.AddWithValue("displayName", displayName);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> KeyCoverageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string version)
    {
        await using var command = new NpgsqlCommand(
            "SELECT productive_core.management_unit_creation_retained_key_covered(@versions)",
            connection,
            transaction);
        command.Parameters.AddWithValue("versions", NpgsqlDbType.Array | NpgsqlDbType.Text, new[] { version });
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Key coverage returned null."));
    }

    private static async Task ExecuteAsync(
        string connectionString,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteOnConnectionAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> ScalarBooleanAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Boolean database probe returned null."));
    }

    private static async Task<long> ScalarInt64Async(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Count database probe returned null."));
    }

    private sealed class DatabaseScenario(
        string connectionString,
        string runtimeConnectionString,
        string runtimeRole,
        Guid firstActorId,
        Guid secondActorId,
        Guid firstOrganizationId,
        Guid secondOrganizationId,
        Guid firstSessionId,
        Guid secondSessionId,
        Guid firstAuthorizationVersion,
        Guid secondAuthorizationVersion) : IAsyncDisposable
    {
        public string ConnectionString { get; } = connectionString;
        public string RuntimeConnectionString { get; } = runtimeConnectionString;
        public string RuntimeRole { get; } = runtimeRole;
        public Guid FirstActorId { get; } = firstActorId;
        public Guid SecondActorId { get; } = secondActorId;
        public Guid FirstOrganizationId { get; } = firstOrganizationId;
        public Guid SecondOrganizationId { get; } = secondOrganizationId;
        public Guid FirstSessionId { get; } = firstSessionId;
        public Guid SecondSessionId { get; } = secondSessionId;
        public Guid FirstAuthorizationVersion { get; } = firstAuthorizationVersion;
        public Guid SecondAuthorizationVersion { get; } = secondAuthorizationVersion;

        public static async Task<DatabaseScenario> CreateAsync()
        {
            PostgreSqlTestServer postgresql = RequirePostgreSql();
            string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
            try
            {
                await using (IdentityDbContext identity = CreateIdentityDbContext(connectionString))
                {
                    await identity.Database.MigrateAsync();
                }

                await using (ProductiveCoreDbContext productive = CreateProductiveDbContext(connectionString))
                {
                    await productive.Database.MigrateAsync();
                }

                string runtimeRole = $"agro_productive_test_{Guid.NewGuid():N}";
                string runtimePassword = $"productive-{Guid.NewGuid():N}";
                await using (var admin = new NpgsqlConnection(connectionString))
                {
                    await admin.OpenAsync();
                    await using var createPrincipal = new NpgsqlCommand(
                        $"""
                        CREATE ROLE {runtimeRole}
                            LOGIN PASSWORD '{runtimePassword}' NOINHERIT NOSUPERUSER
                            NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
                        GRANT agro_productive_app TO {runtimeRole}
                            WITH INHERIT FALSE, SET TRUE;
                        """,
                        admin);
                    await createPrincipal.ExecuteNonQueryAsync();
                }

                var runtimeBuilder = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Username = runtimeRole,
                    Password = runtimePassword,
                    Pooling = false,
                };
                var scenario = new DatabaseScenario(
                    connectionString,
                    runtimeBuilder.ConnectionString,
                    runtimeRole,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid());
                await scenario.SeedIdentityAsync();
                return scenario;
            }
            catch
            {
                await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using (var admin = new NpgsqlConnection(ConnectionString))
            {
                await admin.OpenAsync();
                await using var dropPrincipal = new NpgsqlCommand(
                    $"""
                    REVOKE agro_productive_app FROM {RuntimeRole};
                    DROP ROLE IF EXISTS {RuntimeRole};
                    """,
                    admin);
                await dropPrincipal.ExecuteNonQueryAsync();
            }

            await RequirePostgreSql().DropDatabaseAsync(ConnectionString, CancellationToken.None);
        }

        private async Task SeedIdentityAsync()
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
                VALUES (@firstActor, 'First productive owner', now(), 1),
                       (@secondActor, 'Second productive owner', now(), 1);
                INSERT INTO identity.sessions
                    ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                     "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                     "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
                VALUES (@firstSession, @firstActor, decode(repeat('61', 32), 'hex'), now(),
                        now() + interval '1 hour', true, NULL, NULL, NULL, @firstVersion),
                       (@secondSession, @secondActor, decode(repeat('62', 32), 'hex'), now(),
                        now() + interval '1 hour', true, NULL, NULL, NULL, @secondVersion);
                INSERT INTO identity.organizations
                    ("Id", "DisplayName", "Status", "CreatedByUserId", "CreatedAtUtc", "Version")
                VALUES (@firstOrganization, 'First productive organization', 'active',
                        @firstActor, now(), gen_random_uuid()),
                       (@secondOrganization, 'Second productive organization', 'active',
                        @secondActor, now(), gen_random_uuid());
                INSERT INTO identity.memberships
                    ("Id", "OrganizationId", "UserId", "Role", "Status",
                     "SecurityVersion", "CreatedAtUtc", "Version")
                VALUES (gen_random_uuid(), @firstOrganization, @firstActor,
                        'owner', 'active', 1, now(), gen_random_uuid()),
                       (gen_random_uuid(), @secondOrganization, @secondActor,
                        'owner', 'active', 1, now(), gen_random_uuid());
                """,
                connection);
            command.Parameters.AddWithValue("firstActor", FirstActorId);
            command.Parameters.AddWithValue("secondActor", SecondActorId);
            command.Parameters.AddWithValue("firstSession", FirstSessionId);
            command.Parameters.AddWithValue("secondSession", SecondSessionId);
            command.Parameters.AddWithValue("firstVersion", FirstAuthorizationVersion);
            command.Parameters.AddWithValue("secondVersion", SecondAuthorizationVersion);
            command.Parameters.AddWithValue("firstOrganization", FirstOrganizationId);
            command.Parameters.AddWithValue("secondOrganization", SecondOrganizationId);
            Assert.AreEqual(8, await command.ExecuteNonQueryAsync());
        }
    }
}

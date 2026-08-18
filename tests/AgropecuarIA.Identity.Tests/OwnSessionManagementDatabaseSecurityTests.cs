using System.Data;
using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OwnSessionManagementDatabaseSecurityTests
{
    private const string PreviousMigration =
        "20260818180000_AddProductiveCoreAuthorizationPort";
    private const string OwnSessionManagementMigration =
        "20260818213000_AddOwnSessionManagement";
    private static readonly string[] ExpectedConcurrentOutcomes =
        ["revoked", "already_revoked"];

    [TestMethod]
    public async Task MigrationIsAppOnlyLeastPrivilegeAndSupportsEphemeralRollbackForward()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using IdentityDbContext dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            await using (var previous = new NpgsqlConnection(connectionString))
            {
                await previous.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    previous,
                    "SELECT to_regprocedure('identity.count_own_active_sessions()') IS NOT NULL"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    previous,
                    """
                    SELECT pg_get_constraintdef(oid) LIKE '%manage_sessions%'
                    FROM pg_constraint
                    WHERE conname = 'CK_sessions_StrongAuthentication'
                    """));
            }

            await migrator.MigrateAsync();
            await using (var expanded = new NpgsqlConnection(connectionString))
            {
                await expanded.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT has_function_privilege(
                               'agro_identity_app',
                               'identity.count_own_active_sessions()', 'EXECUTE')
                       AND has_function_privilege(
                               'agro_identity_app',
                               'identity.list_own_active_sessions(integer,integer)', 'EXECUTE')
                       AND has_function_privilege(
                               'agro_identity_app',
                               'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)',
                               'EXECUTE')
                       AND NOT has_function_privilege(
                               'public',
                               'identity.count_own_active_sessions()', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_job',
                               'identity.list_own_active_sessions(integer,integer)', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_discovery',
                               'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)',
                               'EXECUTE')
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT bool_and(p.prosecdef)
                       AND bool_and(p.proowner = (SELECT oid FROM pg_roles
                                                  WHERE rolname = 'agro_identity_owner'))
                       AND bool_and(p.proconfig = ARRAY['search_path=pg_catalog'])
                    FROM pg_proc AS p
                    JOIN pg_namespace AS n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'identity'
                      AND p.proname IN (
                          'count_own_active_sessions',
                          'list_own_active_sessions',
                          'revoke_other_own_session')
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT NOT has_table_privilege(
                                   'agro_identity_app', 'identity.users', 'SELECT')
                       AND NOT has_any_column_privilege(
                                   'agro_identity_app', 'identity.users', 'SELECT')
                       AND NOT has_column_privilege(
                                   'agro_identity_app', 'identity.sessions', 'TokenHash', 'SELECT')
                       AND NOT has_table_privilege(
                                   'agro_identity_app', 'identity.sessions', 'UPDATE')
                       AND NOT has_any_column_privilege(
                                   'agro_identity_app', 'identity.sessions', 'UPDATE')
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT (SELECT pg_get_constraintdef(oid) LIKE '%manage_sessions%'
                            FROM pg_constraint
                            WHERE conname = 'CK_sessions_StrongAuthentication')
                       AND (SELECT pg_get_constraintdef(oid) LIKE '%manage_sessions%'
                            FROM pg_constraint
                            WHERE conname = 'CK_step_up_attempts_Purpose')
                    """));
            }

            await migrator.MigrateAsync(PreviousMigration);
            await using (var rolledBack = new NpgsqlConnection(connectionString))
            {
                await rolledBack.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regprocedure('identity.count_own_active_sessions()') IS NOT NULL"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    """
                    SELECT pg_get_constraintdef(oid) LIKE '%manage_sessions%'
                    FROM pg_constraint
                    WHERE conname = 'CK_step_up_attempts_Purpose'
                    """));
            }

            await migrator.MigrateAsync();
            Assert.IsEmpty(await dbContext.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task InventoryIsActorScopedPrivacySafeOrderedAndPoolLocal()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        var poolBuilder = new NpgsqlConnectionStringBuilder(scenario.ConnectionString)
        {
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 1,
            ApplicationName = $"own-session-inventory-{Guid.NewGuid():N}",
        };
        await using (var connection = new NpgsqlConnection(poolBuilder.ConnectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(
                IsolationLevel.RepeatableRead);
            await SetAppContextAsync(connection, transaction, seeded);

            Assert.AreEqual(2L, await ScalarInt64Async(
                connection,
                transaction,
                "SELECT identity.count_own_active_sessions()"));
            await using var list = new NpgsqlCommand(
                """
                SELECT session_id, authenticated_at_utc, expires_at_utc, version, is_current
                FROM identity.list_own_active_sessions(0, 50)
                """,
                connection,
                transaction);
            await using NpgsqlDataReader reader = await list.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual(seeded.CurrentSessionId, reader.GetGuid(0));
            Assert.IsTrue(reader.GetBoolean(4));
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual(seeded.TargetSessionId, reader.GetGuid(0));
            Assert.IsFalse(reader.GetBoolean(4));
            Assert.IsFalse(await reader.ReadAsync());
            await reader.CloseAsync();

            await AssertPermissionDeniedAsync(
                connection,
                transaction,
                "SELECT \"TokenHash\" FROM identity.sessions LIMIT 1");
            await transaction.RollbackAsync();
        }

        await using (var reused = new NpgsqlConnection(poolBuilder.ConnectionString))
        {
            await reused.OpenAsync();
            await using NpgsqlTransaction transaction = await reused.BeginTransactionAsync();
            await SetAppRoleAsync(reused, transaction);
            Assert.AreEqual(0L, await ScalarInt64Async(
                reused,
                transaction,
                "SELECT identity.count_own_active_sessions()"));
            await transaction.RollbackAsync();
        }

        NpgsqlConnection.ClearPool(new NpgsqlConnection(poolBuilder.ConnectionString));
    }

    [TestMethod]
    public async Task RevokeIsPurposeBoundActorScopedCasAndRollbackSafe()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        Assert.AreEqual(
            "strong_authentication_required",
            await RevokeAsync(
                scenario.ConnectionString,
                seeded,
                seeded.TargetSessionId,
                seeded.TargetVersion,
                DateTimeOffset.UtcNow.AddSeconds(-30),
                Guid.NewGuid(),
                commit: false));
        Assert.AreEqual(
            "current_session",
            await RevokeAsync(
                scenario.ConnectionString,
                seeded,
                seeded.CurrentSessionId,
                seeded.AuthorizationVersion,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                Guid.NewGuid(),
                commit: false));
        Assert.AreEqual(
            "not_available",
            await RevokeAsync(
                scenario.ConnectionString,
                seeded,
                seeded.ForeignSessionId,
                seeded.ForeignVersion,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                Guid.NewGuid(),
                commit: false));
        Assert.AreEqual(
            "version_mismatch",
            await RevokeAsync(
                scenario.ConnectionString,
                seeded,
                seeded.TargetSessionId,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddMinutes(-5),
                Guid.NewGuid(),
                commit: false));

        Guid rolledBackVersion = Guid.NewGuid();
        Assert.AreEqual(
            "revoked",
            await RevokeAsync(
                scenario.ConnectionString,
                seeded,
                seeded.TargetSessionId,
                seeded.TargetVersion,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                rolledBackVersion,
                commit: false));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));

        await using var denied = new NpgsqlConnection(scenario.ConnectionString);
        await denied.OpenAsync();
        await using NpgsqlTransaction deniedTransaction = await denied.BeginTransactionAsync();
        await SetAppContextAsync(denied, deniedTransaction, seeded);
        await AssertPermissionDeniedAsync(
            denied,
            deniedTransaction,
            "UPDATE identity.sessions SET \"RevokedAtUtc\" = now() WHERE \"Id\" = @target",
            ("target", seeded.TargetSessionId));
        await deniedTransaction.RollbackAsync();
    }

    [TestMethod]
    public async Task ConcurrentReplayProducesOneTransitionAndOneJournal()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        Task<string> first = RevokeAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            Guid.NewGuid(),
            Guid.NewGuid());
        Task<string> second = RevokeAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            Guid.NewGuid(),
            Guid.NewGuid());
        string[] outcomes = await Task.WhenAll(first, second);

        CollectionAssert.AreEquivalent(
            ExpectedConcurrentOutcomes,
            outcomes);
        await using var verify = new NpgsqlConnection(scenario.ConnectionString);
        await verify.OpenAsync();
        Assert.AreEqual(1L, await ScalarInt64Async(
            verify,
            null,
            """
            SELECT count(*) FROM identity.audit_events
            WHERE "SessionId" = @target AND "Action" = 'session_revoked'
            """,
            ("target", seeded.TargetSessionId)));
    }

    [TestMethod]
    public async Task CancelledBlockedRevokeLeavesSessionActive()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        await using var blocker = new NpgsqlConnection(scenario.ConnectionString);
        await blocker.OpenAsync();
        await using NpgsqlTransaction blockerTransaction = await blocker.BeginTransactionAsync();
        await using (var lockTable = new NpgsqlCommand(
            "LOCK TABLE identity.sessions IN ACCESS EXCLUSIVE MODE",
            blocker,
            blockerTransaction))
        {
            await lockTable.ExecuteNonQueryAsync();
        }

        await using var cancelled = new NpgsqlConnection(scenario.ConnectionString);
        await cancelled.OpenAsync();
        await using NpgsqlTransaction cancelledTransaction = await cancelled.BeginTransactionAsync();
        await SetAppContextAsync(cancelled, cancelledTransaction, seeded);
        await using var revoke = CreateRevokeCommand(
            cancelled,
            cancelledTransaction,
            seeded.TargetSessionId,
            seeded.TargetVersion,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            Guid.NewGuid());
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        try
        {
            await revoke.ExecuteScalarAsync(cancellation.Token);
            Assert.Fail("The blocked revocation unexpectedly ignored cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
        await cancelledTransaction.RollbackAsync();
        await blockerTransaction.RollbackAsync();

        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));
    }

    [TestMethod]
    public async Task RevokeAllMigrationIsExecuteOnlyAndNMinusOneCompatible()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using IdentityDbContext dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(OwnSessionManagementMigration);

            string nMinusOneIndividualDefinition;
            await using (var previous = new NpgsqlConnection(connectionString))
            {
                await previous.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    previous,
                    "SELECT to_regprocedure('identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)') IS NOT NULL"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    previous,
                    "SELECT to_regprocedure('identity.revoke_all_other_own_sessions(timestamptz)') IS NOT NULL"));
                nMinusOneIndividualDefinition = await ScalarStringAsync(
                    previous,
                    """
                    SELECT pg_get_functiondef(
                        'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)'
                            ::regprocedure)
                    """);
            }

            await migrator.MigrateAsync();
            await using (var expanded = new NpgsqlConnection(connectionString))
            {
                await expanded.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT has_function_privilege(
                               'agro_identity_app',
                               'identity.revoke_all_other_own_sessions(timestamptz)',
                               'EXECUTE')
                       AND NOT has_function_privilege(
                               'public',
                               'identity.revoke_all_other_own_sessions(timestamptz)',
                               'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_job',
                               'identity.revoke_all_other_own_sessions(timestamptz)',
                               'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_discovery',
                               'identity.revoke_all_other_own_sessions(timestamptz)',
                               'EXECUTE')
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT p.prosecdef
                       AND p.provolatile = 'v'
                       AND p.proowner = (SELECT oid FROM pg_roles
                                         WHERE rolname = 'agro_identity_owner')
                       AND p.proconfig = ARRAY['search_path=pg_catalog']
                    FROM pg_proc AS p
                    JOIN pg_namespace AS n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'identity'
                      AND p.proname = 'revoke_all_other_own_sessions'
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT pg_get_functiondef(
                        'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)'
                            ::regprocedure)
                        LIKE '%pg_advisory_xact_lock(hashtextextended(actor_id::text, 0))%'
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT NOT has_table_privilege(
                                   'agro_identity_app', 'identity.users', 'SELECT')
                       AND NOT has_any_column_privilege(
                                   'agro_identity_app', 'identity.users', 'SELECT')
                       AND NOT has_column_privilege(
                                   'agro_identity_app', 'identity.sessions', 'TokenHash', 'SELECT')
                       AND NOT has_table_privilege(
                                   'agro_identity_app', 'identity.sessions', 'UPDATE')
                       AND NOT has_any_column_privilege(
                                   'agro_identity_app', 'identity.sessions', 'UPDATE')
                    """));
            }

            await migrator.MigrateAsync(OwnSessionManagementMigration);
            await using (var rolledBack = new NpgsqlConnection(connectionString))
            {
                await rolledBack.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regprocedure('identity.revoke_all_other_own_sessions(timestamptz)') IS NOT NULL"));
                Assert.IsTrue(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regprocedure('identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)') IS NOT NULL"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    """
                    SELECT pg_get_functiondef(
                        'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)'
                            ::regprocedure)
                        LIKE '%pg_advisory_xact_lock(hashtextextended(actor_id::text, 0))%'
                    """));
                Assert.AreEqual(
                    nMinusOneIndividualDefinition,
                    await ScalarStringAsync(
                        rolledBack,
                        """
                        SELECT pg_get_functiondef(
                            'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)'
                                ::regprocedure)
                        """));
            }

            await migrator.MigrateAsync();
            Assert.IsEmpty(await dbContext.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task RevokeAllHandlesZeroOneAndManyWithoutTouchingCurrentOrForeignRows()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        BulkResult[] one = await RevokeAllAndJournalAsync(
            scenario.ConnectionString,
            seeded);
        Assert.HasCount(1, one);
        Assert.AreEqual("revoked", one[0].Outcome);
        Assert.AreEqual(seeded.TargetSessionId, one[0].SessionId);

        SessionVersion second = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x66);
        SessionVersion third = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x77);

        BulkResult[] changed = await RevokeAllAndJournalAsync(
            scenario.ConnectionString,
            seeded);

        Assert.HasCount(2, changed);
        Assert.IsTrue(changed.All(item => item.Outcome == "revoked"));
        Assert.HasCount(1, changed.Select(item => item.RevokedAtUtc).Distinct().ToArray());
        CollectionAssert.AreEquivalent(
            new[] { second.SessionId, third.SessionId },
            changed.Select(item => item.SessionId!.Value).ToArray());
        Assert.IsTrue(changed.All(item => item.Version is Guid value && value != Guid.Empty));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.ForeignSessionId,
            seeded.ForeignVersion));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.ExpiredSessionId,
            seeded.ExpiredVersion));
        Assert.IsFalse(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.RevokedSessionId,
            seeded.RevokedVersion));
        Assert.AreEqual(3L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));

        BulkResult[] replay = await RevokeAllAndJournalAsync(
            scenario.ConnectionString,
            seeded);
        Assert.HasCount(1, replay);
        Assert.AreEqual("no_sessions", replay[0].Outcome);
        Assert.IsNull(replay[0].SessionId);
        Assert.AreEqual(3L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    [TestMethod]
    public async Task RevokeAllRejectsWrongPurposeAndStaleActorBeforeMutation()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        await ExecuteAsync(
            scenario.ConnectionString,
            """
            UPDATE identity.sessions
            SET "StrongAuthenticationPurpose" = 'manage_authentication_methods'
            WHERE "Id" = @session
            """,
            ("session", seeded.CurrentSessionId));
        BulkResult[] wrongPurpose = await RevokeAllAsync(
            scenario.ConnectionString,
            seeded,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.HasCount(1, wrongPurpose);
        Assert.AreEqual("strong_authentication_required", wrongPurpose[0].Outcome);

        await ExecuteAsync(
            scenario.ConnectionString,
            """
            UPDATE identity.sessions
            SET "StrongAuthenticationPurpose" = 'manage_sessions'
            WHERE "Id" = @session
            """,
            ("session", seeded.CurrentSessionId));
        BulkResult[] stale = await RevokeAllAsync(
            scenario.ConnectionString,
            seeded,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            authorizationVersion: Guid.NewGuid());
        Assert.HasCount(1, stale);
        Assert.AreEqual("not_available", stale[0].Outcome);
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));
    }

    [TestMethod]
    public async Task ConcurrentBulkAndIndividualRevokesJournalEachTargetOnce()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        SessionVersion second = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x68);

        Task<BulkResult[]> bulk = RevokeAllAndJournalAsync(
            scenario.ConnectionString,
            seeded);
        Task<string> individual = RevokeAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            Guid.NewGuid(),
            Guid.NewGuid());
        await Task.WhenAll(bulk, individual);

        Assert.IsTrue(individual.Result is "revoked" or "already_revoked");
        Assert.IsTrue(bulk.Result.All(item => item.Outcome == "revoked"));
        Assert.AreEqual(2L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
        await using (var verify = new NpgsqlConnection(scenario.ConnectionString))
        {
            await verify.OpenAsync();
            Assert.AreEqual(2L, await ScalarInt64Async(
                verify,
                null,
                """
                SELECT count(*)
                FROM (
                    SELECT "SessionId"
                    FROM identity.audit_events
                    WHERE "UserId" = @actor AND "Action" = 'session_revoked'
                    GROUP BY "SessionId"
                    HAVING count(*) = 1
                ) AS journaled_targets
                """,
                ("actor", seeded.ActorId)));
        }
        Assert.IsFalse(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));
        Assert.IsFalse(await IsSessionActiveAsync(
            scenario.ConnectionString,
            second.SessionId,
            second.Version));
    }

    [TestMethod]
    public async Task CrossCurrentIndividualAndBulkLeaveOneCurrentSessionValid()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        await ExecuteAsync(
            scenario.ConnectionString,
            """
            UPDATE identity.sessions
            SET "StrongAuthenticatedAtUtc" = now() - interval '1 minute',
                "StrongAuthenticationPurpose" = 'manage_sessions'
            WHERE "Id" = @session
            """,
            ("session", seeded.TargetSessionId));

        await using var bulkConnection = new NpgsqlConnection(scenario.ConnectionString);
        await bulkConnection.OpenAsync();
        await using NpgsqlTransaction bulkTransaction =
            await bulkConnection.BeginTransactionAsync();
        await SetAppContextAsync(
            bulkConnection,
            bulkTransaction,
            seeded,
            seeded.TargetVersion,
            seeded.TargetSessionId);
        int bulkBackendPid = await ScalarInt32Async(
            bulkConnection,
            bulkTransaction,
            "SELECT pg_backend_pid()");
        await using (var holdActorLock = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(CAST(@actor AS text), 0))",
            bulkConnection,
            bulkTransaction))
        {
            holdActorLock.Parameters.AddWithValue("actor", seeded.ActorId.ToString("D"));
            await holdActorLock.ExecuteNonQueryAsync();
        }

        Task<string> individualFromAToB = RevokeAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            Guid.NewGuid(),
            Guid.NewGuid());
        await WaitForAdvisoryWaiterAsync(
            scenario.ConnectionString,
            bulkBackendPid);

        BulkResult[] bulkFromBToA = await ExecuteRevokeAllAsync(
            bulkConnection,
            bulkTransaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        foreach (BulkResult revoked in bulkFromBToA)
        {
            Assert.AreEqual("revoked", revoked.Outcome);
            await InsertSessionRevokedJournalAsync(
                bulkConnection,
                bulkTransaction,
                seeded.ActorId,
                revoked.SessionId!.Value,
                Guid.NewGuid());
        }
        await bulkTransaction.CommitAsync();
        string individualOutcome = await individualFromAToB;

        bool sessionAActive = await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion);
        bool sessionBActive = await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion);
        Assert.IsFalse(sessionAActive);
        Assert.IsTrue(sessionBActive);
        Assert.AreEqual(1L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
        Assert.AreEqual("not_available", individualOutcome);
        Assert.HasCount(1, bulkFromBToA);
        Assert.AreEqual(seeded.CurrentSessionId, bulkFromBToA[0].SessionId);
    }

    [TestMethod]
    public async Task ConcurrentBulkCommandsProduceOneBatchAndOneJournalPerTarget()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        _ = await AddActiveSessionAsync(scenario.ConnectionString, seeded.ActorId, 0x69);

        Task<BulkResult[]> first = RevokeAllAndJournalAsync(
            scenario.ConnectionString,
            seeded);
        Task<BulkResult[]> second = RevokeAllAndJournalAsync(
            scenario.ConnectionString,
            seeded);
        BulkResult[][] results = await Task.WhenAll(first, second);

        Assert.AreEqual(1, results.Count(result =>
            result.Length == 1 && result[0].Outcome == "no_sessions"));
        Assert.AreEqual(1, results.Count(result =>
            result.Length == 2 && result.All(item => item.Outcome == "revoked")));
        Assert.AreEqual(2L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    [TestMethod]
    public async Task JournalFailureAndCancellationRollbackTheWholeBulkAndPoolDoesNotLeakContext()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        SessionVersion second = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x6A);

        await AssertJournalFailureRollsBackAsync(scenario.ConnectionString, seeded);
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            second.SessionId,
            second.Version));
        Assert.AreEqual(0L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));

        await AssertCancelledBulkRollsBackAsync(scenario.ConnectionString, seeded);
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));

        var poolBuilder = new NpgsqlConnectionStringBuilder(scenario.ConnectionString)
        {
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 1,
            ApplicationName = $"revoke-all-sessions-{Guid.NewGuid():N}",
        };
        await using (var primed = new NpgsqlConnection(poolBuilder.ConnectionString))
        {
            await primed.OpenAsync();
            await using NpgsqlTransaction transaction = await primed.BeginTransactionAsync();
            await SetAppContextAsync(primed, transaction, seeded);
            await transaction.RollbackAsync();
        }
        await using (var reused = new NpgsqlConnection(poolBuilder.ConnectionString))
        {
            await reused.OpenAsync();
            await using NpgsqlTransaction transaction = await reused.BeginTransactionAsync();
            await SetAppRoleAsync(reused, transaction);
            BulkResult[] result = await ExecuteRevokeAllAsync(
                reused,
                transaction,
                DateTimeOffset.UtcNow.AddMinutes(-5));
            Assert.HasCount(1, result);
            Assert.AreEqual("not_available", result[0].Outcome);
            await transaction.RollbackAsync();
        }
        NpgsqlConnection.ClearPool(new NpgsqlConnection(poolBuilder.ConnectionString));
    }

    private static async Task<BulkResult[]> RevokeAllAsync(
        string connectionString,
        SeededSessions seeded,
        DateTimeOffset strongNotBefore,
        Guid? authorizationVersion = null)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(
            connection,
            transaction,
            seeded,
            authorizationVersion);
        BulkResult[] result = await ExecuteRevokeAllAsync(
            connection,
            transaction,
            strongNotBefore);
        await transaction.RollbackAsync();
        return result;
    }

    private static async Task<BulkResult[]> RevokeAllAndJournalAsync(
        string connectionString,
        SeededSessions seeded)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(connection, transaction, seeded);
        BulkResult[] result = await ExecuteRevokeAllAsync(
            connection,
            transaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        foreach (BulkResult revoked in result.Where(item => item.Outcome == "revoked"))
        {
            await InsertSessionRevokedJournalAsync(
                connection,
                transaction,
                seeded.ActorId,
                revoked.SessionId ?? throw new InvalidOperationException(
                    "A revoked row omitted its session identifier."),
                Guid.NewGuid());
        }
        await transaction.CommitAsync();
        return result;
    }

    private static async Task<BulkResult[]> ExecuteRevokeAllAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset strongNotBefore,
        CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT outcome, session_id, revoked_at_utc, version
            FROM identity.revoke_all_other_own_sessions(@strongNotBefore)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("strongNotBefore", strongNotBefore);
        var results = new List<BulkResult>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new BulkResult(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetGuid(1),
                reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3)));
        }
        return results.ToArray();
    }

    private static async Task<SessionVersion> AddActiveSessionAsync(
        string connectionString,
        Guid actorId,
        byte tokenByte)
    {
        var session = new SessionVersion(Guid.NewGuid(), Guid.NewGuid());
        await ExecuteAsync(
            connectionString,
            """
            INSERT INTO identity.sessions
                ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                 "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                 "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
            VALUES
                (@session, @actor, @tokenHash, now() - interval '15 minutes',
                 now() + interval '1 hour', true, NULL, NULL, NULL, @version)
            """,
            ("session", session.SessionId),
            ("actor", actorId),
            ("tokenHash", Digest(tokenByte)),
            ("version", session.Version));
        return session;
    }

    private static async Task InsertSessionRevokedJournalAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actorId,
        Guid targetSessionId,
        Guid journalId)
    {
        await using var journal = new NpgsqlCommand(
            """
            INSERT INTO identity.audit_events
                ("Id", "UserId", "SessionId", "Action", "Outcome",
                 "Connection", "CorrelationId", "OccurredAtUtc")
            VALUES (@id, @actor, @target, 'session_revoked', 'succeeded',
                    NULL, @correlation, now())
            """,
            connection,
            transaction);
        journal.Parameters.AddWithValue("id", journalId);
        journal.Parameters.AddWithValue("actor", actorId);
        journal.Parameters.AddWithValue("target", targetSessionId);
        journal.Parameters.AddWithValue("correlation", $"test-{journalId:N}");
        Assert.AreEqual(1, await journal.ExecuteNonQueryAsync());
    }

    private static async Task<long> CountSessionRevokedJournalsAsync(
        string connectionString,
        Guid actorId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return await ScalarInt64Async(
            connection,
            null,
            """
            SELECT count(*) FROM identity.audit_events
            WHERE "UserId" = @actor AND "Action" = 'session_revoked'
            """,
            ("actor", actorId));
    }

    private static async Task AssertJournalFailureRollsBackAsync(
        string connectionString,
        SeededSessions seeded)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(connection, transaction, seeded);
        BulkResult[] result = await ExecuteRevokeAllAsync(
            connection,
            transaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.HasCount(2, result);
        Guid duplicateJournalId = Guid.NewGuid();
        await InsertSessionRevokedJournalAsync(
            connection,
            transaction,
            seeded.ActorId,
            result[0].SessionId!.Value,
            duplicateJournalId);
        try
        {
            await InsertSessionRevokedJournalAsync(
                connection,
                transaction,
                seeded.ActorId,
                result[1].SessionId!.Value,
                duplicateJournalId);
            Assert.Fail("A duplicate journal identifier unexpectedly committed.");
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UniqueViolation)
        {
        }
        await transaction.RollbackAsync();
    }

    private static async Task AssertCancelledBulkRollsBackAsync(
        string connectionString,
        SeededSessions seeded)
    {
        await using var blocker = new NpgsqlConnection(connectionString);
        await blocker.OpenAsync();
        await using NpgsqlTransaction blockerTransaction = await blocker.BeginTransactionAsync();
        await using (var lockTable = new NpgsqlCommand(
            "LOCK TABLE identity.sessions IN ACCESS EXCLUSIVE MODE",
            blocker,
            blockerTransaction))
        {
            await lockTable.ExecuteNonQueryAsync();
        }

        await using var cancelled = new NpgsqlConnection(connectionString);
        await cancelled.OpenAsync();
        await using NpgsqlTransaction cancelledTransaction = await cancelled.BeginTransactionAsync();
        await SetAppContextAsync(cancelled, cancelledTransaction, seeded);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        try
        {
            _ = await ExecuteRevokeAllAsync(
                cancelled,
                cancelledTransaction,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                cancellation.Token);
            Assert.Fail("The blocked bulk revocation unexpectedly ignored cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
        await cancelledTransaction.RollbackAsync();
        await blockerTransaction.RollbackAsync();
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

    private static async Task WaitForAdvisoryWaiterAsync(
        string connectionString,
        int holderBackendPid)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await ScalarBooleanAsync(
                connection,
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_locks AS holder
                    JOIN pg_locks AS waiter
                      ON waiter.locktype = holder.locktype
                     AND waiter.database IS NOT DISTINCT FROM holder.database
                     AND waiter.classid IS NOT DISTINCT FROM holder.classid
                     AND waiter.objid IS NOT DISTINCT FROM holder.objid
                     AND waiter.objsubid IS NOT DISTINCT FROM holder.objsubid
                    WHERE holder.pid = @holderPid
                      AND holder.locktype = 'advisory'
                      AND holder.granted
                      AND waiter.pid <> holder.pid
                      AND NOT waiter.granted)
                """,
                ("holderPid", holderBackendPid)))
            {
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }
        Assert.Fail("The individual revocation never waited on the actor advisory lock.");
    }

    private static IdentityDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static PostgreSqlTestServer RequirePostgreSql() =>
        IdentityTestAssembly.PostgreSql
        ?? throw new AssertFailedException(
            "PostgreSQL integration fixture could not start: "
            + IdentityTestAssembly.StartupError?.Message);

    private static async Task<SeededSessions> SeedAsync(string connectionString)
    {
        var seeded = new SeededSessions(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
            VALUES (@actor, 'Session actor', now() - interval '1 day', 1),
                   (@foreignActor, 'Foreign actor', now() - interval '1 day', 1);
            INSERT INTO identity.sessions
                ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                 "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                 "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
            VALUES
                (@currentSession, @actor, @currentHash, now() - interval '1 minute',
                 now() + interval '1 hour', true, now() - interval '1 minute',
                 'manage_sessions', NULL, @authorizationVersion),
                (@targetSession, @actor, @targetHash, now() - interval '10 minutes',
                 now() + interval '1 hour', true, NULL, NULL, NULL, @targetVersion),
                (@expiredSession, @actor, @expiredHash, now() - interval '1 day',
                 now() - interval '1 hour', true, NULL, NULL, NULL, @expiredVersion),
                (@revokedSession, @actor, @revokedHash, now() - interval '1 day',
                 now() + interval '1 hour', true, NULL, NULL, now(), @revokedVersion),
                (@foreignSession, @foreignActor, @foreignHash, now() - interval '2 minutes',
                 now() + interval '1 hour', true, NULL, NULL, NULL, @foreignVersion);
            """,
            connection);
        command.Parameters.AddWithValue("actor", seeded.ActorId);
        command.Parameters.AddWithValue("foreignActor", seeded.ForeignActorId);
        command.Parameters.AddWithValue("currentSession", seeded.CurrentSessionId);
        command.Parameters.AddWithValue("targetSession", seeded.TargetSessionId);
        command.Parameters.AddWithValue("foreignSession", seeded.ForeignSessionId);
        command.Parameters.AddWithValue("expiredSession", seeded.ExpiredSessionId);
        command.Parameters.AddWithValue("revokedSession", seeded.RevokedSessionId);
        command.Parameters.AddWithValue("authorizationVersion", seeded.AuthorizationVersion);
        command.Parameters.AddWithValue("targetVersion", seeded.TargetVersion);
        command.Parameters.AddWithValue("foreignVersion", seeded.ForeignVersion);
        command.Parameters.AddWithValue("expiredVersion", seeded.ExpiredVersion);
        command.Parameters.AddWithValue("revokedVersion", seeded.RevokedVersion);
        command.Parameters.AddWithValue("currentHash", Digest(0x11));
        command.Parameters.AddWithValue("targetHash", Digest(0x22));
        command.Parameters.AddWithValue("expiredHash", Digest(0x33));
        command.Parameters.AddWithValue("revokedHash", Digest(0x44));
        command.Parameters.AddWithValue("foreignHash", Digest(0x55));
        Assert.AreEqual(7, await command.ExecuteNonQueryAsync());
        return seeded;
    }

    private static async Task SetAppContextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SeededSessions seeded,
        Guid? authorizationVersion = null,
        Guid? currentSessionId = null)
    {
        await SetAppRoleAsync(connection, transaction);
        await using var context = new NpgsqlCommand(
            """
            SELECT set_config('app.current_actor_id', @actor, true),
                   set_config('app.current_scope_kind', 'platform', true),
                   set_config('app.current_session_id', @session, true),
                   set_config('app.current_authorization_version', @version, true)
            """,
            connection,
            transaction);
        context.Parameters.AddWithValue("actor", seeded.ActorId.ToString("D"));
        context.Parameters.AddWithValue(
            "session",
            (currentSessionId ?? seeded.CurrentSessionId).ToString("D"));
        context.Parameters.AddWithValue(
            "version",
            (authorizationVersion ?? seeded.AuthorizationVersion).ToString("D"));
        await context.ExecuteNonQueryAsync();
    }

    private static async Task SetAppRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var role = new NpgsqlCommand(
            "SET LOCAL ROLE agro_identity_app",
            connection,
            transaction);
        await role.ExecuteNonQueryAsync();
    }

    private static async Task<string> RevokeAsync(
        string connectionString,
        SeededSessions seeded,
        Guid targetSessionId,
        Guid expectedVersion,
        DateTimeOffset strongNotBefore,
        Guid newVersion,
        bool commit)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(connection, transaction, seeded);
        await using NpgsqlCommand command = CreateRevokeCommand(
            connection,
            transaction,
            targetSessionId,
            expectedVersion,
            strongNotBefore,
            newVersion);
        string outcome = (string)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The revoke function returned no outcome."));
        if (commit)
        {
            await transaction.CommitAsync();
        }
        else
        {
            await transaction.RollbackAsync();
        }

        return outcome;
    }

    private static NpgsqlCommand CreateRevokeCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid targetSessionId,
        Guid expectedVersion,
        DateTimeOffset strongNotBefore,
        Guid newVersion)
    {
        var command = new NpgsqlCommand(
            """
            SELECT outcome FROM identity.revoke_other_own_session(
                @target, @expectedVersion, @strongNotBefore, @newVersion)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("target", targetSessionId);
        command.Parameters.AddWithValue("expectedVersion", expectedVersion);
        command.Parameters.AddWithValue("strongNotBefore", strongNotBefore);
        command.Parameters.AddWithValue("newVersion", newVersion);
        return command;
    }

    private static async Task<string> RevokeAndJournalAsync(
        string connectionString,
        SeededSessions seeded,
        Guid journalId,
        Guid newVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(connection, transaction, seeded);
        await using NpgsqlCommand revoke = CreateRevokeCommand(
            connection,
            transaction,
            seeded.TargetSessionId,
            seeded.TargetVersion,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            newVersion);
        string outcome = (string)(await revoke.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The revoke function returned no outcome."));
        if (outcome == "revoked")
        {
            await using var journal = new NpgsqlCommand(
                """
                INSERT INTO identity.audit_events
                    ("Id", "UserId", "SessionId", "Action", "Outcome",
                     "Connection", "CorrelationId", "OccurredAtUtc")
                VALUES (@id, @actor, @target, 'session_revoked', 'succeeded',
                        NULL, @correlation, now())
                """,
                connection,
                transaction);
            journal.Parameters.AddWithValue("id", journalId);
            journal.Parameters.AddWithValue("actor", seeded.ActorId);
            journal.Parameters.AddWithValue("target", seeded.TargetSessionId);
            journal.Parameters.AddWithValue("correlation", $"test-{journalId:N}");
            Assert.AreEqual(1, await journal.ExecuteNonQueryAsync());
        }

        await transaction.CommitAsync();
        return outcome;
    }

    private static async Task<bool> IsSessionActiveAsync(
        string connectionString,
        Guid sessionId,
        Guid expectedVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return await ScalarBooleanAsync(
            connection,
            """
            SELECT "RevokedAtUtc" IS NULL AND "Version" = @version
            FROM identity.sessions WHERE "Id" = @session
            """,
            ("session", sessionId),
            ("version", expectedVersion));
    }

    private static async Task AssertPermissionDeniedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        try
        {
            await command.ExecuteNonQueryAsync();
            Assert.Fail("The application role unexpectedly received direct table access.");
        }
        catch (PostgresException error)
            when (error.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
        }
    }

    private static async Task<bool> ScalarBooleanAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database probe returned null."));
    }

    private static async Task<string> ScalarStringAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database text probe returned null."));
    }

    private static async Task<long> ScalarInt64Async(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database count probe returned null."));
    }

    private static async Task<int> ScalarInt32Async(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (int)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database integer probe returned null."));
    }

    private static byte[] Digest(byte value) => Enumerable.Repeat(value, 32).ToArray();

    private sealed record SeededSessions(
        Guid ActorId,
        Guid ForeignActorId,
        Guid CurrentSessionId,
        Guid TargetSessionId,
        Guid ForeignSessionId,
        Guid AuthorizationVersion,
        Guid TargetVersion)
    {
        public Guid ForeignVersion { get; } = Guid.NewGuid();

        public Guid ExpiredSessionId { get; } = Guid.NewGuid();

        public Guid ExpiredVersion { get; } = Guid.NewGuid();

        public Guid RevokedSessionId { get; } = Guid.NewGuid();

        public Guid RevokedVersion { get; } = Guid.NewGuid();
    }

    private sealed record SessionVersion(Guid SessionId, Guid Version);

    private sealed record BulkResult(
        string Outcome,
        Guid? SessionId,
        DateTimeOffset? RevokedAtUtc,
        Guid? Version);

    private sealed class DatabaseScenario : IAsyncDisposable
    {
        private readonly PostgreSqlTestServer _postgresql;

        private DatabaseScenario(PostgreSqlTestServer postgresql, string connectionString)
        {
            _postgresql = postgresql;
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public static async Task<DatabaseScenario> CreateAsync()
        {
            PostgreSqlTestServer postgresql = RequirePostgreSql();
            string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
            await using IdentityDbContext dbContext = CreateDbContext(connectionString);
            await dbContext.Database.MigrateAsync();
            return new DatabaseScenario(postgresql, connectionString);
        }

        public async ValueTask DisposeAsync()
        {
            await _postgresql.DropDatabaseAsync(ConnectionString, CancellationToken.None);
        }
    }
}

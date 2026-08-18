using System.Data;
using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

public sealed partial class OwnSessionManagementDatabaseSecurityTests
{
    private const string RevokeAllOtherOwnSessionsMigration =
        "20260818230000_AddRevokeAllOtherOwnSessions";

    [TestMethod]
    public async Task RevokeAllOwnMigrationIsFunctionOnlyExecuteOnlyAndNMinusOneCompatible()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using IdentityDbContext dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(RevokeAllOtherOwnSessionsMigration);

            string individualDefinition;
            string bulkOtherDefinition;
            await using (var previous = new NpgsqlConnection(connectionString))
            {
                await previous.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    previous,
                    "SELECT to_regprocedure('identity.revoke_all_own_sessions(timestamptz)') IS NOT NULL"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    previous,
                    "SELECT to_regprocedure('identity.revoke_current_own_session()') IS NOT NULL"));
                individualDefinition = await ScalarStringAsync(
                    previous,
                    """
                    SELECT pg_get_functiondef(
                        'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)'
                            ::regprocedure)
                    """);
                bulkOtherDefinition = await ScalarStringAsync(
                    previous,
                    """
                    SELECT pg_get_functiondef(
                        'identity.revoke_all_other_own_sessions(timestamptz)'
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
                               'identity.revoke_all_own_sessions(timestamptz)', 'EXECUTE')
                       AND has_function_privilege(
                               'agro_identity_app',
                               'identity.revoke_current_own_session()', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'public',
                               'identity.revoke_all_own_sessions(timestamptz)', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'public',
                               'identity.revoke_current_own_session()', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_job',
                               'identity.revoke_all_own_sessions(timestamptz)', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_job',
                               'identity.revoke_current_own_session()', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_discovery',
                               'identity.revoke_all_own_sessions(timestamptz)', 'EXECUTE')
                       AND NOT has_function_privilege(
                               'agro_identity_discovery',
                               'identity.revoke_current_own_session()', 'EXECUTE')
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT count(*) = 2
                       AND bool_and(p.prosecdef)
                       AND bool_and(p.provolatile = 'v')
                       AND bool_and(p.proowner = (SELECT oid FROM pg_roles
                                                  WHERE rolname = 'agro_identity_owner'))
                       AND bool_and(p.proconfig = ARRAY['search_path=pg_catalog'])
                    FROM pg_proc AS p
                    JOIN pg_namespace AS n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'identity'
                      AND p.proname IN (
                          'revoke_all_own_sessions',
                          'revoke_current_own_session')
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
                Assert.AreEqual(
                    individualDefinition,
                    await ScalarStringAsync(
                        expanded,
                        """
                        SELECT pg_get_functiondef(
                            'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)'
                                ::regprocedure)
                        """));
                Assert.AreEqual(
                    bulkOtherDefinition,
                    await ScalarStringAsync(
                        expanded,
                        """
                        SELECT pg_get_functiondef(
                            'identity.revoke_all_other_own_sessions(timestamptz)'
                                ::regprocedure)
                        """));
            }

            await migrator.MigrateAsync(RevokeAllOtherOwnSessionsMigration);
            await using (var rolledBack = new NpgsqlConnection(connectionString))
            {
                await rolledBack.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regprocedure('identity.revoke_all_own_sessions(timestamptz)') IS NOT NULL"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regprocedure('identity.revoke_current_own_session()') IS NOT NULL"));
                Assert.AreEqual(
                    individualDefinition,
                    await ScalarStringAsync(
                        rolledBack,
                        """
                        SELECT pg_get_functiondef(
                            'identity.revoke_other_own_session(uuid,uuid,timestamptz,uuid)'
                                ::regprocedure)
                        """));
                Assert.AreEqual(
                    bulkOtherDefinition,
                    await ScalarStringAsync(
                        rolledBack,
                        """
                        SELECT pg_get_functiondef(
                            'identity.revoke_all_other_own_sessions(timestamptz)'
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
    public async Task RevokeAllOwnHandlesZeroOneAndManyWithPurposePrivacyAndCutoffSafety()
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
        BulkResult[] wrongPurpose = await RevokeAllOwnAsync(
            scenario.ConnectionString,
            seeded,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        AssertGlobalSentinel(wrongPurpose, "strong_authentication_required");
        await ExecuteAsync(
            scenario.ConnectionString,
            """
            UPDATE identity.sessions
            SET "StrongAuthenticationPurpose" = 'manage_sessions'
            WHERE "Id" = @session
            """,
            ("session", seeded.CurrentSessionId));

        BulkResult[] staleCutoff = await RevokeAllOwnAsync(
            scenario.ConnectionString,
            seeded,
            DateTimeOffset.UtcNow.AddSeconds(-30));
        AssertGlobalSentinel(staleCutoff, "strong_authentication_required");

        BulkResult[] stale = await RevokeAllOwnAsync(
            scenario.ConnectionString,
            seeded,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            authorizationVersion: Guid.NewGuid());
        AssertGlobalSentinel(stale, "not_available");

        await ExecuteAsync(
            scenario.ConnectionString,
            """
            UPDATE identity.sessions
            SET "RevokedAtUtc" = now()
            WHERE "Id" = @target
            """,
            ("target", seeded.TargetSessionId));
        BulkResult[] one = await RevokeAllOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion);
        Assert.HasCount(1, one);
        Assert.AreEqual("revoked", one[0].Outcome);
        Assert.AreEqual(seeded.CurrentSessionId, one[0].SessionId);
        Assert.AreNotEqual(seeded.AuthorizationVersion, one[0].Version);

        SessionVersion nextCurrent = await AddStrongSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x71);
        SessionVersion nextOther = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x72);
        SessionVersion nextThird = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x73);
        BulkResult[] many = await RevokeAllOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            nextCurrent.SessionId,
            nextCurrent.Version);
        Assert.HasCount(3, many);
        Assert.IsTrue(many.All(item => item.Outcome == "revoked"));
        Assert.HasCount(1, many.Select(item => item.RevokedAtUtc).Distinct().ToArray());
        CollectionAssert.AreEquivalent(
            new[] { nextCurrent.SessionId, nextOther.SessionId, nextThird.SessionId },
            many.Select(item => item.SessionId!.Value).ToArray());
        Assert.IsTrue(many.All(item =>
            item.Version is Guid value && value != Guid.Empty));

        BulkResult[] zero = await RevokeAllOwnAsync(
            scenario.ConnectionString,
            seeded,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            nextCurrent.Version,
            nextCurrent.SessionId);
        AssertGlobalSentinel(zero, "no_sessions");

        SessionVersion postCutLogin = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x74);
        BulkResult[] unsafeReplay = await RevokeAllOwnAsync(
            scenario.ConnectionString,
            seeded,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            nextCurrent.Version,
            nextCurrent.SessionId);
        AssertGlobalSentinel(unsafeReplay, "not_available");
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            postCutLogin.SessionId,
            postCutLogin.Version));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.ForeignSessionId,
            seeded.ForeignVersion));
        Assert.AreEqual(4L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    [TestMethod]
    public async Task RevokeCurrentOwnIsSerializedIdempotentAndActorScoped()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        BulkResult first = await RevokeCurrentOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion);
        Assert.AreEqual("revoked", first.Outcome);
        Assert.AreEqual(seeded.CurrentSessionId, first.SessionId);
        Assert.IsNotNull(first.RevokedAtUtc);
        Assert.IsNotNull(first.Version);

        BulkResult replay = await RevokeCurrentOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion);
        Assert.AreEqual("already_revoked", replay.Outcome);
        Assert.AreEqual(first.SessionId, replay.SessionId);
        Assert.AreEqual(first.RevokedAtUtc, replay.RevokedAtUtc);
        Assert.AreEqual(first.Version, replay.Version);
        Assert.AreEqual(1L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));

        BulkResult foreign = await RevokeCurrentOwnAsync(
            scenario.ConnectionString,
            seeded,
            seeded.ForeignSessionId,
            seeded.ForeignVersion);
        AssertGlobalRow(foreign, "not_available", fieldsAreNull: true);
        BulkResult stale = await RevokeCurrentOwnAsync(
            scenario.ConnectionString,
            seeded,
            seeded.TargetSessionId,
            Guid.NewGuid());
        AssertGlobalRow(stale, "not_available", fieldsAreNull: true);
        BulkResult expired = await RevokeCurrentOwnAsync(
            scenario.ConnectionString,
            seeded,
            seeded.ExpiredSessionId,
            seeded.ExpiredVersion);
        AssertGlobalRow(expired, "not_available", fieldsAreNull: true);
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.ForeignSessionId,
            seeded.ForeignVersion));
    }

    [TestMethod]
    public async Task RevokeAllOwnKeepsLoginCommittedAfterItsStatementCutoff()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        await using var globalConnection = new NpgsqlConnection(scenario.ConnectionString);
        await globalConnection.OpenAsync();
        await using NpgsqlTransaction globalTransaction =
            await globalConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await SetAppContextAsync(globalConnection, globalTransaction, seeded);
        BulkResult[] changed = await ExecuteRevokeAllOwnAsync(
            globalConnection,
            globalTransaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.HasCount(2, changed);

        SessionVersion postCutLogin = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x75);
        foreach (BulkResult row in changed)
        {
            await InsertSessionRevokedJournalAsync(
                globalConnection,
                globalTransaction,
                seeded.ActorId,
                row.SessionId!.Value,
                Guid.NewGuid());
        }
        await globalTransaction.CommitAsync();

        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            postCutLogin.SessionId,
            postCutLogin.Version));
        Assert.AreEqual(2L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    [TestMethod]
    public async Task CrossCurrentGlobalSerializesWithGlobalIndividualBulkAndLogout()
    {
        await AssertCrossCurrentGlobalVsGlobalAsync();
        await AssertCrossCurrentGlobalVsIndividualAsync();
        await AssertCrossCurrentGlobalVsBulkOtherAsync();
        await AssertCrossCurrentGlobalVsLogoutAsync();
        await AssertCrossCurrentLogoutVsGlobalSameCurrentAsync();
    }

    [TestMethod]
    public async Task RevokeAllOwnJournalFailureCancellationAndPoolReuseAreAtomic()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        SessionVersion third = await AddActiveSessionAsync(
            scenario.ConnectionString,
            seeded.ActorId,
            0x76);

        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
            await SetAppContextAsync(connection, transaction, seeded);
            BulkResult[] rows = await ExecuteRevokeAllOwnAsync(
                connection,
                transaction,
                DateTimeOffset.UtcNow.AddMinutes(-5));
            Assert.HasCount(3, rows);
            Guid duplicateJournalId = Guid.NewGuid();
            await InsertSessionRevokedJournalAsync(
                connection,
                transaction,
                seeded.ActorId,
                rows[0].SessionId!.Value,
                duplicateJournalId);
            try
            {
                await InsertSessionRevokedJournalAsync(
                    connection,
                    transaction,
                    seeded.ActorId,
                    rows[1].SessionId!.Value,
                    duplicateJournalId);
                Assert.Fail("A duplicate journal identifier unexpectedly committed.");
            }
            catch (PostgresException error)
                when (error.SqlState == PostgresErrorCodes.UniqueViolation)
            {
            }
            await transaction.RollbackAsync();
        }

        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            third.SessionId,
            third.Version));
        Assert.AreEqual(0L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));

        await using (var blocker = new NpgsqlConnection(scenario.ConnectionString))
        {
            await blocker.OpenAsync();
            await using NpgsqlTransaction blockerTransaction =
                await blocker.BeginTransactionAsync();
            await using (var lockTable = new NpgsqlCommand(
                "LOCK TABLE identity.sessions IN ACCESS EXCLUSIVE MODE",
                blocker,
                blockerTransaction))
            {
                await lockTable.ExecuteNonQueryAsync();
            }

            await using var cancelled = new NpgsqlConnection(scenario.ConnectionString);
            await cancelled.OpenAsync();
            await using NpgsqlTransaction cancelledTransaction =
                await cancelled.BeginTransactionAsync();
            await SetAppContextAsync(cancelled, cancelledTransaction, seeded);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
            try
            {
                _ = await ExecuteRevokeAllOwnAsync(
                    cancelled,
                    cancelledTransaction,
                    DateTimeOffset.UtcNow.AddMinutes(-5),
                    cancellation.Token);
                Assert.Fail("The blocked global revocation unexpectedly ignored cancellation.");
            }
            catch (OperationCanceledException)
            {
            }
            await cancelledTransaction.RollbackAsync();
            await blockerTransaction.RollbackAsync();
        }

        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion));

        var poolBuilder = new NpgsqlConnectionStringBuilder(scenario.ConnectionString)
        {
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 1,
            ApplicationName = $"revoke-all-own-sessions-{Guid.NewGuid():N}",
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
            BulkResult[] result = await ExecuteRevokeAllOwnAsync(
                reused,
                transaction,
                DateTimeOffset.UtcNow.AddMinutes(-5));
            AssertGlobalSentinel(result, "not_available");
            BulkResult logout = await ExecuteRevokeCurrentOwnAsync(reused, transaction);
            AssertGlobalRow(logout, "not_available", fieldsAreNull: true);
            await transaction.RollbackAsync();
        }
        NpgsqlConnection.ClearPool(new NpgsqlConnection(poolBuilder.ConnectionString));
    }

    private static async Task AssertCrossCurrentGlobalVsGlobalAsync()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        await MakeTargetStrongAsync(scenario.ConnectionString, seeded.TargetSessionId);

        await using var holder = new NpgsqlConnection(scenario.ConnectionString);
        await holder.OpenAsync();
        await using NpgsqlTransaction holderTransaction = await holder.BeginTransactionAsync();
        await SetAppContextAsync(
            holder,
            holderTransaction,
            seeded,
            seeded.TargetVersion,
            seeded.TargetSessionId);
        int holderPid = await HoldActorAdvisoryAsync(
            holder,
            holderTransaction,
            seeded.ActorId);

        Task<BulkResult[]> fromA = RevokeAllOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion);
        await WaitForAdvisoryWaiterAsync(scenario.ConnectionString, holderPid);

        BulkResult[] fromB = await ExecuteRevokeAllOwnAsync(
            holder,
            holderTransaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        await JournalRevokedRowsAsync(holder, holderTransaction, seeded.ActorId, fromB);
        await holderTransaction.CommitAsync();
        BulkResult[] replay = await fromA;

        Assert.HasCount(2, fromB);
        Assert.IsTrue(fromB.All(row => row.Outcome == "revoked"));
        AssertGlobalSentinel(replay, "no_sessions");
        Assert.AreEqual(2L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    private static async Task AssertCrossCurrentGlobalVsIndividualAsync()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        await MakeTargetStrongAsync(scenario.ConnectionString, seeded.TargetSessionId);

        await using var holder = new NpgsqlConnection(scenario.ConnectionString);
        await holder.OpenAsync();
        await using NpgsqlTransaction holderTransaction = await holder.BeginTransactionAsync();
        await SetAppContextAsync(
            holder,
            holderTransaction,
            seeded,
            seeded.TargetVersion,
            seeded.TargetSessionId);
        int holderPid = await HoldActorAdvisoryAsync(
            holder,
            holderTransaction,
            seeded.ActorId);

        Task<string> fromAToB = RevokeAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            Guid.NewGuid(),
            Guid.NewGuid());
        await WaitForAdvisoryWaiterAsync(scenario.ConnectionString, holderPid);

        BulkResult[] globalFromB = await ExecuteRevokeAllOwnAsync(
            holder,
            holderTransaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        await JournalRevokedRowsAsync(
            holder,
            holderTransaction,
            seeded.ActorId,
            globalFromB);
        await holderTransaction.CommitAsync();
        Assert.AreEqual("not_available", await fromAToB);
        Assert.AreEqual(2L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    private static async Task AssertCrossCurrentGlobalVsBulkOtherAsync()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        await MakeTargetStrongAsync(scenario.ConnectionString, seeded.TargetSessionId);

        await using var holder = new NpgsqlConnection(scenario.ConnectionString);
        await holder.OpenAsync();
        await using NpgsqlTransaction holderTransaction = await holder.BeginTransactionAsync();
        await SetAppContextAsync(
            holder,
            holderTransaction,
            seeded,
            seeded.TargetVersion,
            seeded.TargetSessionId);
        int holderPid = await HoldActorAdvisoryAsync(
            holder,
            holderTransaction,
            seeded.ActorId);

        Task<BulkResult[]> globalFromA = RevokeAllOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion);
        await WaitForAdvisoryWaiterAsync(scenario.ConnectionString, holderPid);

        BulkResult[] bulkFromB = await ExecuteRevokeAllAsync(
            holder,
            holderTransaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        await JournalRevokedRowsAsync(
            holder,
            holderTransaction,
            seeded.ActorId,
            bulkFromB);
        await holderTransaction.CommitAsync();
        BulkResult[] refused = await globalFromA;

        Assert.HasCount(1, bulkFromB);
        Assert.AreEqual(seeded.CurrentSessionId, bulkFromB[0].SessionId);
        AssertGlobalSentinel(refused, "not_available");
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));
        Assert.AreEqual(1L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    private static async Task AssertCrossCurrentGlobalVsLogoutAsync()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);
        await MakeTargetStrongAsync(scenario.ConnectionString, seeded.TargetSessionId);

        await using var holder = new NpgsqlConnection(scenario.ConnectionString);
        await holder.OpenAsync();
        await using NpgsqlTransaction holderTransaction = await holder.BeginTransactionAsync();
        await SetAppContextAsync(holder, holderTransaction, seeded);
        int holderPid = await HoldActorAdvisoryAsync(
            holder,
            holderTransaction,
            seeded.ActorId);

        Task<BulkResult> logoutFromB = RevokeCurrentOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            seeded.TargetSessionId,
            seeded.TargetVersion);
        await WaitForAdvisoryWaiterAsync(scenario.ConnectionString, holderPid);

        BulkResult[] globalFromA = await ExecuteRevokeAllOwnAsync(
            holder,
            holderTransaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        await JournalRevokedRowsAsync(
            holder,
            holderTransaction,
            seeded.ActorId,
            globalFromA);
        await holderTransaction.CommitAsync();
        BulkResult logoutReplay = await logoutFromB;

        Assert.HasCount(2, globalFromA);
        Assert.AreEqual("already_revoked", logoutReplay.Outcome);
        Assert.AreEqual(seeded.TargetSessionId, logoutReplay.SessionId);
        Assert.AreEqual(2L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    private static async Task AssertCrossCurrentLogoutVsGlobalSameCurrentAsync()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        SeededSessions seeded = await SeedAsync(scenario.ConnectionString);

        await using var holder = new NpgsqlConnection(scenario.ConnectionString);
        await holder.OpenAsync();
        await using NpgsqlTransaction holderTransaction = await holder.BeginTransactionAsync();
        await SetAppContextAsync(holder, holderTransaction, seeded);
        int holderPid = await HoldActorAdvisoryAsync(
            holder,
            holderTransaction,
            seeded.ActorId);

        Task<BulkResult[]> globalFromA = RevokeAllOwnAndJournalAsync(
            scenario.ConnectionString,
            seeded,
            seeded.CurrentSessionId,
            seeded.AuthorizationVersion);
        await WaitForAdvisoryWaiterAsync(scenario.ConnectionString, holderPid);

        BulkResult logoutFromA = await ExecuteRevokeCurrentOwnAsync(
            holder,
            holderTransaction);
        Assert.AreEqual("revoked", logoutFromA.Outcome);
        Assert.AreEqual(seeded.CurrentSessionId, logoutFromA.SessionId);
        await InsertSessionRevokedJournalAsync(
            holder,
            holderTransaction,
            seeded.ActorId,
            logoutFromA.SessionId!.Value,
            Guid.NewGuid());
        await holderTransaction.CommitAsync();
        BulkResult[] refused = await globalFromA;

        AssertGlobalSentinel(refused, "not_available");
        Assert.IsTrue(await IsSessionActiveAsync(
            scenario.ConnectionString,
            seeded.TargetSessionId,
            seeded.TargetVersion));
        Assert.AreEqual(1L, await CountSessionRevokedJournalsAsync(
            scenario.ConnectionString,
            seeded.ActorId));
    }

    private static async Task MakeTargetStrongAsync(
        string connectionString,
        Guid targetSessionId) =>
        await ExecuteAsync(
            connectionString,
            """
            UPDATE identity.sessions
            SET "StrongAuthenticatedAtUtc" = now() - interval '1 minute',
                "StrongAuthenticationPurpose" = 'manage_sessions'
            WHERE "Id" = @session
            """,
            ("session", targetSessionId));

    private static async Task<int> HoldActorAdvisoryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actorId)
    {
        int backendPid = await ScalarInt32Async(
            connection,
            transaction,
            "SELECT pg_backend_pid()");
        await using var hold = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(CAST(@actor AS text), 0))",
            connection,
            transaction);
        hold.Parameters.AddWithValue("actor", actorId.ToString("D"));
        await hold.ExecuteNonQueryAsync();
        return backendPid;
    }

    private static async Task JournalRevokedRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actorId,
        IEnumerable<BulkResult> rows)
    {
        foreach (BulkResult row in rows)
        {
            Assert.AreEqual("revoked", row.Outcome);
            await InsertSessionRevokedJournalAsync(
                connection,
                transaction,
                actorId,
                row.SessionId!.Value,
                Guid.NewGuid());
        }
    }

    private static async Task<BulkResult[]> RevokeAllOwnAsync(
        string connectionString,
        SeededSessions seeded,
        DateTimeOffset strongNotBefore,
        Guid? authorizationVersion = null,
        Guid? currentSessionId = null)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(
            connection,
            transaction,
            seeded,
            authorizationVersion,
            currentSessionId);
        BulkResult[] result = await ExecuteRevokeAllOwnAsync(
            connection,
            transaction,
            strongNotBefore);
        await transaction.RollbackAsync();
        return result;
    }

    private static async Task<BulkResult[]> RevokeAllOwnAndJournalAsync(
        string connectionString,
        SeededSessions seeded,
        Guid currentSessionId,
        Guid authorizationVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(
            connection,
            transaction,
            seeded,
            authorizationVersion,
            currentSessionId);
        BulkResult[] result = await ExecuteRevokeAllOwnAsync(
            connection,
            transaction,
            DateTimeOffset.UtcNow.AddMinutes(-5));
        foreach (BulkResult row in result.Where(item => item.Outcome == "revoked"))
        {
            await InsertSessionRevokedJournalAsync(
                connection,
                transaction,
                seeded.ActorId,
                row.SessionId!.Value,
                Guid.NewGuid());
        }
        await transaction.CommitAsync();
        return result;
    }

    private static async Task<BulkResult[]> ExecuteRevokeAllOwnAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset strongNotBefore,
        CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT outcome, session_id, revoked_at_utc, version
            FROM identity.revoke_all_own_sessions(@strongNotBefore)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("strongNotBefore", strongNotBefore);
        var results = new List<BulkResult>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadBulkResult(reader));
        }
        return results.ToArray();
    }

    private static async Task<BulkResult> RevokeCurrentOwnAsync(
        string connectionString,
        SeededSessions seeded,
        Guid currentSessionId,
        Guid authorizationVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(
            connection,
            transaction,
            seeded,
            authorizationVersion,
            currentSessionId);
        BulkResult result = await ExecuteRevokeCurrentOwnAsync(connection, transaction);
        await transaction.RollbackAsync();
        return result;
    }

    private static async Task<BulkResult> RevokeCurrentOwnAndJournalAsync(
        string connectionString,
        SeededSessions seeded,
        Guid currentSessionId,
        Guid authorizationVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetAppContextAsync(
            connection,
            transaction,
            seeded,
            authorizationVersion,
            currentSessionId);
        BulkResult result = await ExecuteRevokeCurrentOwnAsync(connection, transaction);
        if (result.Outcome == "revoked")
        {
            await InsertSessionRevokedJournalAsync(
                connection,
                transaction,
                seeded.ActorId,
                result.SessionId!.Value,
                Guid.NewGuid());
        }
        await transaction.CommitAsync();
        return result;
    }

    private static async Task<BulkResult> ExecuteRevokeCurrentOwnAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT outcome, session_id, revoked_at_utc, version
            FROM identity.revoke_current_own_session()
            """,
            connection,
            transaction);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.IsTrue(await reader.ReadAsync(cancellationToken));
        BulkResult result = ReadBulkResult(reader);
        Assert.IsFalse(await reader.ReadAsync(cancellationToken));
        return result;
    }

    private static BulkResult ReadBulkResult(NpgsqlDataReader reader) =>
        new(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetGuid(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3));

    private static async Task<SessionVersion> AddStrongSessionAsync(
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
                (@session, @actor, @tokenHash, now() - interval '1 minute',
                 now() + interval '1 hour', true, now() - interval '1 minute',
                 'manage_sessions', NULL, @version)
            """,
            ("session", session.SessionId),
            ("actor", actorId),
            ("tokenHash", Digest(tokenByte)),
            ("version", session.Version));
        return session;
    }

    private static void AssertGlobalSentinel(BulkResult[] result, string outcome)
    {
        Assert.HasCount(1, result);
        AssertGlobalRow(result[0], outcome, fieldsAreNull: true);
    }

    private static void AssertGlobalRow(
        BulkResult result,
        string outcome,
        bool fieldsAreNull)
    {
        Assert.AreEqual(outcome, result.Outcome);
        if (fieldsAreNull)
        {
            Assert.IsNull(result.SessionId);
            Assert.IsNull(result.RevokedAtUtc);
            Assert.IsNull(result.Version);
        }
    }
}

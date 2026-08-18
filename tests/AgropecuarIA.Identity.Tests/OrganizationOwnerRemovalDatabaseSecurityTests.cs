using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

#pragma warning disable CA1861 // Test assertions intentionally use compact inline arrays.

[TestClass]
[DoNotParallelize]
public sealed class OrganizationOwnerRemovalDatabaseSecurityTests
{
    private const string PreviousMigration =
        "20260811214842_AddOrganizationOwnerInvitations";

    [TestMethod]
    public async Task MigrationSupportsNMinusOneRollbackAndRollForward()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using var dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            Guid actor = Guid.NewGuid();
            Guid target = Guid.NewGuid();
            Guid organization = Guid.NewGuid();
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var seed = new NpgsqlCommand(
                    """
                    INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
                    VALUES (@actor, 'Rollback actor', now(), 1),
                           (@target, 'Rollback target', now(), 1);
                    INSERT INTO identity.organizations
                        ("Id", "DisplayName", "Status", "CreatedByUserId", "CreatedAtUtc", "Version")
                    VALUES (@organization, 'Rollback organization', 'active', @actor, now(), @version);
                    INSERT INTO identity.memberships
                        ("Id", "OrganizationId", "UserId", "Role", "Status",
                         "SecurityVersion", "CreatedAtUtc")
                    VALUES (gen_random_uuid(), @organization, @actor, 'owner', 'active', 1, now()),
                           (gen_random_uuid(), @organization, @target, 'owner', 'active', 1, now());
                    INSERT INTO identity.organization_memberships
                        ("UserId", "OrganizationId", "OrganizationName", "Role")
                    VALUES (@actor, @organization, 'Rollback organization', 'owner'),
                           (@target, @organization, 'Rollback organization', 'owner');
                    """,
                    connection);
                seed.Parameters.AddWithValue("actor", actor);
                seed.Parameters.AddWithValue("target", target);
                seed.Parameters.AddWithValue("organization", organization);
                seed.Parameters.AddWithValue("version", Guid.NewGuid());
                Assert.AreEqual(7, await seed.ExecuteNonQueryAsync());
            }

            await migrator.MigrateAsync();
            await using (var expanded = new NpgsqlConnection(connectionString))
            {
                await expanded.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT to_regprocedure('identity.remove_active_owner(uuid,uuid,timestamptz,uuid)') IS NOT NULL
                       AND NOT EXISTS (
                            SELECT 1 FROM identity.memberships
                            WHERE "Version" = '00000000-0000-0000-0000-000000000000'::uuid)
                    """));
                Guid nMinusOneUser = Guid.NewGuid();
                await using var legacyWriter = new NpgsqlCommand(
                    """
                    INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
                    VALUES (@user, 'N minus one writer', now(), 1);
                    INSERT INTO identity.memberships
                        ("Id", "OrganizationId", "UserId", "Role", "Status",
                         "SecurityVersion", "CreatedAtUtc")
                    VALUES (gen_random_uuid(), @organization, @user, 'owner', 'active', 1, now());
                    """,
                    expanded);
                legacyWriter.Parameters.AddWithValue("organization", organization);
                legacyWriter.Parameters.AddWithValue("user", nMinusOneUser);
                Assert.AreEqual(2, await legacyWriter.ExecuteNonQueryAsync());
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT "Version" <> '00000000-0000-0000-0000-000000000000'::uuid
                    FROM identity.memberships WHERE "UserId" = @user
                    """,
                    ("user", nMinusOneUser)));

                await using var simulateRemoval = new NpgsqlCommand(
                    """
                    UPDATE identity.memberships
                    SET "Status" = 'removed', "RemovedAtUtc" = now(),
                        "RemovedByUserId" = @actor, "SecurityVersion" = 2,
                        "Version" = gen_random_uuid()
                    WHERE "OrganizationId" = @organization AND "UserId" = @target;
                    DELETE FROM identity.organization_memberships
                    WHERE "OrganizationId" = @organization AND "UserId" = @target;
                    """,
                    expanded);
                simulateRemoval.Parameters.AddWithValue("actor", actor);
                simulateRemoval.Parameters.AddWithValue("target", target);
                simulateRemoval.Parameters.AddWithValue("organization", organization);
                Assert.AreEqual(2, await simulateRemoval.ExecuteNonQueryAsync());
            }

            await migrator.MigrateAsync(PreviousMigration);
            await using (var rolledBack = new NpgsqlConnection(connectionString))
            {
                await rolledBack.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    rolledBack,
                    """
                    SELECT to_regprocedure('identity.remove_active_owner(uuid,uuid,timestamptz,uuid)') IS NULL
                       AND (SELECT "Status" = 'active' FROM identity.memberships
                            WHERE "OrganizationId" = @organization AND "UserId" = @target)
                       AND EXISTS (SELECT 1 FROM identity.organization_memberships
                                   WHERE "OrganizationId" = @organization AND "UserId" = @target)
                    """,
                    ("organization", organization), ("target", target)));
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
    public async Task FunctionsAreAppOnlyAndRemovalRevokesProjectionAndPendingInvitations()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        Guid actor = Guid.NewGuid();
        Guid target = Guid.NewGuid();
        Guid organization = Guid.NewGuid();
        Guid actorSession = Guid.NewGuid();
        Guid targetSession = Guid.NewGuid();
        Guid authorizationVersion = Guid.NewGuid();
        Guid targetMembership = Guid.NewGuid();
        Guid targetVersion = Guid.NewGuid();
        Guid invitation = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();
        await using (var seed = new NpgsqlCommand(
            """
            INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
            VALUES (@actor, 'Removal actor', now(), 1), (@target, 'Removal target', now(), 1);
            INSERT INTO identity.sessions
                ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                 "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                 "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
            VALUES
                (@actorSession, @actor, @actorHash, now() - interval '2 minutes',
                 now() + interval '1 hour', true, now() - interval '1 minute',
                 'manage_organization_owners', NULL, @authorizationVersion),
                (@targetSession, @target, @targetHash, now() - interval '2 minutes',
                 now() + interval '1 hour', true, NULL, NULL, NULL, @targetSessionVersion);
            INSERT INTO identity.organizations
                ("Id", "DisplayName", "Status", "CreatedByUserId", "CreatedAtUtc", "Version")
            VALUES (@organization, 'Removal organization', 'active', @actor,
                    now() - interval '1 day', @organizationVersion);
            INSERT INTO identity.memberships
                ("Id", "OrganizationId", "UserId", "Role", "Status",
                 "SecurityVersion", "CreatedAtUtc", "Version")
            VALUES
                (gen_random_uuid(), @organization, @actor, 'owner', 'active', 1,
                 now() - interval '1 day', gen_random_uuid()),
                (@targetMembership, @organization, @target, 'owner', 'active', 1,
                 now() - interval '1 day', @targetVersion);
            INSERT INTO identity.organization_memberships
                ("UserId", "OrganizationId", "OrganizationName", "Role")
            VALUES (@actor, @organization, 'Removal organization', 'owner'),
                   (@target, @organization, 'Removal organization', 'owner');
            INSERT INTO identity.organization_owner_invitations
                ("Id", "OrganizationId", "CreatedByUserId", "CreationSessionId",
                 "CreationKeyVersion", "CreationKeyDigest", "TokenKeyVersion", "TokenDigest",
                 "Status", "CreatedAtUtc", "ExpiresAtUtc", "Version")
            VALUES (@invitation, @organization, @target, @targetSession,
                    'v1', @creationDigest, 'v1', @tokenDigest, 'pending',
                    now() - interval '1 minute', now() + interval '1 hour', gen_random_uuid());
            """,
            connection))
        {
            seed.Parameters.AddWithValue("actor", actor);
            seed.Parameters.AddWithValue("target", target);
            seed.Parameters.AddWithValue("organization", organization);
            seed.Parameters.AddWithValue("actorSession", actorSession);
            seed.Parameters.AddWithValue("targetSession", targetSession);
            seed.Parameters.AddWithValue("authorizationVersion", authorizationVersion);
            seed.Parameters.AddWithValue("targetSessionVersion", Guid.NewGuid());
            seed.Parameters.AddWithValue("targetMembership", targetMembership);
            seed.Parameters.AddWithValue("targetVersion", targetVersion);
            seed.Parameters.AddWithValue("invitation", invitation);
            seed.Parameters.AddWithValue("organizationVersion", Guid.NewGuid());
            seed.Parameters.AddWithValue("actorHash", Digest(0x11));
            seed.Parameters.AddWithValue("targetHash", Digest(0x12));
            seed.Parameters.AddWithValue("creationDigest", Digest(0x21));
            seed.Parameters.AddWithValue("tokenDigest", Digest(0x22));
            Assert.AreEqual(10, await seed.ExecuteNonQueryAsync());
        }

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT has_function_privilege('agro_identity_app',
                       'identity.remove_active_owner(uuid,uuid,timestamptz,uuid)', 'EXECUTE')
               AND NOT has_function_privilege('public',
                       'identity.remove_active_owner(uuid,uuid,timestamptz,uuid)', 'EXECUTE')
               AND NOT has_table_privilege('agro_identity_app', 'identity.memberships', 'UPDATE')
               AND NOT has_table_privilege('agro_identity_app', 'identity.memberships', 'DELETE')
               AND pg_get_function_result(
                       'identity.list_active_owner_memberships()'::regprocedure) NOT LIKE '%user_id%'
               AND pg_get_function_result(
                       'identity.list_active_owner_memberships()'::regprocedure) LIKE '%is_current_user%'
            """));

        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
        {
            await SetAppContextAsync(
                connection, transaction, actor, organization, actorSession, authorizationVersion);
            await using var remove = new NpgsqlCommand(
                """
                SELECT outcome, membership_id, status, security_version, version,
                       revoked_invitation_ids, is_current_user
                FROM identity.remove_active_owner(
                    @membership, @expectedVersion, now(), @newVersion)
                """,
                connection,
                transaction);
            Guid newVersion = Guid.NewGuid();
            remove.Parameters.AddWithValue("membership", targetMembership);
            remove.Parameters.AddWithValue("expectedVersion", targetVersion);
            remove.Parameters.AddWithValue("newVersion", newVersion);
            await using NpgsqlDataReader reader = await remove.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual("removed", reader.GetString(0));
            Assert.AreEqual(targetMembership, reader.GetGuid(1));
            Assert.AreEqual("removed", reader.GetString(2));
            Assert.AreEqual(2L, reader.GetInt64(3));
            Assert.AreEqual(newVersion, reader.GetGuid(4));
            CollectionAssert.AreEqual(new[] { invitation }, reader.GetFieldValue<Guid[]>(5));
            Assert.IsFalse(reader.GetBoolean(6));
            await reader.CloseAsync();
            await transaction.CommitAsync();
        }

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT (SELECT "Status" = 'removed' AND "SecurityVersion" = 2
                    FROM identity.memberships WHERE "Id" = @membership)
               AND NOT EXISTS (SELECT 1 FROM identity.organization_memberships
                               WHERE "OrganizationId" = @organization AND "UserId" = @target)
               AND (SELECT "Status" = 'revoked' AND "RevokedByUserId" = @actor
                    FROM identity.organization_owner_invitations WHERE "Id" = @invitation)
            """,
            ("membership", targetMembership), ("organization", organization),
            ("target", target), ("actor", actor), ("invitation", invitation)));

        var pooledBuilder = new NpgsqlConnectionStringBuilder(scenario.ConnectionString)
        {
            Pooling = true,
            ApplicationName = $"owner-removal-pool-{Guid.NewGuid():N}",
        };
        await using (var firstLease = new NpgsqlConnection(pooledBuilder.ConnectionString))
        {
            await firstLease.OpenAsync();
            await using NpgsqlTransaction transaction = await firstLease.BeginTransactionAsync();
            await SetAppContextAsync(
                firstLease, transaction, actor, organization, actorSession, authorizationVersion);
            Assert.AreEqual(1L, await ScalarInt64Async(
                firstLease, transaction,
                "SELECT count(*) FROM identity.list_active_owner_memberships()"));
            await transaction.RollbackAsync();
        }

        await using (var secondLease = new NpgsqlConnection(pooledBuilder.ConnectionString))
        {
            await secondLease.OpenAsync();
            await using NpgsqlTransaction transaction = await secondLease.BeginTransactionAsync();
            await using var role = new NpgsqlCommand(
                "SET LOCAL ROLE agro_identity_app", secondLease, transaction);
            await role.ExecuteNonQueryAsync();
            Assert.AreEqual(0L, await ScalarInt64Async(
                secondLease, transaction,
                "SELECT count(*) FROM identity.list_active_owner_memberships()"));
            await transaction.RollbackAsync();
        }

        NpgsqlConnection.ClearPool(new NpgsqlConnection(pooledBuilder.ConnectionString));
    }

    [TestMethod]
    public async Task ConcurrentCrossRemovalLeavesOneActiveOwner()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();
        Guid sessionA = Guid.NewGuid();
        Guid sessionB = Guid.NewGuid();
        Guid authorizationA = Guid.NewGuid();
        Guid authorizationB = Guid.NewGuid();
        Guid organization = Guid.NewGuid();
        Guid membershipA = Guid.NewGuid();
        Guid membershipB = Guid.NewGuid();
        Guid versionA = Guid.NewGuid();
        Guid versionB = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var seed = new NpgsqlCommand(
                """
                INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
                VALUES (@ownerA, 'Race owner A', now(), 1),
                       (@ownerB, 'Race owner B', now(), 1);
                INSERT INTO identity.sessions
                    ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                     "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                     "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
                VALUES
                    (@sessionA, @ownerA, @hashA, now() - interval '2 minutes',
                     now() + interval '1 hour', true, now() - interval '1 minute',
                     'manage_organization_owners', NULL, @authorizationA),
                    (@sessionB, @ownerB, @hashB, now() - interval '2 minutes',
                     now() + interval '1 hour', true, now() - interval '1 minute',
                     'manage_organization_owners', NULL, @authorizationB);
                INSERT INTO identity.organizations
                    ("Id", "DisplayName", "Status", "CreatedByUserId", "CreatedAtUtc", "Version")
                VALUES (@organization, 'Race organization', 'active', @ownerA,
                        now() - interval '1 day', gen_random_uuid());
                INSERT INTO identity.memberships
                    ("Id", "OrganizationId", "UserId", "Role", "Status",
                     "SecurityVersion", "CreatedAtUtc", "Version")
                VALUES
                    (@membershipA, @organization, @ownerA, 'owner', 'active', 1,
                     now() - interval '1 day', @versionA),
                    (@membershipB, @organization, @ownerB, 'owner', 'active', 1,
                     now() - interval '1 day', @versionB);
                INSERT INTO identity.organization_memberships
                    ("UserId", "OrganizationId", "OrganizationName", "Role")
                VALUES (@ownerA, @organization, 'Race organization', 'owner'),
                       (@ownerB, @organization, 'Race organization', 'owner');
                """,
                connection);
            seed.Parameters.AddWithValue("ownerA", ownerA);
            seed.Parameters.AddWithValue("ownerB", ownerB);
            seed.Parameters.AddWithValue("sessionA", sessionA);
            seed.Parameters.AddWithValue("sessionB", sessionB);
            seed.Parameters.AddWithValue("authorizationA", authorizationA);
            seed.Parameters.AddWithValue("authorizationB", authorizationB);
            seed.Parameters.AddWithValue("hashA", Digest(0x41));
            seed.Parameters.AddWithValue("hashB", Digest(0x42));
            seed.Parameters.AddWithValue("organization", organization);
            seed.Parameters.AddWithValue("membershipA", membershipA);
            seed.Parameters.AddWithValue("membershipB", membershipB);
            seed.Parameters.AddWithValue("versionA", versionA);
            seed.Parameters.AddWithValue("versionB", versionB);
            Assert.AreEqual(9, await seed.ExecuteNonQueryAsync());
        }

        Task<string> removeB = RemoveAsActorAsync(
            scenario.ConnectionString, ownerA, organization, sessionA, authorizationA,
            membershipB, versionB);
        Task<string> removeA = RemoveAsActorAsync(
            scenario.ConnectionString, ownerB, organization, sessionB, authorizationB,
            membershipA, versionA);
        string[] outcomes = await Task.WhenAll(removeB, removeA);
        Assert.AreEqual(1, outcomes.Count(outcome => outcome == "removed"));
        Assert.IsTrue(outcomes.Any(
            outcome => outcome is "not_available" or PostgresErrorCodes.SerializationFailure));
        await using var verify = new NpgsqlConnection(scenario.ConnectionString);
        await verify.OpenAsync();
        Assert.AreEqual(1L, await ScalarInt64Async(
            verify,
            null,
            """
            SELECT count(*) FROM identity.memberships
            WHERE "OrganizationId" = @organization AND "Status" = 'active'
            """,
            ("organization", organization)));
    }

    private static async Task SetAppContextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actor,
        Guid organization,
        Guid session,
        Guid authorizationVersion)
    {
        await using var role = new NpgsqlCommand("SET LOCAL ROLE agro_identity_app", connection, transaction);
        await role.ExecuteNonQueryAsync();
        await using var context = new NpgsqlCommand(
            """
            SELECT set_config('app.current_actor_id', @actor, true),
                   set_config('app.current_scope_kind', 'tenant', true),
                   set_config('app.current_organization_id', @organization, true),
                   set_config('app.current_session_id', @session, true),
                   set_config('app.current_authorization_version', @authorizationVersion, true)
            """,
            connection,
            transaction);
        context.Parameters.AddWithValue("actor", actor.ToString("D"));
        context.Parameters.AddWithValue("organization", organization.ToString("D"));
        context.Parameters.AddWithValue("session", session.ToString("D"));
        context.Parameters.AddWithValue("authorizationVersion", authorizationVersion.ToString("D"));
        await context.ExecuteNonQueryAsync();
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

    private static async Task<long> ScalarInt64Async(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database count probe returned null."));
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

    private static async Task<string> RemoveAsActorAsync(
        string connectionString,
        Guid actor,
        Guid organization,
        Guid session,
        Guid authorizationVersion,
        Guid membership,
        Guid expectedVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable);
        await SetAppContextAsync(
            connection, transaction, actor, organization, session, authorizationVersion);
        await using var command = new NpgsqlCommand(
            """
            SELECT outcome FROM identity.remove_active_owner(
                @membership, @expectedVersion, now(), @newVersion)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("membership", membership);
        command.Parameters.AddWithValue("expectedVersion", expectedVersion);
        command.Parameters.AddWithValue("newVersion", Guid.NewGuid());
        try
        {
            string outcome = (string)(await command.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("The removal outcome was null."));
            await transaction.CommitAsync();
            return outcome;
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync();
            return exception.SqlState;
        }
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

    private static byte[] Digest(byte value) => Enumerable.Repeat(value, 32).ToArray();
}

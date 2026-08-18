using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ProductiveCoreAuthorizationPortDatabaseTests
{
    private const string PreviousMigration =
        "20260818153846_AddOrganizationOwnerRemoval";

    [TestMethod]
    public async Task MigrationIsAdditiveAppOnlyAndSupportsEphemeralRollbackForward()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using var dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            await using (var before = new NpgsqlConnection(connectionString))
            {
                await before.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    before,
                    "SELECT to_regprocedure('identity.authorize_productive_owner()') IS NOT NULL"));
            }

            await migrator.MigrateAsync();
            await using (var expanded = new NpgsqlConnection(connectionString))
            {
                await expanded.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    "SELECT to_regprocedure('identity.authorize_productive_owner()') IS NOT NULL"));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    "SELECT has_function_privilege('agro_productive_app', 'identity.authorize_productive_owner()', 'EXECUTE')"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    expanded,
                    "SELECT has_function_privilege('agro_identity_app', 'identity.authorize_productive_owner()', 'EXECUTE')"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    expanded,
                    "SELECT has_function_privilege('agro_identity_job', 'identity.authorize_productive_owner()', 'EXECUTE')"));
                Assert.IsFalse(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT has_table_privilege('agro_productive_app', 'identity.sessions', 'SELECT')
                        OR has_table_privilege('agro_productive_app', 'identity.memberships', 'SELECT')
                        OR has_table_privilege('agro_productive_app', 'identity.organizations', 'SELECT')
                    """));
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT NOT rolcanlogin AND NOT rolinherit AND NOT rolsuper
                       AND NOT rolcreatedb AND NOT rolcreaterole
                       AND NOT rolreplication AND NOT rolbypassrls
                    FROM pg_roles WHERE rolname = 'agro_productive_app'
                    """));
            }

            await migrator.MigrateAsync(PreviousMigration);
            await using (var rolledBack = new NpgsqlConnection(connectionString))
            {
                await rolledBack.OpenAsync();
                Assert.IsFalse(await ScalarBooleanAsync(
                    rolledBack,
                    "SELECT to_regprocedure('identity.authorize_productive_owner()') IS NOT NULL"));
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
    public async Task PortRevalidatesLiveSessionOwnerMembershipAndTransactionLocalContext()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using (IdentityDbContext dbContext = CreateDbContext(connectionString))
            {
                await dbContext.Database.MigrateAsync();
            }

            Guid actorId = Guid.NewGuid();
            Guid otherActorId = Guid.NewGuid();
            Guid organizationId = Guid.NewGuid();
            Guid otherOrganizationId = Guid.NewGuid();
            Guid sessionId = Guid.NewGuid();
            Guid authorizationVersion = Guid.NewGuid();
            await SeedAsync(
                connectionString,
                actorId,
                otherActorId,
                organizationId,
                otherOrganizationId,
                sessionId,
                authorizationVersion);

            var poolingBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Pooling = true,
                MinPoolSize = 1,
                MaxPoolSize = 1,
            };
            await using var connection = new NpgsqlConnection(poolingBuilder.ConnectionString);
            await connection.OpenAsync();

            Assert.AreEqual(
                authorizationVersion,
                await AuthorizeAsync(connection, actorId, organizationId, sessionId));
            Assert.IsNull(await AuthorizeAsync(
                connection,
                actorId,
                otherOrganizationId,
                sessionId));
            Assert.IsNull(await AuthorizeAsync(
                connection,
                otherActorId,
                organizationId,
                sessionId));

            await using (NpgsqlTransaction noContext = await connection.BeginTransactionAsync())
            {
                await SetLocalRoleAsync(connection, noContext);
                Assert.IsNull(await ExecuteAuthorizationPortAsync(connection, noContext));
                await noContext.CommitAsync();
            }

            await ExecuteAsync(
                connectionString,
                "UPDATE identity.memberships SET \"Status\" = 'removed', "
                + "\"RemovedAtUtc\" = now(), \"RemovedByUserId\" = @actor, "
                + "\"SecurityVersion\" = \"SecurityVersion\" + 1, \"Version\" = gen_random_uuid() "
                + "WHERE \"OrganizationId\" = @organization AND \"UserId\" = @actor",
                ("actor", actorId),
                ("organization", organizationId));
            Assert.IsNull(await AuthorizeAsync(connection, actorId, organizationId, sessionId));

            await ExecuteAsync(
                connectionString,
                "UPDATE identity.memberships SET \"Status\" = 'active', "
                + "\"RemovedAtUtc\" = NULL, \"RemovedByUserId\" = NULL, "
                + "\"Version\" = gen_random_uuid() "
                + "WHERE \"OrganizationId\" = @organization AND \"UserId\" = @actor; "
                + "UPDATE identity.sessions SET \"RevokedAtUtc\" = now(), \"Version\" = gen_random_uuid() "
                + "WHERE \"Id\" = @session",
                ("actor", actorId),
                ("organization", organizationId),
                ("session", sessionId));
            Assert.IsNull(await AuthorizeAsync(connection, actorId, organizationId, sessionId));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
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

    private static async Task SeedAsync(
        string connectionString,
        Guid actorId,
        Guid otherActorId,
        Guid organizationId,
        Guid otherOrganizationId,
        Guid sessionId,
        Guid authorizationVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
            VALUES (@actor, 'Productive owner', now(), 1),
                   (@otherActor, 'Other owner', now(), 1);
            INSERT INTO identity.sessions
                ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                 "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                 "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
            VALUES (@session, @actor, decode(repeat('11', 32), 'hex'), now(),
                    now() + interval '1 hour', true, NULL, NULL, NULL, @authorizationVersion);
            INSERT INTO identity.organizations
                ("Id", "DisplayName", "Status", "CreatedByUserId", "CreatedAtUtc", "Version")
            VALUES (@organization, 'Productive organization', 'active', @actor, now(), @organizationVersion),
                   (@otherOrganization, 'Other organization', 'active', @otherActor, now(), @otherOrganizationVersion);
            INSERT INTO identity.memberships
                ("Id", "OrganizationId", "UserId", "Role", "Status",
                 "SecurityVersion", "CreatedAtUtc", "Version")
            VALUES (gen_random_uuid(), @organization, @actor, 'owner', 'active', 1, now(), @membershipVersion),
                   (gen_random_uuid(), @otherOrganization, @otherActor, 'owner', 'active', 1, now(), @otherMembershipVersion);
            """,
            connection);
        command.Parameters.AddWithValue("actor", actorId);
        command.Parameters.AddWithValue("otherActor", otherActorId);
        command.Parameters.AddWithValue("organization", organizationId);
        command.Parameters.AddWithValue("otherOrganization", otherOrganizationId);
        command.Parameters.AddWithValue("session", sessionId);
        command.Parameters.AddWithValue("authorizationVersion", authorizationVersion);
        command.Parameters.AddWithValue("organizationVersion", Guid.NewGuid());
        command.Parameters.AddWithValue("otherOrganizationVersion", Guid.NewGuid());
        command.Parameters.AddWithValue("membershipVersion", Guid.NewGuid());
        command.Parameters.AddWithValue("otherMembershipVersion", Guid.NewGuid());
        Assert.AreEqual(7, await command.ExecuteNonQueryAsync());
    }

    private static async Task<Guid?> AuthorizeAsync(
        NpgsqlConnection connection,
        Guid actorId,
        Guid organizationId,
        Guid sessionId)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetLocalRoleAsync(connection, transaction);
        await using (var context = new NpgsqlCommand(
            """
            SELECT set_config('app.current_scope_kind', 'tenant', true),
                   set_config('app.current_actor_id', @actor, true),
                   set_config('app.current_organization_id', @organization, true),
                   set_config('app.current_session_id', @session, true)
            """,
            connection,
            transaction))
        {
            context.Parameters.AddWithValue("actor", actorId.ToString("D"));
            context.Parameters.AddWithValue("organization", organizationId.ToString("D"));
            context.Parameters.AddWithValue("session", sessionId.ToString("D"));
            await context.ExecuteNonQueryAsync();
        }

        Guid? result = await ExecuteAuthorizationPortAsync(connection, transaction);
        await transaction.CommitAsync();
        return result;
    }

    private static async Task SetLocalRoleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand(
            "SET LOCAL ROLE agro_productive_app",
            connection,
            transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<Guid?> ExecuteAuthorizationPortAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand(
            "SELECT identity.authorize_productive_owner()",
            connection,
            transaction);
        object? value = await command.ExecuteScalarAsync();
        return value is Guid result ? result : null;
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

    private static async Task<bool> ScalarBooleanAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database security probe returned null."));
    }
}

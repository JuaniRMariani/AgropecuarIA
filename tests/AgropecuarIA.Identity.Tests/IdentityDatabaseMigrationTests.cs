using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class IdentityDatabaseMigrationTests
{
    [TestMethod]
    public async Task StartupAppliesVersionedIdentitySchemaAndRelationalConstraints()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_name = '__EFMigrationsHistory'
            )
            """), "Startup must apply a versioned EF Core migration, not EnsureCreated.");

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_index AS index
                JOIN pg_class AS table_definition ON table_definition.oid = index.indrelid
                JOIN pg_namespace AS schema_definition ON schema_definition.oid = table_definition.relnamespace
                CROSS JOIN LATERAL (
                    SELECT array_agg(lower(attribute.attname) ORDER BY key.ordinality) AS columns
                    FROM unnest(index.indkey) WITH ORDINALITY AS key(attnum, ordinality)
                    JOIN pg_attribute AS attribute
                      ON attribute.attrelid = index.indrelid
                     AND attribute.attnum = key.attnum
                ) AS indexed_columns
                WHERE schema_definition.nspname = 'identity'
                  AND table_definition.relname = 'external_identities'
                  AND index.indisunique
                  AND indexed_columns.columns = ARRAY['issuer', 'subject']::text[]
            )
            """), "External issuer/subject must be unique at the database boundary.");

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'identity'
                  AND table_name = 'external_identities'
                  AND lower(column_name) IN ('userid', 'issuer', 'subject')
                GROUP BY table_schema, table_name
                HAVING count(*) = 3 AND bool_and(is_nullable = 'NO')
            )
            """), "External identity ownership and provider keys must be non-null.");

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_constraint AS constraint_definition
                JOIN pg_class AS table_definition
                  ON table_definition.oid = constraint_definition.conrelid
                JOIN pg_namespace AS schema_definition
                  ON schema_definition.oid = table_definition.relnamespace
                WHERE schema_definition.nspname = 'identity'
                  AND table_definition.relname IN ('external_identities', 'organization_memberships', 'sessions')
                  AND constraint_definition.contype = 'f'
                GROUP BY schema_definition.nspname
                HAVING count(*) >= 3
            )
            """), "Identity-owned rows must retain database-enforced user relationships.");

        Guid auditId = Guid.NewGuid();
        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO identity.audit_events
                ("Id", "UserId", "SessionId", "Action", "Outcome", "Connection", "CorrelationId", "OccurredAtUtc")
            VALUES
                (@id, NULL, NULL, 'schema_probe', 'succeeded', NULL, 'migration-test', now())
            """,
            connection))
        {
            insert.Parameters.AddWithValue("id", auditId);
            await insert.ExecuteNonQueryAsync();
        }

        await using var mutation = new NpgsqlCommand(
            "UPDATE identity.audit_events SET \"Outcome\" = 'changed' WHERE \"Id\" = @id",
            connection);
        mutation.Parameters.AddWithValue("id", auditId);
        PostgresException rejection = await Assert.ThrowsExactlyAsync<PostgresException>(
            () => mutation.ExecuteNonQueryAsync());
        Assert.AreEqual(PostgresErrorCodes.ObjectNotInPrerequisiteState, rejection.SqlState);
    }

    [TestMethod]
    public async Task MigrationCanRollbackAndRollForwardOnAnEphemeralDatabase()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(scenario.ConnectionString)
            .Options;
        await using var dbContext = new IdentityDbContext(options);
        IMigrator migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync(Migration.InitialDatabase);
        Assert.IsFalse(await IdentityTableExistsAsync(scenario.ConnectionString));

        await migrator.MigrateAsync();
        Assert.IsTrue(await IdentityTableExistsAsync(scenario.ConnectionString));
    }

    private static async Task<bool> ScalarBooleanAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database probe returned null."));
    }

    private static async Task<bool> IdentityTableExistsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return await ScalarBooleanAsync(
            connection,
            """
            SELECT to_regclass('identity.external_identities') IS NOT NULL
            """);
    }
}

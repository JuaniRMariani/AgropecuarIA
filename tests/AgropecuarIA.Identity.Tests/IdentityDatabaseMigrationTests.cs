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

        Guid journalEntryId = Guid.NewGuid();
        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO identity.audit_events
                ("Id", "UserId", "SessionId", "Action", "Outcome", "Connection", "CorrelationId", "OccurredAtUtc")
            VALUES
                (@id, NULL, NULL, 'schema_probe', 'succeeded', NULL, 'migration-test', now())
            """,
            connection))
        {
            insert.Parameters.AddWithValue("id", journalEntryId);
            await insert.ExecuteNonQueryAsync();
        }

        await using var mutation = new NpgsqlCommand(
            "UPDATE identity.audit_events SET \"Outcome\" = 'changed' WHERE \"Id\" = @id",
            connection);
        mutation.Parameters.AddWithValue("id", journalEntryId);
        PostgresException rejection = await Assert.ThrowsExactlyAsync<PostgresException>(
            () => mutation.ExecuteNonQueryAsync());
        Assert.AreEqual(PostgresErrorCodes.ObjectNotInPrerequisiteState, rejection.SqlState);

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            "SELECT to_regclass('identity.audit_events') IS NOT NULL"));
        Assert.IsFalse(await ScalarBooleanAsync(
            connection,
            "SELECT to_regclass('identity.security_journal_entries') IS NOT NULL"));
        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            "SELECT to_regclass('identity.\"IX_audit_events_UserId_OccurredAtUtc\"') IS NOT NULL"));
    }

    [TestMethod]
    public async Task FoundationMigrationPreservesJournalAndBackfillsLegacyOutboxEnvelope()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(scenario.ConnectionString)
            .Options;
        await using var dbContext = new IdentityDbContext(options);
        IMigrator migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260806002243_InitialIdentity");

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var journalEntryId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc")
                VALUES (@userId, 'Legacy user', '2026-01-02T03:04:05Z');

                INSERT INTO identity.outbox_messages
                    ("EventId", "Type", "Version", "OccurredAtUtc", "AggregateId", "Payload", "DispatchedAtUtc")
                VALUES
                    (@eventId, 'IdentityLinked', 1, '2026-01-02T03:05:00Z', @userId, '{}'::jsonb, NULL);

                INSERT INTO identity.audit_events
                    ("Id", "UserId", "SessionId", "Action", "Outcome", "Connection", "CorrelationId", "OccurredAtUtc")
                VALUES
                    (@journalEntryId, @userId, NULL, 'identity_linked', 'succeeded', 'google', 'legacy-correlation', '2026-01-02T03:05:00Z');
                """,
                connection);
            insert.Parameters.AddWithValue("userId", userId);
            insert.Parameters.AddWithValue("eventId", eventId);
            insert.Parameters.AddWithValue("journalEntryId", journalEntryId);
            await insert.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync();

        var nMinusOneEventId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var legacyWriter = new NpgsqlCommand(
                """
                INSERT INTO identity.outbox_messages
                    ("EventId", "Type", "Version", "OccurredAtUtc", "AggregateId", "Payload", "DispatchedAtUtc")
                VALUES
                    (@eventId, 'IdentityLinked', 1, '2026-01-02T03:06:00Z', @userId, '{}'::jsonb, NULL);

                INSERT INTO identity.audit_events
                    ("Id", "UserId", "SessionId", "Action", "Outcome", "Connection", "CorrelationId", "OccurredAtUtc")
                VALUES
                    (@journalEntryId, @userId, NULL, 'n_minus_one_probe', 'succeeded', NULL, 'n-minus-one', '2026-01-02T03:06:00Z');
                """,
                connection);
            legacyWriter.Parameters.AddWithValue("eventId", nMinusOneEventId);
            legacyWriter.Parameters.AddWithValue("userId", userId);
            legacyWriter.Parameters.AddWithValue("journalEntryId", Guid.NewGuid());
            await legacyWriter.ExecuteNonQueryAsync();
        }

        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var envelope = new NpgsqlCommand(
                """
                SELECT "SchemaVersion", "Source", "ScopeKind", "TenantId", "EffectiveAtUtc",
                       "RecordedAtUtc", "ActorId", "CorrelationId", "AggregateType", "AggregateVersion"
                FROM identity.outbox_messages
                WHERE "EventId" = @eventId
                """,
                connection);
            envelope.Parameters.AddWithValue("eventId", eventId);
            await using var reader = await envelope.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());
            Assert.AreEqual("1.0.0", reader.GetString(0));
            Assert.AreEqual("identity-tenancy", reader.GetString(1));
            Assert.AreEqual("platform", reader.GetString(2));
            Assert.IsTrue(reader.IsDBNull(3));
            Assert.AreEqual(reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5));
            Assert.AreEqual(userId, reader.GetGuid(6));
            StringAssert.StartsWith(reader.GetString(7), "legacy-");
            Assert.AreEqual("PlatformUser", reader.GetString(8));
            Assert.AreEqual(1L, reader.GetInt64(9));
        }

        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var verify = new NpgsqlCommand(
                """
                SELECT
                    (SELECT "Version" FROM identity.users WHERE "Id" = @userId) = 1
                    AND EXISTS (
                        SELECT 1
                        FROM identity.audit_events
                        WHERE "Id" = @journalEntryId)
                    AND to_regclass('identity.audit_events') IS NOT NULL
                """,
                connection);
            verify.Parameters.AddWithValue("userId", userId);
            verify.Parameters.AddWithValue("journalEntryId", journalEntryId);
            Assert.IsTrue((bool)(await verify.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("The migration verification returned null.")));
        }
    }

    [TestMethod]
    public async Task AuthenticationAssuranceMigrationFailsClosedForLegacyRowsAndWriters()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(scenario.ConnectionString)
            .Options;
        await using var dbContext = new IdentityDbContext(options);
        IMigrator migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260806013631_EnforceFoundationContracts");

        Guid userId = Guid.NewGuid();
        Guid preMigrationSessionId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
                VALUES (@userId, 'Legacy assurance user', now(), 1);

                INSERT INTO identity.sessions
                    ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc", "RevokedAtUtc", "Version")
                VALUES
                    (@sessionId, @userId, @tokenHash, now(), now() + interval '1 hour', NULL, @version);
                """,
                connection);
            insert.Parameters.AddWithValue("userId", userId);
            insert.Parameters.AddWithValue("sessionId", preMigrationSessionId);
            insert.Parameters.AddWithValue("tokenHash", Enumerable.Repeat((byte)0x1a, 32).ToArray());
            insert.Parameters.AddWithValue("version", Guid.NewGuid());
            await insert.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync();

        Guid nMinusOneSessionId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var legacyWriter = new NpgsqlCommand(
                """
                INSERT INTO identity.sessions
                    ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc", "RevokedAtUtc", "Version")
                VALUES
                    (@sessionId, @userId, @tokenHash, now(), now() + interval '1 hour', NULL, @version);
                """,
                connection);
            legacyWriter.Parameters.AddWithValue("sessionId", nMinusOneSessionId);
            legacyWriter.Parameters.AddWithValue("userId", userId);
            legacyWriter.Parameters.AddWithValue("tokenHash", Enumerable.Repeat((byte)0x2b, 32).ToArray());
            legacyWriter.Parameters.AddWithValue("version", Guid.NewGuid());
            await legacyWriter.ExecuteNonQueryAsync();

            await using var verifyLegacy = new NpgsqlCommand(
                """
                SELECT bool_and(NOT "IsAuthenticationAssuranceVerified")
                FROM identity.sessions
                WHERE "Id" IN (@preMigrationSessionId, @nMinusOneSessionId)
                """,
                connection);
            verifyLegacy.Parameters.AddWithValue("preMigrationSessionId", preMigrationSessionId);
            verifyLegacy.Parameters.AddWithValue("nMinusOneSessionId", nMinusOneSessionId);
            Assert.IsTrue((bool)(await verifyLegacy.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("The legacy assurance verification returned null.")));
        }

        using var browser = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var verifyCurrentWriter = new NpgsqlCommand(
                """
                SELECT count(*) = 1
                FROM identity.sessions
                WHERE "IsAuthenticationAssuranceVerified"
                """,
                connection);
            Assert.IsTrue((bool)(await verifyCurrentWriter.ExecuteScalarAsync()
                ?? throw new InvalidOperationException("The current writer verification returned null.")));
        }
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

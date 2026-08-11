using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
public sealed class TerritoryDatabaseSecurityTests
{
    private static readonly Guid SeedSnapshotId =
        Guid.Parse("00000000-0000-4000-8000-000000000001");
    private static readonly string[] HomonymOfficialCodes = ["06001", "14001"];
    private static readonly string[] HomonymHierarchyLabels =
    [
        "Departamento de prueba, Buenos Aires",
        "Departamento de prueba, Córdoba",
    ];

    [TestMethod]
    public async Task CleanMigrationSeedsReproducibleNationalSnapshotAndLeastPrivilegeRoles()
    {
        TerritoryDatabasePostgreSqlServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using TerritoryDbContext dbContext = CreateDbContext(connectionString);
            await dbContext.Database.MigrateAsync();

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var command = new NpgsqlCommand(
                """
                SELECT snapshot."Provider", snapshot."Version", snapshot."CapturedAtUtc",
                       encode(snapshot."ContentHash", 'hex'), snapshot."Status",
                       count(unit.*), count(DISTINCT unit."OfficialCode"),
                       bool_or(unit."OfficialCode" = '94')
                FROM territory.snapshots AS snapshot
                JOIN territory.official_units AS unit ON unit."SnapshotId" = snapshot."Id"
                WHERE snapshot."Id" = @snapshotId
                GROUP BY snapshot."Provider", snapshot."Version", snapshot."CapturedAtUtc",
                         snapshot."ContentHash", snapshot."Status"
                """,
                connection))
            {
                command.Parameters.AddWithValue("snapshotId", SeedSnapshotId);
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                Assert.IsTrue(await reader.ReadAsync());
                Assert.AreEqual("georef", reader.GetString(0));
                Assert.AreEqual("1.0.0", reader.GetString(1));
                Assert.AreEqual(
                    DateTimeOffset.Parse(
                        "2026-08-05T16:33:00Z",
                        System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetFieldValue<DateTimeOffset>(2));
                Assert.AreEqual(
                    "ee27e73d27b1fe45a5010b758e97f073fcf8909f0d8bb46541b8bb4eb9eb6fe7",
                    reader.GetString(3));
                Assert.AreEqual("active", reader.GetString(4));
                Assert.AreEqual(24L, reader.GetInt64(5));
                Assert.AreEqual(24L, reader.GetInt64(6));
                Assert.IsTrue(reader.GetBoolean(7));
            }

            List<TerritoryUnitImport> units = await ReadSeedUnitsAsync(connection);
            Assert.AreEqual(
                "ee27e73d27b1fe45a5010b758e97f073fcf8909f0d8bb46541b8bb4eb9eb6fe7",
                Convert.ToHexString(TerritorySnapshotValidator.ComputeContentHash(units))
                    .ToLowerInvariant());

            await using (var command = new NpgsqlCommand(
                """
                SELECT bool_and(NOT rolcanlogin AND NOT rolinherit AND NOT rolsuper
                                AND NOT rolcreatedb AND NOT rolcreaterole
                                AND NOT rolreplication AND NOT rolbypassrls),
                       count(*)
                FROM pg_catalog.pg_roles
                WHERE rolname IN (
                    'agro_territory_owner', 'agro_territory_app', 'agro_territory_importer')
                """,
                connection))
            {
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                Assert.IsTrue(await reader.ReadAsync());
                Assert.IsTrue(reader.GetBoolean(0));
                Assert.AreEqual(3L, reader.GetInt64(1));
            }

            Assert.IsFalse(await ScalarBooleanAsync(
                connection,
                "SELECT has_schema_privilege('public', 'territory', 'USAGE')"));
            Assert.IsFalse(await ScalarBooleanAsync(
                connection,
                "SELECT has_table_privilege('agro_territory_app', " +
                "'territory.snapshots', 'INSERT,UPDATE,DELETE')"));
            Assert.IsTrue(await ScalarBooleanAsync(
                connection,
                "SELECT has_table_privilege('agro_territory_app', " +
                "'territory.snapshots', 'SELECT')"));
            Assert.IsFalse(await ScalarBooleanAsync(
                connection,
                "SELECT has_function_privilege('public', " +
                "'territory.activate_official_snapshot(uuid)', 'EXECUTE')"));
            Assert.IsFalse(await ScalarBooleanAsync(
                connection,
                "SELECT has_function_privilege('agro_territory_app', " +
                "'territory.activate_official_snapshot(uuid)', 'EXECUTE')"));
            Assert.IsTrue(await ScalarBooleanAsync(
                connection,
                "SELECT has_function_privilege('agro_territory_importer', " +
                "'territory.activate_official_snapshot(uuid)', 'EXECUTE')"));
            Assert.AreEqual(
                "agro_territory_owner",
                await ScalarStringAsync(
                    connection,
                    "SELECT tableowner FROM pg_catalog.pg_tables " +
                    "WHERE schemaname='territory' AND tablename='snapshots'"));
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task ConstraintsAndForcedPoliciesFailClosedOutsideStaging()
    {
        TerritoryDatabasePostgreSqlServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using TerritoryDbContext dbContext = CreateDbContext(connectionString);
            await dbContext.Database.MigrateAsync();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            Guid stagingId = Guid.NewGuid();
            await ExecuteAsync(connection, SnapshotInsertSql(stagingId, "constraints", Digest(3)));
            string stagingProvince = UnitInsertSql(
                stagingId,
                "06",
                "Buenos Aires",
                "province",
                null,
                null,
                null);
            await ExecuteAsync(connection, stagingProvince);
            await AssertSqlStateAsync(
                connection,
                stagingProvince,
                PostgresErrorCodes.UniqueViolation);
            await AssertSqlStateAsync(
                connection,
                UnitInsertSql(stagingId, "06001", "Department", "county", "06", null, null),
                PostgresErrorCodes.CheckViolation);
            await AssertSqlStateAsync(
                connection,
                UnitInsertSql(stagingId, "06002", "Department", "department", "999", null, null),
                PostgresErrorCodes.ForeignKeyViolation);
            await AssertSqlStateAsync(
                connection,
                UnitInsertSql(stagingId, "06003", "Department", "department", "06", -35, null),
                PostgresErrorCodes.CheckViolation);

            await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
            {
                await ExecuteAsync(connection, transaction, "SET LOCAL ROLE agro_territory_app");
                Assert.AreEqual(
                    1L,
                    await ScalarInt64Async(
                        connection,
                        transaction,
                        "SELECT count(*) FROM territory.snapshots"));
                Assert.AreEqual(
                    24L,
                    await ScalarInt64Async(
                        connection,
                        transaction,
                        "SELECT count(*) FROM territory.official_units"));
                await transaction.RollbackAsync();
            }

            await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
            {
                await ExecuteAsync(connection, transaction, "SET LOCAL ROLE agro_territory_importer");
                Assert.AreEqual(
                    0L,
                    await ExecuteNonQueryAsync(
                        connection,
                        transaction,
                        "DELETE FROM territory.snapshots WHERE \"Id\" = " +
                        "'00000000-0000-4000-8000-000000000001'"));
                await transaction.RollbackAsync();
            }

            Assert.AreEqual(
                1L,
                await ScalarInt64Async(
                    connection,
                    transaction: null,
                    "SELECT count(*) FROM territory.snapshots WHERE \"Status\"='active'"));
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task FailedActivationPreservesPriorAndConcurrentValidActivationsKeepOneActive()
    {
        TerritoryDatabasePostgreSqlServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using TerritoryDbContext dbContext = CreateDbContext(connectionString);
            await dbContext.Database.MigrateAsync();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            Guid invalidId = Guid.NewGuid();
            await ExecuteAsync(connection, SnapshotInsertSql(invalidId, "invalid", Digest(4)));
            await ExecuteAsync(
                connection,
                UnitInsertSql(invalidId, "02", "CABA", "province", null, null, null));
            await AssertSqlStateAsync(
                connection,
                $"SELECT territory.activate_official_snapshot('{invalidId}'::uuid)",
                PostgresErrorCodes.CheckViolation);
            Assert.AreEqual(
                SeedSnapshotId,
                await ScalarGuidAsync(
                    connection,
                    "SELECT \"Id\" FROM territory.snapshots WHERE \"Status\"='active'"));

            Guid candidateA = Guid.NewGuid();
            Guid candidateB = Guid.NewGuid();
            byte[] seedHash = Convert.FromHexString(
                "ee27e73d27b1fe45a5010b758e97f073fcf8909f0d8bb46541b8bb4eb9eb6fe7");
            await CloneSeedAsStagingAsync(connection, candidateA, "2.0.0-a", seedHash);
            await CloneSeedAsStagingAsync(connection, candidateB, "2.0.0-b", seedHash);

            Task activateA = ActivateAsImporterAsync(connectionString, candidateA);
            Task activateB = ActivateAsImporterAsync(connectionString, candidateB);
            await Task.WhenAll(activateA, activateB);

            Assert.AreEqual(
                1L,
                await ScalarInt64Async(
                    connection,
                    transaction: null,
                    "SELECT count(*) FROM territory.snapshots WHERE \"Status\"='active'"));
            Assert.AreEqual(
                2L,
                await ScalarInt64Async(
                    connection,
                    transaction: null,
                    "SELECT count(*) FROM territory.snapshots WHERE \"Status\"='retired'"));
            Assert.AreEqual(
                1L,
                await ScalarInt64Async(
                    connection,
                    transaction: null,
                    "SELECT count(*) FROM territory.snapshots " +
                    "WHERE \"Id\"='" + invalidId + "' AND \"Status\"='staging'"));

            await AssertSqlStateAsync(
                connection,
                """
                UPDATE territory.official_units SET "Name"='mutated'
                WHERE "SnapshotId"=(SELECT "Id" FROM territory.snapshots WHERE "Status"='active')
                """,
                PostgresErrorCodes.CheckViolation);
            await AssertSqlStateAsync(
                connection,
                "DELETE FROM territory.snapshots WHERE \"Status\"='active'",
                PostgresErrorCodes.CheckViolation);
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task FirstModuleMigrationSupportsEphemeralRollbackAndRollForward()
    {
        TerritoryDatabasePostgreSqlServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using TerritoryDbContext dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync();
            Assert.AreEqual(24, await dbContext.OfficialTerritoryUnits.CountAsync());

            await migrator.MigrateAsync(Migration.InitialDatabase);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            Assert.IsNull(await ScalarNullableStringAsync(
                connection,
                "SELECT to_regclass('territory.snapshots')::text"));
            Assert.AreEqual(1L, await ScalarInt64Async(connection, null, "SELECT 1"));

            await migrator.MigrateAsync();
            dbContext.ChangeTracker.Clear();
            Assert.AreEqual(24, await dbContext.OfficialTerritoryUnits.CountAsync());
            Assert.AreEqual(1, await dbContext.OfficialTerritorySnapshots
                .CountAsync(snapshot => snapshot.Status == "active"));
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task ProductionRepositoryReadsActiveAndAtomicallyActivatesValidatedSnapshot()
    {
        TerritoryDatabasePostgreSqlServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using TerritoryDbContext dbContext = CreateDbContext(connectionString);
            await dbContext.Database.MigrateAsync();
            var repository = new PostgresTerritoryReferenceRepository(dbContext);

            TerritoryReferenceSearchPage? initial = await repository.SearchAsync(
                new TerritoryReferenceSearchCriteria("tierra", "province", null, 10),
                CancellationToken.None);
            Assert.IsNotNull(initial);
            Assert.AreEqual("1.0.0", initial.Source.Version);
            Assert.AreEqual(1, initial.Items.Count);
            Assert.AreEqual("94", initial.Items[0].OfficialCode);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            List<TerritoryUnitImport> units = await ReadSeedUnitsAsync(connection);
            units.Add(new TerritoryUnitImport(
                "06001",
                "Departamento de prueba",
                "department",
                "06",
                -1E-07,
                -6E-07));
            units.Add(new TerritoryUnitImport(
                "14001",
                "Departamento de prueba",
                "department",
                "14",
                -31.4,
                -64.2));
            Guid nextId = Guid.NewGuid();
            string hash = Convert.ToHexString(TerritorySnapshotValidator.ComputeContentHash(units));
            ValidatedTerritorySnapshot validated = TerritorySnapshotValidator.Validate(
                new TerritorySnapshotImport(
                    nextId,
                    "georef",
                    "repository-2.0.0",
                    DateTimeOffset.Parse(
                        "2026-08-05T16:33:00Z",
                        System.Globalization.CultureInfo.InvariantCulture),
                    hash,
                    units),
                DateTimeOffset.Parse(
                    "2026-08-11T01:00:00Z",
                    System.Globalization.CultureInfo.InvariantCulture));

            await repository.ImportAndActivateAsync(validated, CancellationToken.None);
            TerritoryReferenceSearchPage? activated = await repository.SearchAsync(
                new TerritoryReferenceSearchCriteria("tierra", "province", null, 10),
                CancellationToken.None);
            Assert.IsNotNull(activated);
            Assert.AreEqual("repository-2.0.0", activated.Source.Version);
            Assert.AreEqual("94", activated.Items.Single().OfficialCode);
            TerritoryReferenceSearchPage? hierarchy = await repository.SearchAsync(
                new TerritoryReferenceSearchCriteria("departamento", "department", "06", 10),
                CancellationToken.None);
            Assert.IsNotNull(hierarchy);
            Assert.AreEqual(
                "Departamento de prueba, Buenos Aires",
                hierarchy.Items.Single().HierarchyLabel);
            TerritoryReferenceSearchPage? homonyms = await repository.SearchAsync(
                new TerritoryReferenceSearchCriteria(
                    "departamento de prueba",
                    "department",
                    null,
                    10),
                CancellationToken.None);
            Assert.IsNotNull(homonyms);
            Assert.HasCount(2, homonyms.Items);
            CollectionAssert.AreEqual(
                HomonymOfficialCodes,
                homonyms.Items.Select(item => item.OfficialCode).ToArray());
            CollectionAssert.AreEquivalent(
                HomonymHierarchyLabels,
                homonyms.Items.Select(item => item.HierarchyLabel).ToArray());
            Assert.AreEqual(
                1L,
                await ScalarInt64Async(
                    connection,
                    transaction: null,
                    "SELECT count(*) FROM territory.snapshots WHERE \"Status\"='active'"));
            Assert.AreEqual(
                SeedSnapshotId,
                await ScalarGuidAsync(
                    connection,
                    "SELECT \"Id\" FROM territory.snapshots WHERE \"Status\"='retired'"));
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    private static async Task<List<TerritoryUnitImport>> ReadSeedUnitsAsync(
        NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT "OfficialCode", "Name", "Level", "ParentCode",
                   "CentroidLatitude", "CentroidLongitude"
            FROM territory.official_units
            WHERE "SnapshotId"=@snapshotId
            ORDER BY "OfficialCode"
            """,
            connection);
        command.Parameters.AddWithValue("snapshotId", SeedSnapshotId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        List<TerritoryUnitImport> result = [];
        while (await reader.ReadAsync())
        {
            result.Add(new TerritoryUnitImport(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetDouble(4),
                reader.IsDBNull(5) ? null : reader.GetDouble(5)));
        }

        return result;
    }

    private static async Task CloneSeedAsStagingAsync(
        NpgsqlConnection connection,
        Guid snapshotId,
        string version,
        byte[] contentHash)
    {
        await ExecuteAsync(connection, SnapshotInsertSql(snapshotId, version, contentHash));
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO territory.official_units
                ("SnapshotId", "OfficialCode", "Name", "NormalizedName", "Level",
                 "ParentCode", "CentroidLatitude", "CentroidLongitude")
            SELECT @snapshotId, "OfficialCode", "Name", "NormalizedName", "Level",
                   "ParentCode", "CentroidLatitude", "CentroidLongitude"
            FROM territory.official_units
            WHERE "SnapshotId"=@seedSnapshotId
            """,
            connection);
        command.Parameters.AddWithValue("snapshotId", snapshotId);
        command.Parameters.AddWithValue("seedSnapshotId", SeedSnapshotId);
        Assert.AreEqual(24, await command.ExecuteNonQueryAsync());
    }

    private static async Task ActivateAsImporterAsync(string connectionString, Guid snapshotId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(connection, transaction, "SET LOCAL ROLE agro_territory_importer");
        await using var command = new NpgsqlCommand(
            "SELECT territory.activate_official_snapshot(@snapshotId)",
            connection,
            transaction);
        command.Parameters.AddWithValue("snapshotId", snapshotId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static string SnapshotInsertSql(Guid snapshotId, string version, byte[] contentHash) =>
        $"""
        INSERT INTO territory.snapshots
            ("Id", "Provider", "Version", "CapturedAtUtc", "ContentHash",
             "Status", "ImportedAtUtc", "ActivatedAtUtc")
        VALUES
            ('{snapshotId}', 'georef', '{version}', '2026-08-05T16:33:00Z',
             decode('{Convert.ToHexString(contentHash)}', 'hex'),
             'staging', '2026-08-11T00:00:00Z', NULL)
        """;

    private static string UnitInsertSql(
        Guid snapshotId,
        string code,
        string name,
        string level,
        string? parentCode,
        double? latitude,
        double? longitude) =>
        $"""
        INSERT INTO territory.official_units
            ("SnapshotId", "OfficialCode", "Name", "NormalizedName", "Level",
             "ParentCode", "CentroidLatitude", "CentroidLongitude")
        VALUES
            ('{snapshotId}', '{code}', '{name}', 'normalized', '{level}',
             {SqlLiteral(parentCode)}, {SqlNumber(latitude)}, {SqlNumber(longitude)})
        """;

    private static string SqlLiteral(string? value) =>
        value is null ? "NULL" : $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string SqlNumber(double? value) =>
        value?.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";

    private static byte[] Digest(byte value) => Enumerable.Repeat(value, 32).ToArray();

    private static async Task AssertSqlStateAsync(
        NpgsqlConnection connection,
        string sql,
        string? expectedSqlState)
    {
        if (expectedSqlState is null)
        {
            await ExecuteAsync(connection, sql);
            return;
        }

        await using var command = new NpgsqlCommand(sql, connection);
        PostgresException exception = await Assert.ThrowsExactlyAsync<PostgresException>(
            async () => await command.ExecuteNonQueryAsync());
        Assert.AreEqual(expectedSqlState, exception.SqlState);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> ScalarBooleanAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Database boolean probe returned null."));
    }

    private static async Task<long> ScalarInt64Async(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        object value = await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Database count probe returned null.");
        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<Guid> ScalarGuidAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Database UUID probe returned null."));
    }

    private static async Task<string> ScalarStringAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Database text probe returned null."));
    }

    private static async Task<string?> ScalarNullableStringAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        object? value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? null : (string)value;
    }

    private static TerritoryDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TerritoryDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    "territory"))
            .Options);

    private static TerritoryDatabasePostgreSqlServer RequirePostgreSql() =>
        TerritoryDatabaseTestAssembly.PostgreSql
        ?? throw new AssertFailedException(
            "PostgreSQL integration fixture could not start: " +
            TerritoryDatabaseTestAssembly.StartupError?.Message);
}

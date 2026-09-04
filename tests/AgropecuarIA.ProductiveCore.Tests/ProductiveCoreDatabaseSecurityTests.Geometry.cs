using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    private const string ValidGeometry = """{"type":"Polygon","coordinates":[[[-61,-35],[-60.99,-35],[-60.99,-34.99],[-61,-34.99],[-61,-35]]]}""";

    [TestMethod]
    public async Task InitialGeometryUsesPostgisFactsAndImmutableAtomicSnapshot()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, version, "Spatial facts");
        ProductiveCoreGeometryApplicationService service = CreateGeometryService(scenario);
        ConfiguredFieldGeometryResult result = await service.ConfigureGeometryAsync(
            new(scenario.FirstOrganizationId, field, ValidGeometry, 7.1234m, version), ArchiveContext(scenario), CancellationToken.None);
        Assert.AreEqual("configured", result.SpatialStatus);
        Assert.AreEqual(2L, result.Revision);
        Assert.AreEqual(7.1234m, result.DeclaredAreaHectares);
        Assert.IsTrue(result.CalculatedAreaHectares > 90m);
        Assert.IsNull(result.OfficialProvinceCode);
        Assert.IsNull(result.OfficialDepartmentCode);
        Assert.Contains("MultiPolygon", result.BoundaryGeoJson!);
        ConfiguredFieldGeometryResult read = await service.GetGeometryAsync(scenario.FirstOrganizationId, field, ArchiveContext(scenario), CancellationToken.None);
        Assert.AreEqual(result, read);

        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.IsTrue(await ScalarBooleanAsync(admin, """
            SELECT (SELECT count(*) = 1 FROM productive_core.management_unit_geometry_versions s
                    WHERE public.GeometryType(s."Boundary") = 'MULTIPOLYGON' AND public.ST_SRID(s."Boundary") = 4326
                      AND public.ST_NDims(s."Boundary") = 2 AND public.ST_IsValid(s."Boundary")
                      AND s."CalculatedAreaHectares" = round((public.ST_Area(s."Boundary"::public.geography,true)/10000)::numeric,4))
                AND (SELECT count(*) = 1 FROM productive_core.journal_entries WHERE "Action" = 'management_unit_geometry_configured')
                AND (SELECT count(*) = 1 FROM productive_core.outbox_messages WHERE "EventType" = 'ManagementUnitGeometryConfigured'
                    AND "PayloadJson"->>'calculationMethod' = 'postgis-geography-spheroid')
                AND NOT has_table_privilege('agro_productive_app','productive_core.management_unit_geometry_versions','UPDATE')
                AND NOT has_table_privilege('agro_productive_app','productive_core.management_unit_geometry_versions','DELETE')
                AND (SELECT relrowsecurity AND relforcerowsecurity FROM pg_class WHERE oid='productive_core.management_unit_geometry_versions'::regclass)
            """));

        ProductiveCoreOperationException repeated = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() => service.ConfigureGeometryAsync(
            new(scenario.FirstOrganizationId, field, ValidGeometry, 8m, result.Version), ArchiveContext(scenario), CancellationToken.None));
        Assert.AreEqual("productive_core.field_geometry_already_configured", repeated.Code);
        var renamed = await CreateRenameApplicationService(new PostgresProductiveCoreUnitOfWorkFactory(new TestProductiveDbContextFactory(scenario.RuntimeConnectionString))).RenameFieldDraftAsync(
            new(scenario.FirstOrganizationId, field, "Renamed spatial field", result.Version, new string('r', 32)), ArchiveContext(scenario), CancellationToken.None);
        var archived = await CreateArchiveService(scenario).ArchiveFieldDraftAsync(
            new(scenario.FirstOrganizationId, field, renamed.Version, new string('a', 32)), ArchiveContext(scenario), CancellationToken.None);
        ConfiguredFieldGeometryResult afterArchive = await service.GetGeometryAsync(scenario.FirstOrganizationId, field, ArchiveContext(scenario), CancellationToken.None);
        Assert.AreEqual(result.GeometryVersionId, afterArchive.GeometryVersionId);
        Assert.AreEqual(result.BoundaryGeoJson, afterArchive.BoundaryGeoJson);
        Assert.AreEqual("archived", afterArchive.Status);
        Assert.AreEqual(archived.Version, afterArchive.Version);
    }

    [TestMethod]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[[[-61,-35],[-60,-34],[-61,-34],[-60,-35],[-61,-35]]]}")]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[]}")]
    [DataRow("{\"type\":\"Point\",\"coordinates\":[-61,-35]}")]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[[[-61,-35,0],[-60,-35,0],[-60,-34,0],[-61,-35,0]]]}")]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[[[-181,-35],[-60,-35],[-60,-34],[-181,-35]]]}")]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[[[-61,-35],[-60,-35],[-60,-34],[-61,-34]]]}")]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[[[-61,-35],[-60,-35],[-60,-34],[-61,-35]]],\"crs\":{}}")]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[[[1e999,-35],[-60,-35],[-60,-34],[1e999,-35]]]}")]
    [DataRow("{\"type\":\"Polygon\",\"coordinates\":[[[-61,-35],[-60.999999999,-35],[-60.999999999,-34.999999999],[-61,-34.999999999],[-61,-35]]]}")]
    public async Task InitialGeometryRejectsInvalidInputsWithoutWrites(string geometry)
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, version, "Invalid geometry");
        ProductiveCoreOperationException error = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            CreateGeometryService(scenario).ConfigureGeometryAsync(new(scenario.FirstOrganizationId, field, geometry, 1m, version), ArchiveContext(scenario), CancellationToken.None));
        Assert.AreEqual("productive_core.field_geometry_invalid", error.Code);
        await AssertNoGeometryWritesAsync(scenario, version);
    }

    [TestMethod]
    [DataRow("management_units")]
    [DataRow("management_unit_geometry_versions")]
    [DataRow("journal_entries")]
    [DataRow("outbox_messages")]
    public async Task InitialGeometryRollsBackEverySinkFailure(string table)
    {
        if (table is not ("management_units" or "management_unit_geometry_versions" or "journal_entries" or "outbox_messages"))
            throw new ArgumentOutOfRangeException(nameof(table));
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, version, "Geometry rollback");
        string triggerEvent = table == "management_units" ? "UPDATE" : "INSERT";
        await ExecuteAsync(scenario.ConnectionString, $"""
            CREATE FUNCTION productive_core.test_geometry_fault() RETURNS trigger LANGUAGE plpgsql AS $fault$
            BEGIN RAISE EXCEPTION 'geometry sink fault'; END $fault$;
            CREATE TRIGGER test_geometry_fault BEFORE {triggerEvent} ON productive_core.{table}
                FOR EACH ROW EXECUTE FUNCTION productive_core.test_geometry_fault();
            """);
        ProductiveCoreOperationException error = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            CreateGeometryService(scenario).ConfigureGeometryAsync(new(scenario.FirstOrganizationId, field, ValidGeometry, 1m, version), ArchiveContext(scenario), CancellationToken.None));
        Assert.AreEqual("productive_core.field_geometry_unavailable", error.Code);
        Assert.IsFalse(error.Retryable);
        await AssertNoGeometryWritesAsync(scenario, version);
    }

    [TestMethod]
    public async Task InitialGeometryRejectsForeignAccessAndConcurrentReconfiguration()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, version, "Concurrent geometry");
        ProductiveCoreGeometryApplicationService service = CreateGeometryService(scenario);
        var command = new ConfigureFieldGeometryCommand(scenario.FirstOrganizationId, field, ValidGeometry, 1m, version);
        ProductiveCoreOperationException foreign = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() => service.ConfigureGeometryAsync(
            command, new("foreign-spatial", scenario.SecondActorId, scenario.SecondSessionId, scenario.FirstOrganizationId), CancellationToken.None));
        Assert.AreEqual("productive_core.field_not_available", foreign.Code);
        async Task<int> AttemptAsync()
        {
            try { await service.ConfigureGeometryAsync(command, ArchiveContext(scenario), CancellationToken.None); return 200; }
            catch (ProductiveCoreOperationException error) { return error.StatusCode; }
        }

        int[] attempts = await Task.WhenAll(AttemptAsync(), AttemptAsync());
        Assert.AreEqual(1, attempts.Count(status => status == 200));
        Assert.AreEqual(1, attempts.Count(status => status == 412));
        await using var connection = new NpgsqlConnection(scenario.RuntimeConnectionString);
        await connection.OpenAsync();
        await using var transaction = await BeginAuthorizedAsync(connection, scenario.SecondActorId, scenario.SecondOrganizationId,
            scenario.SecondSessionId, scenario.SecondAuthorizationVersion);
        Assert.AreEqual(0L, await ScalarInt64Async(connection, transaction, "SELECT count(*) FROM productive_core.management_unit_geometry_versions"));
        await transaction.RollbackAsync();
    }

    [TestMethod]
    public async Task InitialGeometryMissingPostgisFailsExplicitlyWithoutClientFallback()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, version, "Missing capability");
        await using var context = new TestProductiveDbContextFactory(scenario.ConnectionString).CreateDbContext();
        await context.GetService<IMigrator>().MigrateAsync("20260904193122_SecureFieldArchival");
        await ExecuteAsync(scenario.ConnectionString, "DROP EXTENSION postgis");
        ProductiveCoreOperationException error = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            CreateGeometryService(scenario).ConfigureGeometryAsync(new(scenario.FirstOrganizationId, field, ValidGeometry, 1m, version), ArchiveContext(scenario), CancellationToken.None));
        Assert.AreEqual("productive_core.field_geometry_unavailable", error.Code);
        Assert.IsFalse(error.Retryable);
        PostgresException migration = await Assert.ThrowsExactlyAsync<PostgresException>(() => context.Database.MigrateAsync());
        Assert.Contains("requires PostGIS pre-provisioned", migration.MessageText);
    }

    [TestMethod]
    public async Task InitialGeometryAcceptsMultiPolygonHolesAndRejectsArchivedDraft()
    {
        const string multi = """{"type":"MultiPolygon","coordinates":[[[[-61,-35],[-60.9,-35],[-60.9,-34.9],[-61,-34.9],[-61,-35]],[[-60.98,-34.98],[-60.96,-34.98],[-60.96,-34.96],[-60.98,-34.96],[-60.98,-34.98]]],[[[-60,-35],[-59.9,-35],[-59.9,-34.9],[-60,-34.9],[-60,-35]]]]}""";
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, version, "MultiPolygon holes");
        ConfiguredFieldGeometryResult result = await CreateGeometryService(scenario).ConfigureGeometryAsync(
            new(scenario.FirstOrganizationId, field, multi, 1m, version), ArchiveContext(scenario), CancellationToken.None);
        Assert.IsTrue(result.CalculatedAreaHectares > 1000m);
        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.IsTrue(await ScalarBooleanAsync(admin, """
            SELECT public.ST_NumGeometries("Boundary")=2 AND public.ST_NumInteriorRings(public.ST_GeometryN("Boundary",1))=1
                FROM productive_core.management_unit_geometry_versions
            """));
        Guid archivedField = Guid.NewGuid();
        Guid archivedVersion = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, archivedField, archivedVersion, "Archived unconfigured");
        var archived = await CreateArchiveService(scenario).ArchiveFieldDraftAsync(
            new(scenario.FirstOrganizationId, archivedField, archivedVersion, new string('k', 32)), ArchiveContext(scenario), CancellationToken.None);
        ProductiveCoreOperationException error = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            CreateGeometryService(scenario).ConfigureGeometryAsync(new(scenario.FirstOrganizationId, archivedField, ValidGeometry, 1m, archived.Version), ArchiveContext(scenario), CancellationToken.None));
        Assert.AreEqual("productive_core.field_geometry_already_configured", error.Code);
    }

    [TestMethod]
    public void InitialGeometryEnforcesActualUtf8PositionAndDeclaredPrecisionLimits()
    {
        InitialFieldGeometryInput.Validate(ValidGeometry.PadRight(InitialFieldGeometryInput.MaximumUtf8Bytes), 1m);
        ProductiveCoreOperationException bytes = Assert.ThrowsExactly<ProductiveCoreOperationException>(() =>
            InitialFieldGeometryInput.Validate(new string('é', 524289), 1m));
        Assert.AreEqual("productive_core.field_geometry_too_large", bytes.Code);
        string manyPositions = "{\"type\":\"Polygon\",\"coordinates\":[[" + string.Join(',', Enumerable.Repeat("[0,0]", 10001)) + "]]}";
        ProductiveCoreOperationException positions = Assert.ThrowsExactly<ProductiveCoreOperationException>(() =>
            InitialFieldGeometryInput.Validate(manyPositions, 1m));
        Assert.AreEqual("productive_core.field_geometry_too_large", positions.Code);
        foreach (decimal invalidArea in new[] { 0m, -1m, 0.00001m, 100000000000000m })
        {
            ProductiveCoreOperationException precision = Assert.ThrowsExactly<ProductiveCoreOperationException>(() =>
                InitialFieldGeometryInput.Validate(ValidGeometry, invalidArea));
            Assert.AreEqual("productive_core.field_geometry_invalid", precision.Code);
        }
    }

    [TestMethod]
    public async Task InitialGeometryAcceptsMaximumPositionsWithoutSimplification()
    {
        double[][] positions = Enumerable.Range(0, 9999).Select(index =>
        {
            double angle = 2 * Math.PI * index / 9999;
            return new[] { -61 + (0.01 * Math.Cos(angle)), -35 + (0.01 * Math.Sin(angle)) };
        }).ToArray();
        double[][] closed = [.. positions, positions[0]];
        string geometry = System.Text.Json.JsonSerializer.Serialize(new { type = "Polygon", coordinates = new[] { closed } });
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, version, "Maximum positions");
        await CreateGeometryService(scenario).ConfigureGeometryAsync(new(scenario.FirstOrganizationId, field, geometry, 1m, version), ArchiveContext(scenario), CancellationToken.None);
        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.IsTrue(await ScalarBooleanAsync(admin, "SELECT public.ST_NPoints(\"Boundary\")=10000 FROM productive_core.management_unit_geometry_versions"));
    }

    private static ProductiveCoreGeometryApplicationService CreateGeometryService(DatabaseScenario scenario) =>
        new(new PostgresProductiveCoreUnitOfWorkFactory(new TestProductiveDbContextFactory(scenario.RuntimeConnectionString)), TimeProvider.System);

    private static async Task AssertNoGeometryWritesAsync(DatabaseScenario scenario, Guid version)
    {
        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.IsTrue(await ScalarBooleanAsync(admin, $"""
            SELECT (SELECT count(*) = 1 FROM productive_core.management_units WHERE "SpatialStatus"='not_configured' AND "Revision"=1 AND "Version"='{version:D}')
                AND (SELECT count(*) = 0 FROM productive_core.management_unit_geometry_versions)
                AND (SELECT count(*) = 0 FROM productive_core.journal_entries)
                AND (SELECT count(*) = 0 FROM productive_core.outbox_messages)
            """));
    }
}

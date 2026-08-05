using System.Text.Json;
using AgropecuarIA.CapacityPlanningSpike;

namespace AgropecuarIA.CapacityPlanningSpike.Tests;

[TestClass]
public sealed class FixtureContractTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [TestMethod]
    public void PilotFixtureProducesExactPublishedGoldenReport()
    {
        var bundle = ReadFixture<CapacityScenarioBundleFixture>("capacity-scenarios.json");
        var golden = ReadFixture<CapacityReportFixture>("capacity-report.pilot.json");
        var costCatalogFixture = ReadFixture<UnitCostCatalogFixture>("unit-cost-catalog.incomplete.json");
        var pilotFixture = bundle.Scenarios.Single(scenario => scenario.Id == "pilot");
        var pilot = Map(pilotFixture);

        var projection = CapacityPlanner.Project(pilot);
        var slo = SloPolicy.CoreMonthly.Evaluate(1_000_000);
        var cost = CostProjector.Project(
            new CostUsage(
                ComputeHours: 720m,
                StorageGibMonths: projection.RetainedObjectGiB,
                EgressGib: 0m,
                JobMillionRows: pilot.Volumes.ImportRowsPerDay / 1_000_000m,
                SupportMonths: 1m),
            Map(costCatalogFixture));

        Assert.AreEqual(golden.ScenarioId, pilot.Id);
        Assert.AreEqual(golden.Projection.AverageRequestsPerSecond, projection.AverageRequestsPerSecond);
        Assert.AreEqual(golden.Projection.PeakRequestsPerSecond, projection.PeakRequestsPerSecond);
        Assert.AreEqual(golden.Projection.RetainedObjectGiB, projection.RetainedObjectGiB);
        Assert.AreEqual(golden.Projection.DailyImportDrainSeconds, projection.DailyImportDrainSeconds);
        Assert.AreEqual(golden.Slo.AvailabilityTarget, slo.AvailabilityTarget);
        Assert.AreEqual(golden.Slo.WindowDays, slo.WindowDays);
        Assert.AreEqual(golden.Slo.ErrorBudgetSeconds, slo.ErrorBudgetSeconds);
        Assert.AreEqual(golden.Slo.BadEventsPerMillion, slo.BadEvents);

        var incomplete = cost as IncompleteCostProjection;
        Assert.IsNotNull(incomplete);
        Assert.AreEqual(golden.Cost.Currency, incomplete.Currency);
        Assert.AreEqual("incomplete", golden.Cost.Status);
        CollectionAssert.AreEqual(
            golden.Cost.MissingDrivers.Order(StringComparer.Ordinal).ToArray(),
            incomplete.MissingDrivers.Order(StringComparer.Ordinal).ToArray());
    }

    [TestMethod]
    public void ScenarioFixtureHasUniqueIdsAndValidConcurrencyBounds()
    {
        var bundle = ReadFixture<CapacityScenarioBundleFixture>("capacity-scenarios.json");
        var uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var scenario in bundle.Scenarios)
        {
            Assert.IsTrue(uniqueIds.Add(scenario.Id), $"Duplicate scenario ID: {scenario.Id}");
            Assert.IsLessThanOrEqualTo(
                scenario.Volumes.RegisteredUsers,
                scenario.Volumes.ConcurrentUsers,
                $"Concurrent users exceed registered users for {scenario.Id}.");
            _ = CapacityPlanner.Project(Map(scenario));
        }
    }

    private static T ReadFixture<T>(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions)
            ?? throw new InvalidDataException($"Fixture '{fileName}' could not be deserialized.");
    }

    private static CapacityScenario Map(ScenarioFixture fixture) => new(
        fixture.Id,
        new ScenarioVolumes(
            fixture.Volumes.Tenants,
            fixture.Volumes.RegisteredUsers,
            fixture.Volumes.ConcurrentUsers,
            fixture.Volumes.Farms,
            fixture.Volumes.Fields,
            fixture.Volumes.DocumentsPerMonth,
            fixture.Volumes.AverageDocumentMiB,
            fixture.Volumes.ImportRowsPerDay,
            fixture.Volumes.JobsPerHour),
        new DemandProfile(
            fixture.Demand.DailyReadRequests,
            fixture.Demand.DailyWriteRequests,
            fixture.Demand.PeakFactor,
            fixture.Demand.RetentionMonths,
            fixture.Demand.ObjectVersionFactor,
            fixture.Demand.WorkerRowsPerSecond));

    private static UnitCostCatalog Map(UnitCostCatalogFixture fixture) => new(
        ParseStatus(fixture.Status),
        fixture.Currency,
        fixture.Region,
        fixture.Source,
        fixture.AsOf,
        fixture.TaxIncluded,
        fixture.Drivers
            .Select(driver => new UnitCostDriver(driver.Id, driver.Low, driver.Base, driver.High))
            .ToArray());

    private static UnitCostCatalogStatus ParseStatus(string status) => status switch
    {
        "incomplete" => UnitCostCatalogStatus.Incomplete,
        "synthetic-test-only" => UnitCostCatalogStatus.SyntheticTestOnly,
        "approved" => UnitCostCatalogStatus.Approved,
        _ => throw new InvalidDataException($"Unknown unit-cost catalog status '{status}'."),
    };

    private sealed record CapacityScenarioBundleFixture(IReadOnlyList<ScenarioFixture> Scenarios);

    private sealed record ScenarioFixture(
        string Id,
        ScenarioVolumesFixture Volumes,
        ScenarioDemandFixture Demand);

    private sealed record ScenarioVolumesFixture(
        long Tenants,
        long RegisteredUsers,
        long ConcurrentUsers,
        long Farms,
        long Fields,
        long DocumentsPerMonth,
        decimal AverageDocumentMiB,
        long ImportRowsPerDay,
        long JobsPerHour);

    private sealed record ScenarioDemandFixture(
        long DailyReadRequests,
        long DailyWriteRequests,
        decimal PeakFactor,
        int RetentionMonths,
        decimal ObjectVersionFactor,
        decimal WorkerRowsPerSecond);

    private sealed record UnitCostCatalogFixture(
        string Status,
        string Currency,
        string? Region,
        string? Source,
        DateOnly? AsOf,
        bool? TaxIncluded,
        IReadOnlyList<UnitCostDriverFixture> Drivers);

    private sealed record UnitCostDriverFixture(
        string Id,
        decimal? Low,
        decimal? Base,
        decimal? High);

    private sealed record CapacityReportFixture(
        string ScenarioId,
        CapacityProjectionFixture Projection,
        SloFixture Slo,
        CostFixture Cost);

    private sealed record CapacityProjectionFixture(
        decimal AverageRequestsPerSecond,
        decimal PeakRequestsPerSecond,
        decimal RetainedObjectGiB,
        decimal DailyImportDrainSeconds);

    private sealed record SloFixture(
        decimal AvailabilityTarget,
        int WindowDays,
        decimal ErrorBudgetSeconds,
        long BadEventsPerMillion);

    private sealed record CostFixture(
        string Status,
        string Currency,
        IReadOnlyList<string> MissingDrivers);
}

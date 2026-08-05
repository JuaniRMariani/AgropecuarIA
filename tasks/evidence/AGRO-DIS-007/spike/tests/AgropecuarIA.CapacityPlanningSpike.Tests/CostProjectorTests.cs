using AgropecuarIA.CapacityPlanningSpike;

namespace AgropecuarIA.CapacityPlanningSpike.Tests;

[TestClass]
public sealed class CostProjectorTests
{
    private static readonly string[] CatalogApprovalBlocker = ["catalog-approval"];

    private static readonly CostUsage Usage = new(
        ComputeHours: 10m,
        StorageGibMonths: 20m,
        EgressGib: 30m,
        JobMillionRows: 40m,
        SupportMonths: 1m);

    [TestMethod]
    public void ProjectMissingPricesReturnsIncompleteAndNeverTreatsThemAsZero()
    {
        var catalog = new UnitCostCatalog(
            UnitCostCatalogStatus.Incomplete,
            "USD",
            Region: null,
            Source: null,
            AsOf: null,
            TaxIncluded: null,
            [
                new UnitCostDriver(CostDriverIds.ComputeHour, 1m, 2m, 3m),
                new UnitCostDriver(CostDriverIds.StorageGibMonth, null, null, null),
            ]);

        var projection = CostProjector.Project(Usage, catalog);
        var incomplete = projection as IncompleteCostProjection;

        Assert.IsNotNull(incomplete);
        CollectionAssert.AreEquivalent(
            new[]
            {
                CostDriverIds.StorageGibMonth,
                CostDriverIds.EgressGib,
                CostDriverIds.JobMillionRows,
                CostDriverIds.SupportMonth,
            },
            incomplete.MissingDrivers.ToArray());
    }

    [TestMethod]
    public void ProjectCompleteSyntheticCatalogReturnsOrderedBandTotals()
    {
        var catalog = CompleteCatalog();

        var projection = CostProjector.Project(Usage, catalog);
        var estimate = projection as EstimatedCostProjection;

        Assert.IsNotNull(estimate);
        Assert.AreEqual(101m, estimate.Low);
        Assert.AreEqual(202m, estimate.Base);
        Assert.AreEqual(303m, estimate.High);
        Assert.IsLessThanOrEqualTo(estimate.Base, estimate.Low);
        Assert.IsLessThanOrEqualTo(estimate.High, estimate.Base);
    }

    [TestMethod]
    public void ProjectUnorderedPriceBandReturnsTypedValidationError()
    {
        var catalog = CompleteCatalog();
        var unordered = catalog with
        {
            Status = UnitCostCatalogStatus.Approved,
            Drivers = catalog.Drivers
                .Select(driver => driver.Id == CostDriverIds.ComputeHour
                    ? driver with { Low = 3m, Base = 2m, High = 1m }
                    : driver)
                .ToArray(),
        };

        var exception = Assert.ThrowsExactly<CapacityPlanningException>(
            () => CostProjector.Project(Usage, unordered));

        Assert.AreEqual(CapacityPlanningErrorCode.InvalidInput, exception.Code);
    }

    [TestMethod]
    public void ProjectNegativeUsageReturnsTypedValidationError()
    {
        var invalid = Usage with { EgressGib = -1m };

        var exception = Assert.ThrowsExactly<CapacityPlanningException>(
            () => CostProjector.Project(invalid, CompleteCatalog()));

        Assert.AreEqual(CapacityPlanningErrorCode.InvalidInput, exception.Code);
    }

    [TestMethod]
    public void ProjectApprovedCatalogWithoutMetadataReturnsTypedValidationError()
    {
        var missingRegion = CompleteCatalog() with { Status = UnitCostCatalogStatus.Approved, Region = null };
        var missingSource = CompleteCatalog() with { Status = UnitCostCatalogStatus.Approved, Source = " " };
        var missingDate = CompleteCatalog() with { Status = UnitCostCatalogStatus.Approved, AsOf = null };
        var missingTaxPolicy = CompleteCatalog() with { Status = UnitCostCatalogStatus.Approved, TaxIncluded = null };

        foreach (var catalog in new[] { missingRegion, missingSource, missingDate, missingTaxPolicy })
        {
            var exception = Assert.ThrowsExactly<CapacityPlanningException>(
                () => CostProjector.Project(Usage, catalog));
            Assert.AreEqual(CapacityPlanningErrorCode.InvalidInput, exception.Code);
        }
    }

    [TestMethod]
    public void ProjectApprovedCatalogWithNullBandReturnsIncomplete()
    {
        var catalog = CompleteCatalog() with
        {
            Status = UnitCostCatalogStatus.Approved,
            Drivers = CompleteCatalog().Drivers
                .Select(driver => driver.Id == CostDriverIds.EgressGib
                    ? driver with { Base = null }
                    : driver)
                .ToArray(),
        };

        var projection = CostProjector.Project(Usage, catalog);
        var incomplete = projection as IncompleteCostProjection;

        Assert.IsNotNull(incomplete);
        CollectionAssert.AreEqual(new[] { CostDriverIds.EgressGib }, incomplete.MissingDrivers.ToArray());
    }

    [TestMethod]
    public void ProjectIncompleteCatalogNeverProducesAnEstimateEvenWithCompletePrices()
    {
        var catalog = CompleteCatalog() with { Status = UnitCostCatalogStatus.Incomplete };

        var projection = CostProjector.Project(Usage, catalog);
        var incomplete = projection as IncompleteCostProjection;

        Assert.IsNotNull(incomplete);
        CollectionAssert.AreEqual(CatalogApprovalBlocker, incomplete.MissingDrivers.ToArray());
    }

    private static UnitCostCatalog CompleteCatalog() => new(
        UnitCostCatalogStatus.SyntheticTestOnly,
        "USD",
        Region: "synthetic-region",
        Source: "synthetic test fixture",
        AsOf: new DateOnly(2026, 8, 5),
        TaxIncluded: false,
        CostDriverIds.Required
            .Select(driver => new UnitCostDriver(driver, 1m, 2m, 3m))
            .ToArray());
}

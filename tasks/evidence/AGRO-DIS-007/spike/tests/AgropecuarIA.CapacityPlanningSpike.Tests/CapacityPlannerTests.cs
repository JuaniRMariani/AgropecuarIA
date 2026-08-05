using AgropecuarIA.CapacityPlanningSpike;

namespace AgropecuarIA.CapacityPlanningSpike.Tests;

[TestClass]
public sealed class CapacityPlannerTests
{
    [TestMethod]
    public void ProjectPilotScenarioReturnsExactDeterministicProjection()
    {
        var projection = CapacityPlanner.Project(CapacityScenarioFactory.Pilot());

        Assert.AreEqual(120_000m / 86_400m, projection.AverageRequestsPerSecond);
        Assert.AreEqual((120_000m / 86_400m) * 8m, projection.PeakRequestsPerSecond);
        Assert.AreEqual(366.2109375m, projection.RetainedObjectGiB);
        Assert.AreEqual(40m, projection.DailyImportDrainSeconds);
    }

    [TestMethod]
    public void ProjectGrowthScenarioIsMonotonicAndExactlyTenTimesPilot()
    {
        var pilot = CapacityPlanner.Project(CapacityScenarioFactory.Pilot());
        var growth = CapacityPlanner.Project(CapacityScenarioFactory.Growth10X());

        Assert.AreEqual(pilot.AverageRequestsPerSecond * 10m, growth.AverageRequestsPerSecond);
        Assert.AreEqual(pilot.PeakRequestsPerSecond * 10m, growth.PeakRequestsPerSecond);
        Assert.AreEqual(pilot.RetainedObjectGiB * 10m, growth.RetainedObjectGiB);
        Assert.AreEqual(pilot.DailyImportDrainSeconds * 10m, growth.DailyImportDrainSeconds);
    }

    [TestMethod]
    public void ProjectBurstScenarioIncreasesEveryDemandProjection()
    {
        var growthScenario = CapacityScenarioFactory.Growth10X();
        var burstScenario = growthScenario with
        {
            Id = "burst-2x",
            Volumes = growthScenario.Volumes with
            {
                DocumentsPerMonth = growthScenario.Volumes.DocumentsPerMonth * 2,
                ImportRowsPerDay = growthScenario.Volumes.ImportRowsPerDay * 2,
            },
            Demand = growthScenario.Demand with
            {
                DailyReadRequests = growthScenario.Demand.DailyReadRequests * 2,
                DailyWriteRequests = growthScenario.Demand.DailyWriteRequests * 2,
                PeakFactor = 10m,
            },
        };

        var growth = CapacityPlanner.Project(growthScenario);
        var burst = CapacityPlanner.Project(burstScenario);

        Assert.IsGreaterThan(growth.AverageRequestsPerSecond, burst.AverageRequestsPerSecond);
        Assert.IsGreaterThan(growth.PeakRequestsPerSecond, burst.PeakRequestsPerSecond);
        Assert.IsGreaterThan(growth.RetainedObjectGiB, burst.RetainedObjectGiB);
        Assert.IsGreaterThan(growth.DailyImportDrainSeconds, burst.DailyImportDrainSeconds);
    }

    [TestMethod]
    public void ProjectZeroOrNegativeInputsReturnTypedValidationErrors()
    {
        var pilot = CapacityScenarioFactory.Pilot();
        var invalidScenarios = new[]
        {
            pilot with { Volumes = pilot.Volumes with { Tenants = 0 } },
            pilot with { Volumes = pilot.Volumes with { AverageDocumentMiB = -1m } },
            pilot with { Demand = pilot.Demand with { DailyReadRequests = 0 } },
            pilot with { Demand = pilot.Demand with { PeakFactor = 0.99m } },
            pilot with { Demand = pilot.Demand with { WorkerRowsPerSecond = 0m } },
        };

        foreach (var scenario in invalidScenarios)
        {
            var exception = Assert.ThrowsExactly<CapacityPlanningException>(
                () => CapacityPlanner.Project(scenario));
            Assert.AreEqual(CapacityPlanningErrorCode.InvalidInput, exception.Code);
        }
    }

    [TestMethod]
    public void ProjectOverflowReturnsTypedCalculationError()
    {
        var pilot = CapacityScenarioFactory.Pilot();
        var overflow = pilot with
        {
            Volumes = pilot.Volumes with
            {
                DocumentsPerMonth = long.MaxValue,
                AverageDocumentMiB = decimal.MaxValue,
            },
        };

        var exception = Assert.ThrowsExactly<CapacityPlanningException>(
            () => CapacityPlanner.Project(overflow));

        Assert.AreEqual(CapacityPlanningErrorCode.CalculationOverflow, exception.Code);
    }
}

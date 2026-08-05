using AgropecuarIA.CapacityPlanningSpike;

namespace AgropecuarIA.CapacityPlanningSpike.Tests;

internal static class CapacityScenarioFactory
{
    internal static CapacityScenario Pilot() => new(
        "pilot",
        new ScenarioVolumes(
            Tenants: 10,
            RegisteredUsers: 100,
            ConcurrentUsers: 20,
            Farms: 200,
            Fields: 2_000,
            DocumentsPerMonth: 10_000,
            AverageDocumentMiB: 2.5m,
            ImportRowsPerDay: 10_000,
            JobsPerHour: 10),
        new DemandProfile(
            DailyReadRequests: 100_000,
            DailyWriteRequests: 20_000,
            PeakFactor: 8m,
            RetentionMonths: 12,
            ObjectVersionFactor: 1.25m,
            WorkerRowsPerSecond: 250m));

    internal static CapacityScenario Growth10X()
    {
        var pilot = Pilot();
        return pilot with
        {
            Id = "growth-10x",
            Volumes = pilot.Volumes with
            {
                Tenants = pilot.Volumes.Tenants * 10,
                RegisteredUsers = pilot.Volumes.RegisteredUsers * 10,
                ConcurrentUsers = pilot.Volumes.ConcurrentUsers * 10,
                Farms = pilot.Volumes.Farms * 10,
                Fields = pilot.Volumes.Fields * 10,
                DocumentsPerMonth = pilot.Volumes.DocumentsPerMonth * 10,
                ImportRowsPerDay = pilot.Volumes.ImportRowsPerDay * 10,
                JobsPerHour = pilot.Volumes.JobsPerHour * 10,
            },
            Demand = pilot.Demand with
            {
                DailyReadRequests = pilot.Demand.DailyReadRequests * 10,
                DailyWriteRequests = pilot.Demand.DailyWriteRequests * 10,
            },
        };
    }
}

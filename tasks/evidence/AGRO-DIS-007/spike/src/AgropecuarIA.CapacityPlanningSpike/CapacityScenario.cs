namespace AgropecuarIA.CapacityPlanningSpike;

public sealed record CapacityScenario(
    string Id,
    ScenarioVolumes Volumes,
    DemandProfile Demand);

public sealed record ScenarioVolumes(
    long Tenants,
    long RegisteredUsers,
    long ConcurrentUsers,
    long Farms,
    long Fields,
    long DocumentsPerMonth,
    decimal AverageDocumentMiB,
    long ImportRowsPerDay,
    long JobsPerHour);

public sealed record DemandProfile(
    long DailyReadRequests,
    long DailyWriteRequests,
    decimal PeakFactor,
    int RetentionMonths,
    decimal ObjectVersionFactor,
    decimal WorkerRowsPerSecond);

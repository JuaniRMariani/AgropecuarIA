namespace AgropecuarIA.CapacityPlanningSpike;

public sealed record CapacityProjection(
    decimal AverageRequestsPerSecond,
    decimal PeakRequestsPerSecond,
    decimal RetainedObjectGiB,
    decimal DailyImportDrainSeconds);

public static class CapacityPlanner
{
    private const decimal SecondsPerDay = 86_400m;
    private const decimal MebibytesPerGibibyte = 1_024m;

    public static CapacityProjection Project(CapacityScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        Validate(scenario);

        try
        {
            checked
            {
                var dailyRequests = scenario.Demand.DailyReadRequests + scenario.Demand.DailyWriteRequests;
                var averageRequestsPerSecond = dailyRequests / SecondsPerDay;
                var peakRequestsPerSecond = averageRequestsPerSecond * scenario.Demand.PeakFactor;
                var retainedObjectGiB =
                    scenario.Volumes.DocumentsPerMonth
                    * scenario.Volumes.AverageDocumentMiB
                    / MebibytesPerGibibyte
                    * scenario.Demand.RetentionMonths
                    * scenario.Demand.ObjectVersionFactor;
                var dailyImportDrainSeconds =
                    scenario.Volumes.ImportRowsPerDay / scenario.Demand.WorkerRowsPerSecond;

                return new CapacityProjection(
                    averageRequestsPerSecond,
                    peakRequestsPerSecond,
                    retainedObjectGiB,
                    dailyImportDrainSeconds);
            }
        }
        catch (OverflowException exception)
        {
            throw new CapacityPlanningException(
                CapacityPlanningErrorCode.CalculationOverflow,
                nameof(scenario),
                "The scenario exceeds the deterministic model's numeric range.",
                exception);
        }
    }

    private static void Validate(CapacityScenario scenario)
    {
        RequireText(scenario.Id, nameof(scenario.Id));
        ArgumentNullException.ThrowIfNull(scenario.Volumes);
        ArgumentNullException.ThrowIfNull(scenario.Demand);

        RequirePositive(scenario.Volumes.Tenants, nameof(scenario.Volumes.Tenants));
        RequirePositive(scenario.Volumes.RegisteredUsers, nameof(scenario.Volumes.RegisteredUsers));
        RequirePositive(scenario.Volumes.ConcurrentUsers, nameof(scenario.Volumes.ConcurrentUsers));
        RequirePositive(scenario.Volumes.Farms, nameof(scenario.Volumes.Farms));
        RequirePositive(scenario.Volumes.Fields, nameof(scenario.Volumes.Fields));
        RequirePositive(scenario.Volumes.DocumentsPerMonth, nameof(scenario.Volumes.DocumentsPerMonth));
        RequirePositive(scenario.Volumes.AverageDocumentMiB, nameof(scenario.Volumes.AverageDocumentMiB));
        RequirePositive(scenario.Volumes.ImportRowsPerDay, nameof(scenario.Volumes.ImportRowsPerDay));
        RequirePositive(scenario.Volumes.JobsPerHour, nameof(scenario.Volumes.JobsPerHour));
        RequirePositive(scenario.Demand.DailyReadRequests, nameof(scenario.Demand.DailyReadRequests));
        RequirePositive(scenario.Demand.DailyWriteRequests, nameof(scenario.Demand.DailyWriteRequests));
        RequireAtLeastOne(scenario.Demand.PeakFactor, nameof(scenario.Demand.PeakFactor));
        RequirePositive(scenario.Demand.RetentionMonths, nameof(scenario.Demand.RetentionMonths));
        RequireAtLeastOne(scenario.Demand.ObjectVersionFactor, nameof(scenario.Demand.ObjectVersionFactor));
        RequirePositive(scenario.Demand.WorkerRowsPerSecond, nameof(scenario.Demand.WorkerRowsPerSecond));

        if (scenario.Volumes.ConcurrentUsers > scenario.Volumes.RegisteredUsers)
        {
            throw Invalid(
                nameof(scenario.Volumes.ConcurrentUsers),
                "Concurrent users cannot exceed registered users.");
        }
    }

    private static void RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid(parameterName, "A non-empty value is required.");
        }
    }

    private static void RequirePositive(decimal value, string parameterName)
    {
        if (value <= 0m)
        {
            throw Invalid(parameterName, "A value greater than zero is required.");
        }
    }

    private static void RequirePositive(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw Invalid(parameterName, "A value greater than zero is required.");
        }
    }

    private static void RequireAtLeastOne(decimal value, string parameterName)
    {
        if (value < 1m)
        {
            throw Invalid(parameterName, "A value greater than or equal to one is required.");
        }
    }

    private static CapacityPlanningException Invalid(string parameterName, string message) =>
        new(CapacityPlanningErrorCode.InvalidInput, parameterName, message);
}

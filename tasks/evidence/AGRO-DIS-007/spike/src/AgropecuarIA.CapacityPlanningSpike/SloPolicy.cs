namespace AgropecuarIA.CapacityPlanningSpike;

public sealed record SloEvaluation(
    decimal AvailabilityTarget,
    int WindowDays,
    decimal ErrorBudgetSeconds,
    long BadEvents);

public sealed record SloPolicy(decimal AvailabilityTarget, int WindowDays)
{
    private const decimal SecondsPerDay = 86_400m;

    public static SloPolicy CoreMonthly { get; } = new(0.999m, 30);

    public SloEvaluation Evaluate(long EligibleEvents)
    {
        if (AvailabilityTarget <= 0m || AvailabilityTarget >= 1m)
        {
            throw Invalid(nameof(AvailabilityTarget), "Availability must be greater than zero and less than one.");
        }

        if (WindowDays <= 0)
        {
            throw Invalid(nameof(WindowDays), "The rolling window must contain at least one day.");
        }

        if (EligibleEvents <= 0)
        {
            throw Invalid(nameof(EligibleEvents), "Eligible events must be greater than zero.");
        }

        try
        {
            checked
            {
                var errorRatio = 1m - AvailabilityTarget;
                var errorBudgetSeconds = WindowDays * SecondsPerDay * errorRatio;
                var badEvents = decimal.ToInt64(decimal.Floor(EligibleEvents * errorRatio));

                return new SloEvaluation(
                    AvailabilityTarget,
                    WindowDays,
                    errorBudgetSeconds,
                    badEvents);
            }
        }
        catch (OverflowException exception)
        {
            throw new CapacityPlanningException(
                CapacityPlanningErrorCode.CalculationOverflow,
                nameof(EligibleEvents),
                "The SLO evaluation exceeds the deterministic model's numeric range.",
                exception);
        }
    }

    private static CapacityPlanningException Invalid(string parameterName, string message) =>
        new(CapacityPlanningErrorCode.InvalidInput, parameterName, message);
}

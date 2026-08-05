namespace AgropecuarIA.GisWeatherSpike;

public sealed record FreshnessContext(
    DateTimeOffset Now,
    DateTimeOffset RetrievedAt,
    DateTimeOffset ValidTo,
    bool HasUsableValue,
    ProviderError? Error);

public interface IFreshnessPolicy
{
    WeatherAvailability Classify(FreshnessContext context);
}

public sealed class FixedFreshnessPolicy : IFreshnessPolicy
{
    private readonly TimeSpan _maximumSnapshotAge;
    private readonly TimeSpan _maximumClockSkew;

    public FixedFreshnessPolicy(TimeSpan maximumSnapshotAge, TimeSpan? maximumClockSkew = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumSnapshotAge, TimeSpan.Zero);

        _maximumSnapshotAge = maximumSnapshotAge;
        _maximumClockSkew = maximumClockSkew ?? TimeSpan.FromMinutes(5);

        ArgumentOutOfRangeException.ThrowIfLessThan(_maximumClockSkew, TimeSpan.Zero, nameof(maximumClockSkew));
    }

    public WeatherAvailability Classify(FreshnessContext context)
    {
        if (!context.HasUsableValue || context.Error is not null)
        {
            return WeatherAvailability.Unavailable;
        }

        if (context.RetrievedAt - context.Now > _maximumClockSkew)
        {
            return WeatherAvailability.Unavailable;
        }

        var age = context.Now - context.RetrievedAt;
        return age <= _maximumSnapshotAge && context.Now <= context.ValidTo
            ? WeatherAvailability.Fresh
            : WeatherAvailability.Stale;
    }
}

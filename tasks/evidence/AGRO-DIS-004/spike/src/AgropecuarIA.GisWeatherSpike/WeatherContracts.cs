namespace AgropecuarIA.GisWeatherSpike;

public enum WeatherNature
{
    Observed,
    Estimated,
    Forecast,
}

public enum WeatherAvailability
{
    Fresh,
    Stale,
    Unavailable,
}

public enum WeatherGranularity
{
    Hourly,
    Daily,
}

public enum WeatherVariable
{
    Temperature2m,
    Precipitation,
    PrecipitationProbability,
    WindSpeed10m,
    RelativeHumidity2m,
    WindGusts10m,
    ReferenceEvapotranspiration,
}

public enum ProviderErrorCode
{
    Timeout,
    RateLimited,
    ProviderError,
    SchemaInvalid,
    RunMissing,
    Unavailable,
    PayloadTooLarge,
    OutOfOrder,
}

public readonly record struct GeoPoint
{
    public GeoPoint(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be finite and between -90 and 90.");
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be finite and between -180 and 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }
}

// GeoJSON/PostGIS coordinate order. CAP supplies latitude,longitude and is converted at the boundary.
public readonly record struct GeoPosition
{
    public GeoPosition(double longitude, double latitude)
    {
        var point = new GeoPoint(latitude, longitude);
        Longitude = point.Longitude;
        Latitude = point.Latitude;
    }

    public double Longitude { get; }

    public double Latitude { get; }
}

public sealed record ProviderAttribution(string Label, Uri Url, string Licence);

public sealed record ProviderError(ProviderErrorCode Code, string SafeMessage, int? RetryAfterSeconds = null);

public sealed record WeatherSnapshot(
    Guid SnapshotId,
    string Provider,
    string Model,
    string? RunId,
    GeoPoint RequestedPoint,
    GeoPoint? ResolvedGridPoint,
    double? SpatialResolutionMetres,
    DateTimeOffset? IssuedAt,
    DateTimeOffset RetrievedAt,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    WeatherGranularity Granularity,
    WeatherVariable Variable,
    WeatherNature Nature,
    WeatherAvailability Availability,
    double? Confidence,
    string? ConfidenceBasis,
    double? Value,
    string? Unit,
    ProviderError? Error,
    string? Limitation,
    ProviderAttribution Attribution)
{
    public const string ContractVersion = "1.0";

    public WeatherSnapshot Validate()
    {
        if (ValidTo < ValidFrom)
        {
            throw new ArgumentException("ValidTo must not precede ValidFrom.");
        }

        if (SpatialResolutionMetres is not null and <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SpatialResolutionMetres));
        }

        if (Confidence is not null and (< 0 or > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(Confidence));
        }

        if ((Confidence is null) != (ConfidenceBasis is null))
        {
            throw new ArgumentException("Confidence and its basis must be supplied together.");
        }

        if (Availability == WeatherAvailability.Unavailable)
        {
            if (Value is not null || Unit is not null || Error is null)
            {
                throw new ArgumentException("Unavailable snapshots require an error and cannot carry a value or unit.");
            }
        }
        else if (Value is null || string.IsNullOrWhiteSpace(Unit) || Error is not null)
        {
            throw new ArgumentException("Available snapshots require a value and unit and cannot carry an error.");
        }

        return this;
    }
}

public sealed class ProviderParseResult<T>
{
    internal ProviderParseResult(T? value, ProviderError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public T? Value { get; }

    public ProviderError? Error { get; }

}

public static class ProviderParseResult
{
    public static ProviderParseResult<T> Success<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, null);
    }

    public static ProviderParseResult<T> Failure<T>(ProviderError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(default, error);
    }
}

internal static class ImmutableLists
{
    public static IReadOnlyList<T> Create<T>(IEnumerable<T> values) =>
        Array.AsReadOnly(values.ToArray());
}

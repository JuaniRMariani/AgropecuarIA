namespace AgropecuarIA.Weather.Domain;

public static class WeatherFreshnessStatuses
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unavailable = "unavailable";

    public static bool IsValid(string status) =>
        status is Fresh or Stale or Unavailable;
}

public static class WeatherObservedRainMethods
{
    public const string ManualPluviometer = "manual_pluviometer";
    public const string WeatherStation = "weather_station";
    public const string Estimated = "estimated";

    public static bool IsValid(string method) =>
        method is ManualPluviometer or WeatherStation or Estimated;
}

public sealed class WeatherForecastSnapshot
{
    private WeatherForecastSnapshot() { }

    public WeatherForecastSnapshot(
        Guid id,
        double centroidLatitude,
        double centroidLongitude,
        string provider,
        string modelName,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset validUntilUtc,
        string hourlyVariablesJson,
        string dailyVariablesJson,
        string snapshotHash,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (centroidLatitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(centroidLatitude), "Latitude must be between -90 and 90.");

        if (centroidLongitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(centroidLongitude), "Longitude must be between -180 and 180.");

        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hourlyVariablesJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(dailyVariablesJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotHash);

        Id = id;
        CentroidLatitude = Math.Round(centroidLatitude, 4);
        CentroidLongitude = Math.Round(centroidLongitude, 4);
        Provider = provider.Trim();
        ModelName = modelName.Trim();
        IssuedAtUtc = issuedAtUtc;
        ValidUntilUtc = validUntilUtc;
        HourlyVariablesJson = hourlyVariablesJson;
        DailyVariablesJson = dailyVariablesJson;
        SnapshotHash = snapshotHash.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public double CentroidLatitude { get; private set; }
    public double CentroidLongitude { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ValidUntilUtc { get; private set; }
    public string HourlyVariablesJson { get; private set; } = "{}";
    public string DailyVariablesJson { get; private set; } = "{}";
    public string SnapshotHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsFresh(DateTimeOffset nowUtc) =>
        nowUtc <= ValidUntilUtc;
}

public sealed class WeatherObservedRain
{
    private WeatherObservedRain() { }

    public WeatherObservedRain(
        Guid id,
        Guid organizationId,
        Guid fieldId,
        DateTimeOffset observedDateUtc,
        decimal amountMillimeters,
        string method,
        string? notes,
        Guid recordedByUserId,
        DateTimeOffset recordedAtUtc,
        Guid? rectifiedFromId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (fieldId == Guid.Empty)
            throw new ArgumentException("FieldId is required.", nameof(fieldId));
        if (recordedByUserId == Guid.Empty)
            throw new ArgumentException("RecordedByUserId is required.", nameof(recordedByUserId));

        if (amountMillimeters < 0)
            throw new ArgumentOutOfRangeException(nameof(amountMillimeters), "Rain amount cannot be negative.");

        string effectiveMethod = string.IsNullOrWhiteSpace(method) ? WeatherObservedRainMethods.ManualPluviometer : method.Trim().ToLowerInvariant();
        if (!WeatherObservedRainMethods.IsValid(effectiveMethod))
            throw new ArgumentException($"Invalid observation method: {method}", nameof(method));

        Id = id;
        OrganizationId = organizationId;
        FieldId = fieldId;
        ObservedDateUtc = observedDateUtc;
        AmountMillimeters = amountMillimeters;
        Method = effectiveMethod;
        Notes = notes?.Trim();
        RecordedByUserId = recordedByUserId;
        RecordedAtUtc = recordedAtUtc;
        RectifiedFromId = rectifiedFromId;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FieldId { get; private set; }
    public DateTimeOffset ObservedDateUtc { get; private set; }
    public decimal AmountMillimeters { get; private set; }
    public string Method { get; private set; } = WeatherObservedRainMethods.ManualPluviometer;
    public string? Notes { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public Guid? RectifiedFromId { get; private set; }
}

namespace AgropecuarIA.Weather.Domain;

public static class WeatherAlertSeverities
{
    public const string Minor = "minor";
    public const string Yellow = "yellow";
    public const string Orange = "orange";
    public const string Red = "red";

    public static bool IsValid(string severity) =>
        severity is Minor or Yellow or Orange or Red;
}

public static class WeatherAlertStatuses
{
    public const string Actual = "actual";
    public const string Update = "update";
    public const string Cancel = "cancel";

    public static bool IsValid(string status) =>
        status is Actual or Update or Cancel;
}

public sealed class WeatherAlert
{
    private WeatherAlert() { }

    public WeatherAlert(
        Guid id,
        string identifier,
        string sender,
        DateTimeOffset sentUtc,
        string status,
        string eventName,
        string severity,
        string certainty,
        string headline,
        string description,
        string? instruction,
        string areaDescription,
        string polygonCoordinatesJson,
        double minLatitude,
        double maxLatitude,
        double minLongitude,
        double maxLongitude,
        DateTimeOffset effectiveUtc,
        DateTimeOffset expiresUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(sender);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(headline);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(areaDescription);

        string effectiveSeverity = string.IsNullOrWhiteSpace(severity) ? WeatherAlertSeverities.Yellow : severity.Trim().ToLowerInvariant();
        if (!WeatherAlertSeverities.IsValid(effectiveSeverity))
            throw new ArgumentException($"Invalid alert severity: {severity}", nameof(severity));

        string effectiveStatus = string.IsNullOrWhiteSpace(status) ? WeatherAlertStatuses.Actual : status.Trim().ToLowerInvariant();
        if (!WeatherAlertStatuses.IsValid(effectiveStatus))
            throw new ArgumentException($"Invalid alert status: {status}", nameof(status));

        if (expiresUtc < effectiveUtc)
            throw new ArgumentException("Expiration date cannot be earlier than effective date.", nameof(expiresUtc));

        Id = id;
        Identifier = identifier.Trim();
        Sender = sender.Trim();
        SentUtc = sentUtc;
        Status = effectiveStatus;
        EventName = eventName.Trim();
        Severity = effectiveSeverity;
        Certainty = string.IsNullOrWhiteSpace(certainty) ? "Likely" : certainty.Trim();
        Headline = headline.Trim();
        Description = description.Trim();
        Instruction = instruction?.Trim();
        AreaDescription = areaDescription.Trim();
        PolygonCoordinatesJson = string.IsNullOrWhiteSpace(polygonCoordinatesJson) ? "[]" : polygonCoordinatesJson.Trim();
        MinLatitude = minLatitude;
        MaxLatitude = maxLatitude;
        MinLongitude = minLongitude;
        MaxLongitude = maxLongitude;
        EffectiveUtc = effectiveUtc;
        ExpiresUtc = expiresUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Identifier { get; private set; } = string.Empty;
    public string Sender { get; private set; } = string.Empty;
    public DateTimeOffset SentUtc { get; private set; }
    public string Status { get; private set; } = WeatherAlertStatuses.Actual;
    public string EventName { get; private set; } = string.Empty;
    public string Severity { get; private set; } = WeatherAlertSeverities.Yellow;
    public string Certainty { get; private set; } = "Likely";
    public string Headline { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Instruction { get; private set; }
    public string AreaDescription { get; private set; } = string.Empty;
    public string PolygonCoordinatesJson { get; private set; } = "[]";
    public double MinLatitude { get; private set; }
    public double MaxLatitude { get; private set; }
    public double MinLongitude { get; private set; }
    public double MaxLongitude { get; private set; }
    public DateTimeOffset EffectiveUtc { get; private set; }
    public DateTimeOffset ExpiresUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsActive(DateTimeOffset nowUtc) =>
        Status != WeatherAlertStatuses.Cancel &&
        nowUtc >= EffectiveUtc &&
        nowUtc <= ExpiresUtc;

    public bool CoversLocation(double latitude, double longitude) =>
        latitude >= MinLatitude && latitude <= MaxLatitude &&
        longitude >= MinLongitude && longitude <= MaxLongitude;

    public void Cancel()
    {
        Status = WeatherAlertStatuses.Cancel;
    }
}

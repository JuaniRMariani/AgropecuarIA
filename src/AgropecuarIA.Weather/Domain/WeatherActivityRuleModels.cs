namespace AgropecuarIA.Weather.Domain;

public static class WeatherActivityTypes
{
    public const string Pulverizacion = "pulverizacion";
    public const string Siembra = "siembra";
    public const string Cosecha = "cosecha";
    public const string Fertilizacion = "fertilizacion";

    public static bool IsValid(string activity) =>
        activity is Pulverizacion or Siembra or Cosecha or Fertilizacion;
}

public static class ActivitySuitabilityStatuses
{
    public const string Optima = "optima";
    public const string Marginal = "marginal";
    public const string NoApta = "no_apta";
}

public sealed class WeatherActivityRule
{
    private WeatherActivityRule() { }

    public WeatherActivityRule(
        Guid id,
        Guid organizationId,
        Guid? fieldId,
        string activityType,
        string ruleName,
        decimal? maxWindSpeedKmh,
        decimal? minTemperatureCelsius,
        decimal? maxTemperatureCelsius,
        decimal? maxPrecipitationProbability,
        decimal? maxPrecipitationMm,
        decimal? minRelativeHumidity,
        decimal? maxRelativeHumidity,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(activityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);

        string effectiveActivity = activityType.Trim().ToLowerInvariant();
        if (!WeatherActivityTypes.IsValid(effectiveActivity))
            throw new ArgumentException($"Invalid activity type: {activityType}", nameof(activityType));

        Id = id;
        OrganizationId = organizationId;
        FieldId = fieldId;
        ActivityType = effectiveActivity;
        RuleName = ruleName.Trim();
        MaxWindSpeedKmh = maxWindSpeedKmh;
        MinTemperatureCelsius = minTemperatureCelsius;
        MaxTemperatureCelsius = maxTemperatureCelsius;
        MaxPrecipitationProbability = maxPrecipitationProbability;
        MaxPrecipitationMm = maxPrecipitationMm;
        MinRelativeHumidity = minRelativeHumidity;
        MaxRelativeHumidity = maxRelativeHumidity;
        IsEnabled = true;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? FieldId { get; private set; }
    public string ActivityType { get; private set; } = string.Empty;
    public string RuleName { get; private set; } = string.Empty;
    public decimal? MaxWindSpeedKmh { get; private set; }
    public decimal? MinTemperatureCelsius { get; private set; }
    public decimal? MaxTemperatureCelsius { get; private set; }
    public decimal? MaxPrecipitationProbability { get; private set; }
    public decimal? MaxPrecipitationMm { get; private set; }
    public decimal? MinRelativeHumidity { get; private set; }
    public decimal? MaxRelativeHumidity { get; private set; }
    public bool IsEnabled { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    public ActivitySuitabilityResult Evaluate(
        decimal windSpeedKmh,
        decimal temperatureCelsius,
        decimal precipitationProbability,
        decimal precipitationMm,
        decimal relativeHumidity)
    {
        List<string> riskFactors = [];

        if (MaxWindSpeedKmh.HasValue && windSpeedKmh > MaxWindSpeedKmh.Value)
        {
            riskFactors.Add($"Viento de {windSpeedKmh:F1} km/h excede el límite de {MaxWindSpeedKmh.Value:F1} km/h.");
        }

        if (MinTemperatureCelsius.HasValue && temperatureCelsius < MinTemperatureCelsius.Value)
        {
            riskFactors.Add($"Temperatura de {temperatureCelsius:F1}°C inferior al mínimo de {MinTemperatureCelsius.Value:F1}°C (riesgo de helada/inactividad).");
        }

        if (MaxTemperatureCelsius.HasValue && temperatureCelsius > MaxTemperatureCelsius.Value)
        {
            riskFactors.Add($"Temperatura de {temperatureCelsius:F1}°C excede el límite de {MaxTemperatureCelsius.Value:F1}°C (riesgo de evaporación/estrés térmico).");
        }

        if (MaxPrecipitationProbability.HasValue && precipitationProbability > MaxPrecipitationProbability.Value)
        {
            riskFactors.Add($"Probabilidad de precipitación del {precipitationProbability:F0}% excede el umbral de {MaxPrecipitationProbability.Value:F0}%.");
        }

        if (MaxPrecipitationMm.HasValue && precipitationMm > MaxPrecipitationMm.Value)
        {
            riskFactors.Add($"Precipitación pronosticada de {precipitationMm:F1} mm excede el límite de {MaxPrecipitationMm.Value:F1} mm.");
        }

        if (MinRelativeHumidity.HasValue && relativeHumidity < MinRelativeHumidity.Value)
        {
            riskFactors.Add($"Humedad relativa del {relativeHumidity:F0}% es inferior al mínimo de {MinRelativeHumidity.Value:F0}% (riesgo de deriva por evaporación).");
        }

        if (MaxRelativeHumidity.HasValue && relativeHumidity > MaxRelativeHumidity.Value)
        {
            riskFactors.Add($"Humedad relativa del {relativeHumidity:F0}% excede el máximo de {MaxRelativeHumidity.Value:F0}%.");
        }

        string status = riskFactors.Count switch
        {
            0 => ActivitySuitabilityStatuses.Optima,
            1 => ActivitySuitabilityStatuses.Marginal,
            _ => ActivitySuitabilityStatuses.NoApta
        };

        return new ActivitySuitabilityResult(
            ActivityType,
            RuleName,
            status,
            riskFactors.Count == 0,
            riskFactors);
    }
}

public sealed record ActivitySuitabilityResult(
    string ActivityType,
    string RuleName,
    string Status,
    bool IsSuitable,
    IReadOnlyList<string> RiskFactors);

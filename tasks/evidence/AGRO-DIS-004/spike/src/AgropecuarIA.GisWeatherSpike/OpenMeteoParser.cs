using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace AgropecuarIA.GisWeatherSpike;

public sealed record OpenMeteoParseRequest(
    Stream Payload,
    int StatusCode,
    IReadOnlyList<GeoPoint> RequestedPoints,
    string Model,
    DateTimeOffset RetrievedAt,
    DateTimeOffset Now,
    int? RetryAfterSeconds = null);

public sealed class OpenMeteoParser
{
    private const int MaximumPayloadBytes = 4 * 1024 * 1024;
    private static readonly ProviderAttribution Attribution = new(
        "Open-Meteo",
        new Uri("https://open-meteo.com/"),
        "CC BY 4.0 for weather data; commercial plan terms apply in production");

    private static readonly Dictionary<string, VariableDefinition> Variables =
        new Dictionary<string, VariableDefinition>(StringComparer.Ordinal)
        {
            ["temperature_2m"] = new(WeatherVariable.Temperature2m, "°C", null, null),
            ["precipitation"] = new(WeatherVariable.Precipitation, "mm", 0, null),
            ["precipitation_probability"] = new(WeatherVariable.PrecipitationProbability, "%", 0, 100),
            ["wind_speed_10m"] = new(WeatherVariable.WindSpeed10m, "km/h", 0, null),
            ["relative_humidity_2m"] = new(WeatherVariable.RelativeHumidity2m, "%", 0, 100),
            ["wind_gusts_10m"] = new(WeatherVariable.WindGusts10m, "km/h", 0, null),
            ["et0_fao_evapotranspiration"] = new(WeatherVariable.ReferenceEvapotranspiration, "mm", 0, null),
        };

    private readonly IFreshnessPolicy _freshnessPolicy;

    public OpenMeteoParser(IFreshnessPolicy freshnessPolicy)
    {
        _freshnessPolicy = freshnessPolicy ?? throw new ArgumentNullException(nameof(freshnessPolicy));
    }

    public ProviderParseResult<IReadOnlyList<WeatherSnapshot>> Parse(OpenMeteoParseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Payload);

        using var activity = WeatherDiagnostics.ActivitySource.StartActivity("weather.open_meteo.parse", ActivityKind.Internal);
        activity?.SetTag("weather.provider", "open-meteo");
        var started = Stopwatch.GetTimestamp();

        try
        {
            var transportError = MapTransportError(request.StatusCode, request.RetryAfterSeconds);
            if (transportError is not null)
            {
                return Fail<IReadOnlyList<WeatherSnapshot>>(transportError, activity);
            }

            if (request.RequestedPoints.Count == 0 || string.IsNullOrWhiteSpace(request.Model))
            {
                return Fail<IReadOnlyList<WeatherSnapshot>>(
                    SchemaError("Requested points and model are required."), activity);
            }

            using var document = ReadJson(request.Payload, MaximumPayloadBytes);
            var responses = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToArray()
                : [document.RootElement];

            if (responses.Length != request.RequestedPoints.Count)
            {
                return Fail<IReadOnlyList<WeatherSnapshot>>(
                    SchemaError("Response count does not match requested point count."), activity);
            }

            var snapshots = new List<WeatherSnapshot>(responses.Length * Variables.Count);
            for (var index = 0; index < responses.Length; index++)
            {
                var error = ParsePoint(
                    responses[index],
                    request.RequestedPoints[index],
                    request.Model,
                    request.RetrievedAt,
                    request.Now,
                    snapshots);

                if (error is not null)
                {
                    return Fail<IReadOnlyList<WeatherSnapshot>>(error, activity);
                }
            }

            WeatherDiagnostics.ParsedValues.Add(
                snapshots.Count,
                new KeyValuePair<string, object?>("weather.provider", "open-meteo"));
            activity?.SetTag("weather.values.count", snapshots.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return ProviderParseResult.Success<IReadOnlyList<WeatherSnapshot>>(ImmutableLists.Create(snapshots));
        }
        catch (JsonException exception)
        {
            return Fail<IReadOnlyList<WeatherSnapshot>>(
                SchemaError($"JSON payload is invalid at byte {exception.BytePositionInLine}."), activity);
        }
        catch (PayloadLimitExceededException)
        {
            return Fail<IReadOnlyList<WeatherSnapshot>>(
                new ProviderError(ProviderErrorCode.PayloadTooLarge, "Provider payload exceeded the 4 MiB safety limit."), activity);
        }
        finally
        {
            WeatherDiagnostics.ParseDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("weather.provider", "open-meteo"));
        }
    }

    private ProviderError? ParsePoint(
        JsonElement response,
        GeoPoint requestedPoint,
        string model,
        DateTimeOffset retrievedAt,
        DateTimeOffset now,
        List<WeatherSnapshot> snapshots)
    {
        if (response.ValueKind != JsonValueKind.Object ||
            !TryGetFiniteDouble(response, "latitude", out var latitude) ||
            !TryGetFiniteDouble(response, "longitude", out var longitude) ||
            !TryGetInt32(response, "utc_offset_seconds", out var utcOffsetSeconds) ||
            !response.TryGetProperty("hourly_units", out var units) || units.ValueKind != JsonValueKind.Object ||
            !response.TryGetProperty("hourly", out var hourly) || hourly.ValueKind != JsonValueKind.Object)
        {
            return SchemaError("Required Open-Meteo point metadata is absent or has the wrong type.");
        }

        if (utcOffsetSeconds is < -64800 or > 64800)
        {
            return SchemaError("utc_offset_seconds is outside the supported range.");
        }

        GeoPoint resolvedPoint;
        try
        {
            resolvedPoint = new GeoPoint(latitude, longitude);
        }
        catch (ArgumentOutOfRangeException)
        {
            return SchemaError("Resolved coordinates are outside valid latitude/longitude ranges.");
        }

        if (!hourly.TryGetProperty("time", out var timesElement) || timesElement.ValueKind != JsonValueKind.Array)
        {
            return SchemaError("hourly.time must be an array.");
        }

        var times = timesElement.EnumerateArray().ToArray();
        if (times.Length == 0)
        {
            return SchemaError("hourly.time cannot be empty.");
        }

        var offset = TimeSpan.FromSeconds(utcOffsetSeconds);
        var parsedTimes = new DateTimeOffset[times.Length];
        for (var index = 0; index < times.Length; index++)
        {
            if (times[index].ValueKind != JsonValueKind.String ||
                !TryParseProviderTime(times[index].GetString(), offset, out parsedTimes[index]))
            {
                return SchemaError("hourly.time contains an invalid ISO local timestamp.");
            }

            if (index > 0 && parsedTimes[index] <= parsedTimes[index - 1])
            {
                return SchemaError("hourly.time must be strictly increasing.");
            }
        }

        var interval = parsedTimes.Length > 1
            ? parsedTimes[1] - parsedTimes[0]
            : TimeSpan.FromHours(1);
        if (interval <= TimeSpan.Zero || interval > TimeSpan.FromHours(24))
        {
            return SchemaError("The time interval is outside supported hourly/daily bounds.");
        }

        foreach (var (providerName, definition) in Variables)
        {
            if (!units.TryGetProperty(providerName, out var unitElement) ||
                unitElement.ValueKind != JsonValueKind.String ||
                !string.Equals(unitElement.GetString(), definition.Unit, StringComparison.Ordinal))
            {
                return SchemaError($"Unit for {providerName} is absent or unsupported.");
            }

            if (!hourly.TryGetProperty(providerName, out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
            {
                return SchemaError($"hourly.{providerName} must be an array.");
            }

            var values = valuesElement.EnumerateArray().ToArray();
            if (values.Length != parsedTimes.Length)
            {
                return SchemaError($"hourly.{providerName} length differs from hourly.time.");
            }

            for (var index = 0; index < values.Length; index++)
            {
                ProviderError? valueError = null;
                double? value = null;
                if (values[index].ValueKind == JsonValueKind.Null)
                {
                    valueError = new ProviderError(ProviderErrorCode.Unavailable, $"{providerName} is unavailable for this interval.");
                }
                else if (values[index].ValueKind != JsonValueKind.Number ||
                         !values[index].TryGetDouble(out var parsedValue) ||
                         !double.IsFinite(parsedValue) ||
                         (definition.Minimum is not null && parsedValue < definition.Minimum) ||
                         (definition.Maximum is not null && parsedValue > definition.Maximum))
                {
                    return SchemaError($"hourly.{providerName} contains an invalid value.");
                }
                else
                {
                    value = parsedValue;
                }

                var validFrom = parsedTimes[index];
                var validTo = validFrom + interval;
                var availability = _freshnessPolicy.Classify(new FreshnessContext(
                    now,
                    retrievedAt,
                    validTo,
                    value is not null,
                    valueError));
                if (availability == WeatherAvailability.Unavailable && valueError is null)
                {
                    valueError = new ProviderError(
                        ProviderErrorCode.Unavailable,
                        $"{providerName} is unavailable under the current freshness policy.");
                }

                snapshots.Add(new WeatherSnapshot(
                    Guid.NewGuid(),
                    "open-meteo",
                    model,
                    null,
                    requestedPoint,
                    resolvedPoint,
                    null,
                    null,
                    retrievedAt,
                    validFrom,
                    validTo,
                    WeatherGranularity.Hourly,
                    definition.Variable,
                    WeatherNature.Forecast,
                    availability,
                    null,
                    null,
                    availability == WeatherAvailability.Unavailable ? null : value,
                    availability == WeatherAvailability.Unavailable ? null : definition.Unit,
                    availability == WeatherAvailability.Unavailable ? valueError : null,
                    "Forecast grid value; not an observation at the field.",
                    Attribution).Validate());
            }
        }

        return null;
    }

    private static JsonDocument ReadJson(Stream payload, int maximumBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        var total = 0;
        while (true)
        {
            var read = payload.Read(chunk, 0, chunk.Length);
            if (read == 0)
            {
                break;
            }

            total = checked(total + read);
            if (total > maximumBytes)
            {
                throw new PayloadLimitExceededException();
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        return JsonDocument.Parse(buffer, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 32,
        });
    }

    private static ProviderError? MapTransportError(int statusCode, int? retryAfterSeconds) => statusCode switch
    {
        200 => null,
        408 or 504 => new ProviderError(ProviderErrorCode.Timeout, "Open-Meteo timed out."),
        429 => new ProviderError(ProviderErrorCode.RateLimited, "Open-Meteo rate limit was reached.", retryAfterSeconds),
        >= 500 and <= 599 => new ProviderError(ProviderErrorCode.ProviderError, "Open-Meteo returned a server error."),
        _ => new ProviderError(ProviderErrorCode.ProviderError, $"Open-Meteo returned HTTP {statusCode}."),
    };

    private static ProviderParseResult<T> Fail<T>(ProviderError error, Activity? activity)
    {
        WeatherDiagnostics.ParseErrors.Add(
            1,
            new KeyValuePair<string, object?>("weather.provider", "open-meteo"),
            new KeyValuePair<string, object?>("error.type", error.Code.ToString()));
        activity?.SetTag("error.type", error.Code.ToString());
        activity?.SetStatus(ActivityStatusCode.Error, error.SafeMessage);
        return ProviderParseResult.Failure<T>(error);
    }

    private static ProviderError SchemaError(string message) =>
        new(ProviderErrorCode.SchemaInvalid, message);

    private static bool TryGetFiniteDouble(JsonElement element, string propertyName, out double value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetDouble(out value) &&
               double.IsFinite(value);
    }

    private static bool TryGetInt32(JsonElement element, string propertyName, out int value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value);
    }

    private static bool TryParseProviderTime(string? value, TimeSpan offset, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (!DateTime.TryParseExact(
                value,
                ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var local))
        {
            return false;
        }

        timestamp = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset);
        return true;
    }

    private sealed record VariableDefinition(
        WeatherVariable Variable,
        string Unit,
        double? Minimum,
        double? Maximum);

    private sealed class PayloadLimitExceededException : Exception;
}

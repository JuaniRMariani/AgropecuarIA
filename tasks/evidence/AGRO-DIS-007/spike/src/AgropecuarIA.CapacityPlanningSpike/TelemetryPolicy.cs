using System.Collections.ObjectModel;

namespace AgropecuarIA.CapacityPlanningSpike;

public static class TelemetryPolicy
{
    private static readonly ReadOnlyDictionary<string, HashSet<string>> AllowedAttributeValues =
        new ReadOnlyDictionary<string, HashSet<string>>(
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
            {
                ["route_template"] = Values("/capacity/{scenarioId}"),
                ["http.request.method"] = Values("CONNECT", "DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT", "TRACE"),
                ["status_class"] = Values("1xx", "2xx", "3xx", "4xx", "5xx", "network_error"),
                ["dependency"] = Values("identity-provider", "object-storage", "postgresql", "weather-provider"),
                ["job"] = Values("capacity-report", "import", "malware-scan", "weather-refresh"),
                ["cache"] = Values("bypass", "error", "hit", "miss", "stale"),
                ["deployment.environment"] = Values("local", "test", "staging", "production"),
                ["result"] = Values("success", "degraded", "rejected", "failure"),
            });

    private static readonly HashSet<string> AllowedAttributeNames = new(StringComparer.Ordinal)
    {
        "route_template",
        "http.request.method",
        "status_class",
        "dependency",
        "job",
        "cache",
        "deployment.environment",
        "result",
    };

    public static IReadOnlyDictionary<string, string> Sanitize(
        IEnumerable<KeyValuePair<string, string>> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        var sanitized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attribute in attributes)
        {
            if (!AllowedAttributeNames.Contains(attribute.Key))
            {
                continue;
            }

            ValidateAllowedValue(attribute);
            sanitized[attribute.Key] = attribute.Value;
        }

        return new ReadOnlyDictionary<string, string>(sanitized);
    }

    private static void ValidateAllowedValue(KeyValuePair<string, string> attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.Value) || attribute.Value.Length > 128)
        {
            throw new CapacityPlanningException(
                CapacityPlanningErrorCode.InvalidInput,
                nameof(attribute),
                $"Telemetry attribute '{attribute.Key}' must contain a bounded non-empty value.");
        }

        if (AllowedAttributeValues.TryGetValue(attribute.Key, out var values)
            && !values.Contains(attribute.Value))
        {
            throw Invalid(attribute.Key, "The value is outside the closed operational vocabulary.");
        }
    }

    private static HashSet<string> Values(params string[] values) =>
        new(values, StringComparer.Ordinal);

    private static CapacityPlanningException Invalid(string attributeName, string message) =>
        new(
            CapacityPlanningErrorCode.InvalidInput,
            nameof(attributeName),
            $"Telemetry attribute '{attributeName}' is invalid. {message}");
}

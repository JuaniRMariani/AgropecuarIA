using System.Text.Json;

namespace AgropecuarIA.GisWeatherSpike;

public sealed record WrfValidationLimits(
    long MaximumFileBytes,
    long MaximumGridCells,
    int MaximumTimeSteps)
{
    public static WrfValidationLimits SpikeDefaults { get; } = new(
        MaximumFileBytes: 25L * 1024 * 1024,
        MaximumGridCells: 2_000_000,
        MaximumTimeSteps: 73);
}

public sealed record WrfMetadata(
    string Format,
    long FileBytes,
    int TimeSteps,
    int Y,
    int X,
    IReadOnlyList<string> Variables);

public sealed class WrfMetadataValidator
{
    // Names observed in the official SMN sample WRFDETAR_01H_20220101_00_000.nc.
    private static readonly string[] RequiredVariables = ["PP", "T2", "HR2", "dirViento10", "magViento10", "lat", "lon", "time"];
    private readonly WrfValidationLimits _limits;

    public WrfMetadataValidator(WrfValidationLimits limits)
    {
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        if (limits.MaximumFileBytes <= 0 || limits.MaximumGridCells <= 0 || limits.MaximumTimeSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limits));
        }
    }

    public ProviderParseResult<WrfMetadata> Validate(Stream payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            using var document = JsonDocument.Parse(payload, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 16,
            });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetString(root, "dataModel", out var format) || format != "NETCDF4" ||
                !TryGetPositiveInt64(root, "fileBytes", out var fileBytes) ||
                !root.TryGetProperty("dimensions", out var dimensions) || dimensions.ValueKind != JsonValueKind.Object ||
                !TryGetPositiveInt32(dimensions, "time", out var time) ||
                !TryGetPositiveInt32(dimensions, "y", out var y) ||
                !TryGetPositiveInt32(dimensions, "x", out var x) ||
                !root.TryGetProperty("variables", out var variablesElement) || variablesElement.ValueKind != JsonValueKind.Object)
            {
                return SchemaFailure("WRF metadata is missing required NetCDF fields.");
            }

            var variables = variablesElement.EnumerateObject()
                .Select(variable => variable.Name)
                .ToArray();
            if (variables.Any(string.IsNullOrWhiteSpace) || variables.Distinct(StringComparer.Ordinal).Count() != variables.Length)
            {
                return SchemaFailure("WRF variable names must be non-empty and unique.");
            }

            var missing = RequiredVariables.Except(variables, StringComparer.Ordinal).ToArray();
            if (missing.Length > 0)
            {
                return ProviderParseResult.Failure<WrfMetadata>(new ProviderError(
                    ProviderErrorCode.RunMissing,
                    $"WRF metadata is missing required variables: {string.Join(", ", missing)}."));
            }

            long gridCells;
            try
            {
                gridCells = checked((long)y * x);
            }
            catch (OverflowException)
            {
                return LimitFailure("WRF grid dimensions overflow the supported range.");
            }

            if (fileBytes > _limits.MaximumFileBytes || gridCells > _limits.MaximumGridCells || time > _limits.MaximumTimeSteps)
            {
                return LimitFailure("WRF metadata exceeds configured file, grid, or time-step safety limits.");
            }

            foreach (var name in RequiredVariables)
            {
                var expectedDimensions = name switch
                {
                    "lat" or "lon" => new[] { "y", "x" },
                    "time" => new[] { "time" },
                    _ => new[] { "time", "y", "x" },
                };
                var expectedShape = name switch
                {
                    "lat" or "lon" => new[] { y, x },
                    "time" => new[] { time },
                    _ => new[] { time, y, x },
                };
                if (!HasExpectedArray(variablesElement.GetProperty(name), "dimensions", expectedDimensions) ||
                    !HasExpectedArray(variablesElement.GetProperty(name), "shape", expectedShape))
                {
                    return SchemaFailure($"WRF variable {name} dimensions or shape do not match the guarded grid.");
                }
            }

            return ProviderParseResult.Success(new WrfMetadata(
                format,
                fileBytes,
                time,
                y,
                x,
                ImmutableLists.Create(variables)));
        }
        catch (JsonException exception)
        {
            return SchemaFailure($"WRF metadata JSON is truncated or invalid at byte {exception.BytePositionInLine}.");
        }
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return value.Length > 0;
    }

    private static bool TryGetPositiveInt32(JsonElement element, string propertyName, out int value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value) && value > 0;
    }

    private static bool TryGetPositiveInt64(JsonElement element, string propertyName, out long value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt64(out value) && value > 0;
    }

    private static bool HasExpectedArray<T>(JsonElement element, string propertyName, IReadOnlyList<T> expected)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var actual = property.EnumerateArray().ToArray();
        if (actual.Length != expected.Count)
        {
            return false;
        }

        for (var index = 0; index < actual.Length; index++)
        {
            if (typeof(T) == typeof(string))
            {
                if (actual[index].ValueKind != JsonValueKind.String ||
                    !string.Equals(actual[index].GetString(), expected[index]?.ToString(), StringComparison.Ordinal))
                {
                    return false;
                }
            }
            else if (actual[index].ValueKind != JsonValueKind.Number ||
                     !actual[index].TryGetInt32(out var value) ||
                     value != Convert.ToInt32(expected[index], System.Globalization.CultureInfo.InvariantCulture))
            {
                return false;
            }
        }

        return true;
    }

    private static ProviderParseResult<WrfMetadata> SchemaFailure(string message) =>
        ProviderParseResult.Failure<WrfMetadata>(new ProviderError(ProviderErrorCode.SchemaInvalid, message));

    private static ProviderParseResult<WrfMetadata> LimitFailure(string message) =>
        ProviderParseResult.Failure<WrfMetadata>(new ProviderError(ProviderErrorCode.PayloadTooLarge, message));
}

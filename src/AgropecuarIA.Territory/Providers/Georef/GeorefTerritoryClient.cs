using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Domain;

namespace AgropecuarIA.Territory.Providers.Georef;

public sealed class GeorefTerritoryClient(
    HttpClient httpClient,
    TimeProvider timeProvider) : ITerritoryCoordinateProvider
{
    public static readonly Uri ServiceBaseAddress = new(
        "https://apis.datos.gob.ar/georef/api/v2.0/",
        UriKind.Absolute);

    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    public const int MaximumResponseBytes = 256 * 1024;

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 12,
    };

    public async Task<ProviderTerritoryResolution?> ResolveAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        EnsureFixedBaseAddress();
        string relativeUri = string.Create(
            CultureInfo.InvariantCulture,
            $"ubicacion?lat={latitude:R}&lon={longitude:R}");
        using HttpRequestMessage request = new(HttpMethod.Get, relativeUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new TerritoryProviderException(
                    $"Georef returned HTTP {(int)response.StatusCode}.");
            }

            if (response.Content.Headers.ContentType?.MediaType is not string mediaType ||
                !mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                throw new TerritoryProviderException("Georef returned a non-JSON response.");
            }

            if (response.Content.Headers.ContentLength is long length &&
                length > MaximumResponseBytes)
            {
                throw new TerritoryProviderException("Georef response exceeded the size limit.");
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            byte[] payload = await ReadBoundedAsync(stream, cancellationToken);
            return Parse(payload, timeProvider.GetUtcNow(), latitude, longitude);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TerritoryProviderException("Georef request timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new TerritoryProviderException("Georef request failed.", exception);
        }
        catch (IOException exception)
        {
            throw new TerritoryProviderException("Georef response stream failed.", exception);
        }
        catch (JsonException exception)
        {
            throw new TerritoryProviderException("Georef returned invalid JSON.", exception);
        }
    }

    private static ProviderTerritoryResolution? Parse(
        ReadOnlyMemory<byte> payload,
        DateTimeOffset capturedAtUtc,
        double requestedLatitude,
        double requestedLongitude)
    {
        using JsonDocument document = JsonDocument.Parse(payload, JsonOptions);
        JsonElement root = document.RootElement;
        RequireObject(root, "root");
        RequireOnlyProperties(root, "ubicacion", "parametros");
        if (!root.TryGetProperty("parametros", out JsonElement parameters))
        {
            throw new JsonException("Georef field 'parametros' is required.");
        }
        ValidateCoordinateEcho(parameters, "parametros", requestedLatitude, requestedLongitude);
        if (!root.TryGetProperty("ubicacion", out JsonElement location) ||
            location.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireObject(location, "ubicacion");
        RequireOnlyProperties(location, "provincia", "departamento", "gobierno_local", "lat", "lon");
        ValidateCoordinateEcho(location, "ubicacion", requestedLatitude, requestedLongitude);

        AdministrativeUnit province = ParseUnit(location, "provincia", required: true)!;
        AdministrativeUnit? department = ParseUnit(location, "departamento", required: false);
        AdministrativeUnit? municipality = ParseUnit(location, "gobierno_local", required: false);

        AdministrativeUnit selected = municipality ?? department ?? province;
        AdministrativeUnit? parent = selected.Level switch
        {
            TerritoryLevels.Municipality => department ?? province,
            TerritoryLevels.Department => province,
            _ => null,
        };
        string hierarchyLabel = BuildHierarchyLabel(selected, department, province);

        return new ProviderTerritoryResolution(
            new TerritoryReferenceSource("georef", "2.0", capturedAtUtc),
            new TerritoryReferenceMatch(
                selected.Code,
                selected.Name,
                selected.Level,
                parent?.Code,
                parent?.Name,
                hierarchyLabel));
    }

    private static AdministrativeUnit? ParseUnit(
        JsonElement location,
        string property,
        bool required)
    {
        if (!location.TryGetProperty(property, out JsonElement value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            if (required)
            {
                throw new JsonException($"Georef field '{property}' is required.");
            }

            return null;
        }

        RequireObject(value, property);
        RequireOnlyProperties(value, "id", "nombre");
        string? code = ReadNullableString(value, "id");
        string? name = ReadNullableString(value, "nombre");
        if (code is null && name is null && !required)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) ||
            code.Length is < 2 or > 16 || code.Any(character => !char.IsAsciiDigit(character)) ||
            name.Length > 160)
        {
            throw new JsonException($"Georef field '{property}' is malformed.");
        }

        string level = property switch
        {
            "provincia" => TerritoryLevels.Province,
            "departamento" => TerritoryLevels.Department,
            "gobierno_local" => TerritoryLevels.Municipality,
            _ => throw new JsonException("Unknown Georef administrative level."),
        };
        return new AdministrativeUnit(code, name.Trim(), level);
    }

    private static string? ReadNullableString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : throw new JsonException($"Georef field '{property}' must be a string or null.");
    }

    private static void ValidateCoordinateEcho(
        JsonElement element,
        string field,
        double requestedLatitude,
        double requestedLongitude)
    {
        RequireObject(element, field);
        if (field == "parametros")
        {
            RequireOnlyProperties(element, "lat", "lon");
        }

        double latitude = ReadRequiredCoordinate(element, "lat", -90, 90);
        double longitude = ReadRequiredCoordinate(element, "lon", -180, 180);
        const double tolerance = 0.000001;
        if (Math.Abs(latitude - requestedLatitude) > tolerance ||
            Math.Abs(longitude - requestedLongitude) > tolerance)
        {
            throw new JsonException($"Georef field '{field}' does not match the requested coordinate.");
        }
    }

    private static double ReadRequiredCoordinate(
        JsonElement element,
        string property,
        double minimum,
        double maximum)
    {
        if (!element.TryGetProperty(property, out JsonElement value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDouble(out double coordinate) ||
            !double.IsFinite(coordinate) ||
            coordinate < minimum || coordinate > maximum)
        {
            throw new JsonException($"Georef field '{property}' is not a valid coordinate.");
        }

        return coordinate;
    }

    private static string BuildHierarchyLabel(
        AdministrativeUnit selected,
        AdministrativeUnit? department,
        AdministrativeUnit province)
    {
        List<string> names = [selected.Name];
        if (selected.Level == TerritoryLevels.Municipality &&
            department is not null &&
            !department.Name.Equals(selected.Name, StringComparison.Ordinal))
        {
            names.Add(department.Name);
        }

        if (!province.Name.Equals(names[^1], StringComparison.Ordinal))
        {
            names.Add(province.Name);
        }

        return string.Join(", ", names);
    }

    private static void RequireObject(JsonElement element, string field)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Georef field '{field}' must be an object.");
        }
    }

    private static void RequireOnlyProperties(JsonElement element, params string[] allowed)
    {
        HashSet<string> allowedSet = new(allowed, StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!allowedSet.Contains(property.Name))
            {
                throw new JsonException($"Unexpected Georef field '{property.Name}'.");
            }
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[16 * 1024];
        while (true)
        {
            int read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > MaximumResponseBytes)
            {
                throw new TerritoryProviderException("Georef response exceeded the size limit.");
            }

            buffer.Write(chunk, 0, read);
        }

        if (buffer.Length == 0)
        {
            throw new TerritoryProviderException("Georef returned an empty response.");
        }

        return buffer.ToArray();
    }

    private void EnsureFixedBaseAddress()
    {
        if (httpClient.BaseAddress != ServiceBaseAddress)
        {
            throw new InvalidOperationException("Georef HttpClient must use the fixed official base address.");
        }
    }

    private sealed record AdministrativeUnit(string Code, string Name, string Level);
}

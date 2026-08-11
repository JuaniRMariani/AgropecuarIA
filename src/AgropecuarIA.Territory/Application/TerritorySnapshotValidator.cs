using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AgropecuarIA.Territory.Domain;

namespace AgropecuarIA.Territory.Application;

public sealed record TerritorySnapshotImport(
    Guid SnapshotId,
    string Provider,
    string Version,
    DateTimeOffset CapturedAtUtc,
    string ExpectedContentHash,
    IReadOnlyCollection<TerritoryUnitImport> Units);

public sealed record TerritoryUnitImport(
    string OfficialCode,
    string Name,
    string Level,
    string? ParentCode = null,
    double? CentroidLatitude = null,
    double? CentroidLongitude = null);

public sealed class TerritorySnapshotValidationException(string message) : Exception(message);

public static class TerritorySnapshotValidator
{
    private const char FieldSeparator = '\u001F';

    private static readonly HashSet<string> RequiredProvinceCodes =
    [
        "02", "06", "10", "14", "18", "22", "26", "30", "34", "38", "42", "46",
        "50", "54", "58", "62", "66", "70", "74", "78", "82", "86", "90", "94",
    ];

    public static ValidatedTerritorySnapshot Validate(
        TerritorySnapshotImport import,
        DateTimeOffset importedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(import);
        if (import.SnapshotId == Guid.Empty)
        {
            throw Invalid("Snapshot ID is required.");
        }

        ValidateText(import.Provider, 2, 32, "provider");
        if (!import.Provider.Trim().Equals("georef", StringComparison.Ordinal))
        {
            throw Invalid("TerritoryReference v1 only accepts the georef provider.");
        }
        ValidateText(import.Version, 1, 80, "version");
        if (import.CapturedAtUtc == default || import.CapturedAtUtc > importedAtUtc)
        {
            throw Invalid("Captured time must be present and cannot be in the future.");
        }

        if (import.Units.Count == 0)
        {
            throw Invalid("A snapshot must contain territory units.");
        }

        List<CanonicalTerritoryUnit> canonicalUnits = import.Units
            .Select(ToCanonicalUnit)
            .ToList();
        Dictionary<string, CanonicalTerritoryUnit> byCode = new(StringComparer.Ordinal);
        foreach (CanonicalTerritoryUnit unit in canonicalUnits)
        {
            if (!byCode.TryAdd(unit.OfficialCode, unit))
            {
                throw Invalid($"Duplicate official code '{unit.OfficialCode}'.");
            }
        }

        ValidateNationalCoverage(canonicalUnits);
        ValidateParents(byCode);
        ValidateNoCycles(byCode);

        byte[] contentHash = ComputeContentHash(canonicalUnits);
        byte[] expectedHash = ParseExpectedHash(import.ExpectedContentHash);
        if (!CryptographicOperations.FixedTimeEquals(contentHash, expectedHash))
        {
            throw Invalid("Snapshot content hash does not match its canonical units.");
        }

        OfficialTerritorySnapshot snapshot = new(
            import.SnapshotId,
            import.Provider.Trim(),
            import.Version.Trim(),
            import.CapturedAtUtc,
            contentHash,
            importedAtUtc);
        OfficialTerritoryUnit[] units = canonicalUnits
            .Select(unit => new OfficialTerritoryUnit(
                import.SnapshotId,
                unit.OfficialCode,
                unit.Name,
                unit.NormalizedName,
                unit.Level,
                unit.ParentCode,
                unit.CentroidLatitude,
                unit.CentroidLongitude))
            .ToArray();

        return new ValidatedTerritorySnapshot(snapshot, units);
    }

    public static byte[] ComputeContentHash(IReadOnlyCollection<TerritoryUnitImport> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return ComputeContentHash(units.Select(ToCanonicalUnit));
    }

    private static byte[] ComputeContentHash(IEnumerable<CanonicalTerritoryUnit> units)
    {
        StringBuilder canonical = new();
        foreach (CanonicalTerritoryUnit unit in units.OrderBy(unit => unit.OfficialCode, StringComparer.Ordinal))
        {
            canonical
                .Append(unit.OfficialCode).Append(FieldSeparator)
                .Append(unit.Level).Append(FieldSeparator)
                .Append(unit.Name).Append(FieldSeparator)
                .Append(unit.NormalizedName).Append(FieldSeparator)
                .Append(unit.ParentCode ?? string.Empty).Append(FieldSeparator)
                .Append(FormatCoordinate(unit.CentroidLatitude)).Append(FieldSeparator)
                .Append(FormatCoordinate(unit.CentroidLongitude)).Append('\n');
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
    }

    private static CanonicalTerritoryUnit ToCanonicalUnit(TerritoryUnitImport unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ValidateCode(unit.OfficialCode, "official code");
        ValidateText(unit.Name, 1, 160, "name");
        string level = unit.Level?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!TerritoryLevels.IsSupported(level))
        {
            throw Invalid($"Unsupported territory level '{unit.Level}'.");
        }

        string? parentCode = string.IsNullOrWhiteSpace(unit.ParentCode)
            ? null
            : unit.ParentCode.Trim();
        if (parentCode is not null)
        {
            ValidateCode(parentCode, "parent code");
        }

        if (unit.CentroidLatitude.HasValue != unit.CentroidLongitude.HasValue)
        {
            throw Invalid("Centroid latitude and longitude must be supplied together.");
        }

        if (unit.CentroidLatitude is double latitude &&
            (!double.IsFinite(latitude) || latitude is < -90 or > 90))
        {
            throw Invalid("Centroid latitude is outside WGS84 bounds.");
        }

        if (unit.CentroidLongitude is double longitude &&
            (!double.IsFinite(longitude) || longitude is < -180 or > 180))
        {
            throw Invalid("Centroid longitude is outside WGS84 bounds.");
        }

        string name = unit.Name.Trim().Normalize(NormalizationForm.FormC);
        string normalizedName = TerritoryNameNormalizer.Normalize(name);
        if (normalizedName.Length == 0)
        {
            throw Invalid("Normalized territory name cannot be empty.");
        }

        return new CanonicalTerritoryUnit(
            unit.OfficialCode.Trim(),
            name,
            normalizedName,
            level,
            parentCode,
            unit.CentroidLatitude,
            unit.CentroidLongitude);
    }

    private static void ValidateNationalCoverage(IEnumerable<CanonicalTerritoryUnit> units)
    {
        string[] provinceCodes = units
            .Where(unit => unit.Level == TerritoryLevels.Province)
            .Select(unit => unit.OfficialCode)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (provinceCodes.Length != RequiredProvinceCodes.Count ||
            !RequiredProvinceCodes.SetEquals(provinceCodes))
        {
            throw Invalid("Snapshot must contain exactly Argentina's 24 province/CABA official codes.");
        }
    }

    private static void ValidateParents(
        IReadOnlyDictionary<string, CanonicalTerritoryUnit> byCode)
    {
        foreach (CanonicalTerritoryUnit unit in byCode.Values)
        {
            if (unit.Level == TerritoryLevels.Province)
            {
                if (unit.ParentCode is not null)
                {
                    throw Invalid($"Province '{unit.OfficialCode}' cannot have a parent.");
                }

                continue;
            }

            if (unit.ParentCode is null || !byCode.TryGetValue(unit.ParentCode, out CanonicalTerritoryUnit? parent))
            {
                throw Invalid($"Territory unit '{unit.OfficialCode}' requires an existing parent.");
            }

            if (TerritoryLevels.GetDepth(parent.Level) >= TerritoryLevels.GetDepth(unit.Level))
            {
                throw Invalid($"Territory unit '{unit.OfficialCode}' has an invalid parent level.");
            }
        }
    }

    private static void ValidateNoCycles(
        IReadOnlyDictionary<string, CanonicalTerritoryUnit> byCode)
    {
        foreach (CanonicalTerritoryUnit unit in byCode.Values)
        {
            HashSet<string> path = new(StringComparer.Ordinal);
            CanonicalTerritoryUnit? current = unit;
            while (current is not null)
            {
                if (!path.Add(current.OfficialCode))
                {
                    throw Invalid($"Territory hierarchy contains a cycle at '{current.OfficialCode}'.");
                }

                current = current.ParentCode is not null
                    ? byCode[current.ParentCode]
                    : null;
            }
        }
    }

    private static byte[] ParseExpectedHash(string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(expectedHash) || expectedHash.Trim().Length != 64)
        {
            throw Invalid("Expected content hash must be a 64-character SHA-256 hex value.");
        }

        try
        {
            return Convert.FromHexString(expectedHash.Trim());
        }
        catch (FormatException exception)
        {
            throw new TerritorySnapshotValidationException(
                $"Expected content hash is not valid hexadecimal: {exception.Message}");
        }
    }

    private static string FormatCoordinate(double? value) =>
        value?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty;

    private static void ValidateCode(string? value, string field)
    {
        string code = value?.Trim() ?? string.Empty;
        if (code.Length is < 2 or > 16 || code.Any(character => !char.IsAsciiDigit(character)))
        {
            throw Invalid($"Territory {field} must contain 2 to 16 ASCII digits.");
        }
    }

    private static void ValidateText(string? value, int minLength, int maxLength, string field)
    {
        string text = value?.Trim() ?? string.Empty;
        if (text.Length < minLength ||
            text.Length > maxLength ||
            text.Any(char.IsControl))
        {
            throw Invalid(
                $"Snapshot {field} must contain {minLength} to {maxLength} visible characters.");
        }
    }

    private static TerritorySnapshotValidationException Invalid(string message) => new(message);

    private sealed record CanonicalTerritoryUnit(
        string OfficialCode,
        string Name,
        string NormalizedName,
        string Level,
        string? ParentCode,
        double? CentroidLatitude,
        double? CentroidLongitude);
}

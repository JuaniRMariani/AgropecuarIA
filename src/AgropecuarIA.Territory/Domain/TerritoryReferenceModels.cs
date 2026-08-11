using System.Globalization;
using System.Text;

namespace AgropecuarIA.Territory.Domain;

public static class TerritoryLevels
{
    public const string Province = "province";
    public const string Department = "department";
    public const string Municipality = "municipality";
    public const string Locality = "locality";

    public static bool IsSupported(string value) =>
        value is Province or Department or Municipality or Locality;

    public static int GetDepth(string value) => value switch
    {
        Province => 0,
        Department => 1,
        Municipality => 2,
        Locality => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown territory level."),
    };
}

public static class TerritorySnapshotStatuses
{
    public const string Staging = "staging";
    public const string Active = "active";
    public const string Retired = "retired";
}

public sealed class OfficialTerritorySnapshot
{
    private OfficialTerritorySnapshot()
    {
    }

    public OfficialTerritorySnapshot(
        Guid id,
        string provider,
        string version,
        DateTimeOffset capturedAtUtc,
        byte[] contentHash,
        DateTimeOffset importedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Snapshot ID is required.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(contentHash);
        if (contentHash.Length != 32)
        {
            throw new ArgumentException("Snapshot content hash must be SHA-256.", nameof(contentHash));
        }

        Id = id;
        Provider = provider;
        Version = version;
        CapturedAtUtc = capturedAtUtc;
        ContentHash = contentHash.ToArray();
        Status = TerritorySnapshotStatuses.Staging;
        ImportedAtUtc = importedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public byte[] ContentHash { get; private set; } = [];

    public string Status { get; private set; } = string.Empty;

    public DateTimeOffset ImportedAtUtc { get; private set; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }
}

public sealed class OfficialTerritoryUnit
{
    private OfficialTerritoryUnit()
    {
    }

    public OfficialTerritoryUnit(
        Guid snapshotId,
        string officialCode,
        string name,
        string normalizedName,
        string level,
        string? parentCode,
        double? centroidLatitude,
        double? centroidLongitude)
    {
        if (snapshotId == Guid.Empty)
        {
            throw new ArgumentException("Snapshot ID is required.", nameof(snapshotId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(officialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
        if (!TerritoryLevels.IsSupported(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown territory level.");
        }

        if (centroidLatitude.HasValue != centroidLongitude.HasValue)
        {
            throw new ArgumentException("Centroid latitude and longitude must be supplied together.");
        }

        SnapshotId = snapshotId;
        OfficialCode = officialCode;
        Name = name;
        NormalizedName = normalizedName;
        Level = level;
        ParentCode = parentCode;
        CentroidLatitude = centroidLatitude;
        CentroidLongitude = centroidLongitude;
    }

    public Guid SnapshotId { get; private set; }

    public string OfficialCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string Level { get; private set; } = string.Empty;

    public string? ParentCode { get; private set; }

    public double? CentroidLatitude { get; private set; }

    public double? CentroidLongitude { get; private set; }
}

public static class TerritoryNameNormalizer
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder normalized = new(decomposed.Length);
        bool previousWasWhitespace = false;

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (normalized.Length > 0 && !previousWasWhitespace)
                {
                    normalized.Append(' ');
                }

                previousWasWhitespace = true;
                continue;
            }

            normalized.Append(char.ToLowerInvariant(character));
            previousWasWhitespace = false;
        }

        return normalized.ToString().TrimEnd().Normalize(NormalizationForm.FormC);
    }
}

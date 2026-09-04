using System.Globalization;
using System.Text;

namespace AgropecuarIA.Catalog.Domain;

public static class CatalogSupportLevels
{
    public const string Catalogada = "CATALOGADA";
    public const string FlujoGenerico = "FLUJO_GENERICO";
    public const string EspecializadaValidada = "ESPECIALIZADA_VALIDADA";

    public static bool IsValid(string level) =>
        level is Catalogada or FlujoGenerico or EspecializadaValidada;
}

public static class CatalogCategories
{
    public const string Agricultura = "AGRICULTURA";
    public const string Ganaderia = "GANADERIA";
    public const string Forraje = "FORRAJE";
    public const string Horticultura = "HORTICULTURA";
    public const string Fruticultura = "FRUTICULTURA";
    public const string Forestacion = "FORESTACION";
    public const string Otros = "OTROS";

    public static bool IsValid(string value) => value is Agricultura or Ganaderia or Forraje or Horticultura or Fruticultura or Forestacion or Otros;
}

public sealed class CatalogPublishedVersion
{
    private CatalogPublishedVersion() { }

    public CatalogPublishedVersion(
        Guid id,
        string versionTag,
        bool isActive,
        string publishedBy,
        int itemsCount,
        DateTimeOffset publishedAtUtc,
        string? candidateHash = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Version ID is required.", nameof(id));

        ArgumentException.ThrowIfNullOrWhiteSpace(versionTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(publishedBy);

        Id = id;
        VersionTag = versionTag.Trim();
        IsActive = isActive;
        PublishedBy = publishedBy.Trim();
        ItemsCount = itemsCount;
        PublishedAtUtc = publishedAtUtc;
        CandidateHash = candidateHash;
    }

    public Guid Id { get; private set; }
    public string VersionTag { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string PublishedBy { get; private set; } = string.Empty;
    public int ItemsCount { get; private set; }
    public DateTimeOffset PublishedAtUtc { get; private set; }
    public string? CandidateHash { get; private set; }

    public void SetActive(bool active)
    {
        IsActive = active;
    }
}

public sealed class CatalogPublishedItem
{
    private CatalogPublishedItem() { }

    public CatalogPublishedItem(
        Guid id,
        Guid versionId,
        string code,
        string displayName,
        string jurisdiction,
        string supportLevel,
        string category,
        IReadOnlyList<string>? synonyms,
        bool isActive,
        DateTimeOffset createdAtUtc,
        Guid? sourceSnapshotId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Item ID is required.", nameof(id));
        if (versionId == Guid.Empty)
            throw new ArgumentException("Version ID is required.", nameof(versionId));

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        string effectiveSupportLevel = string.IsNullOrWhiteSpace(supportLevel)
            ? CatalogSupportLevels.FlujoGenerico
            : supportLevel.Trim().ToUpperInvariant();

        if (!CatalogSupportLevels.IsValid(effectiveSupportLevel))
            throw new ArgumentException($"Invalid support level: {supportLevel}", nameof(supportLevel));

        Id = id;
        VersionId = versionId;
        Code = code.Trim().ToUpperInvariant();
        NormalizedCode = CatalogNameNormalizer.Normalize(Code);
        DisplayName = displayName.Trim();
        NormalizedDisplayName = CatalogNameNormalizer.Normalize(DisplayName);
        Jurisdiction = string.IsNullOrWhiteSpace(jurisdiction) ? "AR" : jurisdiction.Trim().ToUpperInvariant();
        SupportLevel = effectiveSupportLevel;
        Category = string.IsNullOrWhiteSpace(category) ? CatalogCategories.Otros : category.Trim().ToUpperInvariant();
        Synonyms = synonyms?.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        SourceSnapshotId = sourceSnapshotId;
        NormalizedSynonyms = Synonyms.Select(CatalogNameNormalizer.Normalize).Distinct(StringComparer.Ordinal).ToArray();
    }

    public Guid Id { get; private set; }
    public Guid VersionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string NormalizedDisplayName { get; private set; } = string.Empty;
    public string Jurisdiction { get; private set; } = string.Empty;
    public string SupportLevel { get; private set; } = CatalogSupportLevels.FlujoGenerico;
    public string Category { get; private set; } = CatalogCategories.Otros;
    public List<string> Synonyms { get; private set; } = [];
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? SourceSnapshotId { get; private set; }
    public string[] NormalizedSynonyms { get; private set; } = [];

    public void SetActive(bool active)
    {
        IsActive = active;
    }
}

public static class CatalogNameNormalizer
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

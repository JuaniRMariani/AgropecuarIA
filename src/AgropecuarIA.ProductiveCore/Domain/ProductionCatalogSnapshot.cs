namespace AgropecuarIA.ProductiveCore.Domain;

public static class ProductionCatalogReferenceStatuses
{
    public const string ResolvedPublication = "resolved_publication";
    public const string LegacyUnresolved = "legacy_unresolved";
}

/// <summary>An immutable observation of a published catalog item, not a claim of professional approval.</summary>
public sealed record ProductionCatalogSnapshot(
    Guid VersionId,
    Guid ItemId,
    string VersionTag,
    string Code,
    string DisplayName,
    string DeclaredCatalogSupportLevel,
    Guid? SourceSnapshotId,
    string? SourceId,
    string? SourceHash,
    DateTimeOffset? SourceIngestedAtUtc,
    string ProvenanceStatus,
    DateTimeOffset ResolvedAtUtc)
{
    public void Validate()
    {
        if (VersionId == Guid.Empty || ItemId == Guid.Empty || ResolvedAtUtc == default)
            throw new ArgumentException("A resolved catalog reference requires its publication, item and observation time.");
        RequireText(VersionTag, 64);
        RequireText(Code, 64);
        RequireText(DisplayName, 256);
        RequireText(DeclaredCatalogSupportLevel, 64);
        bool verified = ProvenanceStatus == "verified_snapshot" && SourceSnapshotId is not null && SourceSnapshotId != Guid.Empty
            && !string.IsNullOrWhiteSpace(SourceId) && SourceId.Length <= 128 && SourceIngestedAtUtc is not null
            && SourceHash is { Length: 64 } && SourceHash.All(c => char.IsAsciiHexDigit(c) && !char.IsUpper(c));
        bool unavailable = ProvenanceStatus == "legacy_unavailable" && SourceSnapshotId is null && SourceId is null
            && SourceHash is null && SourceIngestedAtUtc is null;
        if (!verified && !unavailable) throw new ArgumentException("Catalog source provenance must be coherent.");
    }

    private static void RequireText(string value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Any(char.IsControl))
            throw new ArgumentException("The catalog snapshot contains invalid metadata.");
    }
}

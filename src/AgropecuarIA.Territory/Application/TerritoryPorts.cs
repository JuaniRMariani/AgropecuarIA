using AgropecuarIA.Territory.Domain;

namespace AgropecuarIA.Territory.Application;

public interface ITerritoryReferenceReader
{
    Task<TerritoryReferenceSearchPage?> SearchAsync(
        TerritoryReferenceSearchCriteria criteria,
        CancellationToken cancellationToken);
}

public interface ITerritorySnapshotImporter
{
    Task ImportAndActivateAsync(
        ValidatedTerritorySnapshot snapshot,
        CancellationToken cancellationToken);
}

public interface ITerritoryCoordinateProvider
{
    Task<ProviderTerritoryResolution?> ResolveAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}

public sealed record TerritoryReferenceSearchCriteria(
    string NormalizedQuery,
    string? Level,
    string? ParentCode,
    int Limit);

public sealed record TerritoryReferenceSearchPage(
    TerritoryReferenceSource Source,
    IReadOnlyList<TerritoryReferenceMatch> Items);

public sealed record TerritoryReferenceSource(
    string Provider,
    string Version,
    DateTimeOffset CapturedAtUtc);

public sealed record TerritoryReferenceMatch(
    string OfficialCode,
    string Name,
    string Level,
    string? ParentCode,
    string? ParentName,
    string HierarchyLabel);

public sealed record ProviderTerritoryResolution(
    TerritoryReferenceSource Source,
    TerritoryReferenceMatch Unit);

public sealed record ValidatedTerritorySnapshot(
    OfficialTerritorySnapshot Snapshot,
    IReadOnlyList<OfficialTerritoryUnit> Units);

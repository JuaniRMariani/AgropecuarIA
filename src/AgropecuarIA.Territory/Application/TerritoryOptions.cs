namespace AgropecuarIA.Territory.Application;

public sealed class TerritoryReferenceOptions
{
    public static string SectionName => "Territory:Reference";

    public bool CoordinateResolutionEnabled { get; init; }

    public TimeSpan SnapshotFreshFor { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan ResolutionFreshFor { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan ResolutionStaleFor { get; init; } = TimeSpan.FromHours(24);

    public int ResolutionCacheEntries { get; init; } = 1_024;
}

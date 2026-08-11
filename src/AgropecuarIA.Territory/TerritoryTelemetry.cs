using System.Diagnostics;
using System.Diagnostics.Metrics;
using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Domain;

namespace AgropecuarIA.Territory;

public sealed class TerritoryTelemetry
{
    public const string SourceName = "AgropecuarIA.Territory";

    private readonly Counter<long> requests;
    private readonly Histogram<long> resultCount;
    private readonly Histogram<double> referenceAgeHours;

    public TerritoryTelemetry(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(SourceName);
        requests = meter.CreateCounter<long>("territory.requests");
        resultCount = meter.CreateHistogram<long>("territory.search.results");
        referenceAgeHours = meter.CreateHistogram<double>("territory.reference.age.hours");
    }

    public void RecordSearch(
        string status,
        string level,
        int results,
        TimeSpan referenceAge)
    {
        TagList tags = new()
        {
            { "operation", "search" },
            { "status", NormalizeStatus(status) },
            { "level", NormalizeLevel(level) },
        };
        requests.Add(1, tags);
        resultCount.Record(results, tags);
        referenceAgeHours.Record(Math.Max(0, referenceAge.TotalHours), tags);
    }

    public void RecordResolve(string status, string source, string provider)
    {
        requests.Add(
            1,
            new TagList
            {
                { "operation", "resolve" },
                { "status", NormalizeStatus(status) },
                { "source", source is "cache" or "provider" or "none" ? source : "other" },
                { "provider", provider == "georef" ? provider : "other" },
            });
    }

    private static string NormalizeStatus(string status) => status switch
    {
        TerritoryReferenceStatuses.Fresh => TerritoryReferenceStatuses.Fresh,
        TerritoryReferenceStatuses.Stale => TerritoryReferenceStatuses.Stale,
        TerritoryReferenceStatuses.Unavailable => TerritoryReferenceStatuses.Unavailable,
        _ => "other",
    };

    private static string NormalizeLevel(string level) => level switch
    {
        TerritoryLevels.Province => TerritoryLevels.Province,
        TerritoryLevels.Department => TerritoryLevels.Department,
        TerritoryLevels.Municipality => TerritoryLevels.Municipality,
        TerritoryLevels.Locality => TerritoryLevels.Locality,
        "all" => "all",
        _ => "other",
    };
}

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgropecuarIA.ProductiveCore;

public sealed class ProductiveCoreTelemetry
{
    public const string SourceName = "AgropecuarIA.ProductiveCore";

    private static readonly ActivitySource ActivitySource = new(SourceName);
    private readonly Counter<long> operations;
    private readonly Histogram<long> resultCount;

    public ProductiveCoreTelemetry(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(SourceName);
        operations = meter.CreateCounter<long>("productive_core.operations");
        resultCount = meter.CreateHistogram<long>("productive_core.field.results");
    }

    public static Activity? Start(string operationName) =>
        ActivitySource.StartActivity(operationName, ActivityKind.Internal);

    public void Record(string operation, string outcome, int? results = null)
    {
        TagList tags = new()
        {
            { "operation", NormalizeOperation(operation) },
            { "outcome", NormalizeOutcome(outcome) },
        };
        operations.Add(1, tags);
        if (results is not null)
        {
            resultCount.Record(Math.Max(0, results.Value), tags);
        }
    }

    private static string NormalizeOperation(string operation) => operation switch
    {
        "field_create" or "field_list" or "field_detail" => operation,
        _ => "other",
    };

    private static string NormalizeOutcome(string outcome) => outcome switch
    {
        "succeeded" or
        "replayed" or
        "race" or
        "conflict" or
        "in_progress" or
        "failed_terminal" or
        "reconciliation_required" or
        "commit_unknown" or
        "not_available" or
        "unavailable" or
        "rejected" => outcome,
        _ => "other",
    };
}

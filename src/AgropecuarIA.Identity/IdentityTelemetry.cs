using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgropecuarIA.Identity;

public sealed class IdentityTelemetry : IDisposable
{
    public const string SourceName = "AgropecuarIA.Identity";
    private readonly Meter meter = new(SourceName, "1.0.0");
    private readonly Counter<long> operations;

    public IdentityTelemetry()
    {
        operations = meter.CreateCounter<long>("identity.operations");
    }

    public static ActivitySource ActivitySource { get; } = new(SourceName, "1.0.0");

    public static Activity? Start(string operation) => ActivitySource.StartActivity(operation);

    public void Record(string operation, string outcome, string? connection = null)
    {
        TagList tags = new()
        {
            { "identity.operation", operation },
            { "identity.outcome", outcome },
        };

        if (connection is not null)
        {
            tags.Add("identity.connection", connection);
        }

        operations.Add(1, tags);
    }

    public void Dispose() => meter.Dispose();
}

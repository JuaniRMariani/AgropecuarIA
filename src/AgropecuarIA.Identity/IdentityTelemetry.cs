using System.Diagnostics;
using System.Diagnostics.Metrics;
using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Identity;

public sealed class IdentityTelemetry
{
    public const string SourceName = "AgropecuarIA.Identity";
    public const string ContractVersion = "1.0.0";
    public const string ContractConsumer = "identity-api";
    private readonly Counter<long> operations;

    public IdentityTelemetry(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(SourceName, ContractVersion);
        operations = meter.CreateCounter<long>("identity.operations");
    }

    public static ActivitySource ActivitySource { get; } = new(SourceName, ContractVersion);

    public static Activity? Start(string operation)
    {
        Activity? activity = ActivitySource.StartActivity(operation);
        activity?.SetTag("contract.version", ContractVersion);
        activity?.SetTag("contract.consumer", ContractConsumer);
        return activity;
    }

    public void Record(
        string operation,
        string outcome,
        string? connection = null,
        string? purpose = null)
    {
        TagList tags = new()
        {
            { "contract.version", ContractVersion },
            { "contract.consumer", ContractConsumer },
            { "identity.operation", operation },
            { "identity.outcome", outcome },
        };

        if (connection is not null)
        {
            tags.Add("identity.connection", connection);
        }

        if (purpose is not null && StepUpPurposes.IsSupported(purpose))
        {
            tags.Add("identity.step_up_purpose", purpose);
        }

        operations.Add(1, tags);
    }
}

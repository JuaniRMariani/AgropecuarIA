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

    public void RecordOrganizationCreate(string outcome) =>
        Record(
            "organization_create",
            outcome switch
            {
                "succeeded" or
                "replayed" or
                "unavailable" or
                "reauthentication_required" or
                "conflict" or
                "in_progress" or
                "failed_terminal" or
                "reconciliation_required" => outcome,
                _ => "other",
            });

    public void RecordOrganizationOwnerInvitation(string operation, string outcome) =>
        Record(
            operation,
            outcome switch
            {
                "succeeded" or
                "replayed" or
                "rejected" or
                "conflict" or
                "unavailable" or
                "reauthentication_required" => outcome,
                _ => "other",
            });

    public void Record(
        string operation,
        string outcome,
        string? connection = null,
        string? purpose = null)
    {
        string boundedOperation = BoundOperation(operation);
        string boundedOutcome = BoundOutcome(outcome);
        TagList tags = new()
        {
            { "contract.version", ContractVersion },
            { "contract.consumer", ContractConsumer },
            { "identity.operation", boundedOperation },
            { "identity.outcome", boundedOutcome },
        };

        if (connection is not null)
        {
            tags.Add(
                "identity.connection",
                IdentityConnections.IsSupported(connection) ? connection : "other");
        }

        if (purpose is not null && StepUpPurposes.IsSupported(purpose))
        {
            tags.Add("identity.step_up_purpose", purpose);
        }

        operations.Add(1, tags);
    }

    private static string BoundOperation(string operation) => operation switch
    {
        "sign_in" or
        "step_up_started" or
        "step_up_validated" or
        "link_started" or
        "link_proof_attached" or
        "identity_linked" or
        "identity_unlinked" or
        "step_up_completed" or
        "organization_create" or
        "organization_owner_invitation_create" or
        "organization_owner_invitation_list" or
        "organization_owner_invitation_accept" or
        "organization_owner_invitation_revoke" or
        "session_revoked" => operation,
        _ => "other",
    };

    private static string BoundOutcome(string outcome) => outcome switch
    {
        "succeeded" or
        "rejected" or
        "conflict" or
        "unavailable" or
        "replayed" or
        "in_progress" or
        "failed_terminal" or
        "reconciliation_required" or
        "reauthentication_required" => outcome,
        _ => "other",
    };
}

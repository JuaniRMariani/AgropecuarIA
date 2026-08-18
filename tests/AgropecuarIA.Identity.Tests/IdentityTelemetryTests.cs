using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using AgropecuarIA.Identity.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class IdentityTelemetryTests
{
    private static readonly string[] OrganizationOutcomes =
    [
        "succeeded",
        "replayed",
        "unavailable",
        "reauthentication_required",
        "conflict",
        "in_progress",
        "failed_terminal",
        "reconciliation_required",
    ];

    [TestMethod]
    public void StepUpPurposeTagIsBoundedByDomainAllowList()
    {
        IReadOnlyDictionary<string, object?>[] recorded = CaptureMeasurements(telemetry =>
        {
            telemetry.Record(
                "step_up_started",
                "succeeded",
                purpose: StepUpPurposes.ManageAuthenticationMethods);
            telemetry.Record(
                "step_up_started",
                "rejected",
                purpose: "user-controlled-purpose");
        });

        Assert.HasCount(2, recorded);
        Assert.AreEqual(
            StepUpPurposes.ManageAuthenticationMethods,
            recorded[0]["identity.step_up_purpose"]);
        Assert.IsFalse(recorded[1].ContainsKey("identity.step_up_purpose"));
        Assert.IsFalse(recorded.Any(tags => tags.ContainsKey("identity.user_id")));
        Assert.IsFalse(recorded.Any(tags => tags.ContainsKey("identity.session_id")));
    }

    [TestMethod]
    public void OrganizationTelemetryUsesOnlyBoundedOutcomes()
    {
        IReadOnlyDictionary<string, object?>[] recorded = CaptureMeasurements(telemetry =>
        {
            foreach (string outcome in OrganizationOutcomes)
            {
                telemetry.RecordOrganizationCreate(outcome);
            }

            telemetry.RecordOrganizationCreate(
                "canary Estancia Norte organization-create-secret-key " +
                "d2719f32-42fc-4a73-8267-a8b8400eb31a");
        });

        CollectionAssert.AreEqual(
            OrganizationOutcomes,
            recorded[..^1]
                .Select(tags => (string)tags["identity.outcome"]!)
                .ToArray());
        Assert.IsTrue(recorded[..^1].All(tags =>
            Equals(tags["identity.operation"], "organization_create")));
        Assert.AreEqual("organization_create", recorded[^1]["identity.operation"]);
        Assert.AreEqual("other", recorded[^1]["identity.outcome"]);
        string serializedTags = string.Join(
            '|',
            recorded.SelectMany(tags => tags.Values).Select(value => value?.ToString()));
        Assert.IsFalse(serializedTags.Contains("Estancia Norte", StringComparison.Ordinal));
        Assert.IsFalse(serializedTags.Contains(
            "organization-create-secret-key",
            StringComparison.Ordinal));
        Assert.IsFalse(serializedTags.Contains(
            "d2719f32-42fc-4a73-8267-a8b8400eb31a",
            StringComparison.Ordinal));
    }

    [TestMethod]
    public void OwnerInvitationTelemetryCannotExposeBearerOrTenantData()
    {
        const string bearer = "secret-owner-invitation-bearer";
        IReadOnlyDictionary<string, object?>[] recorded = CaptureMeasurements(telemetry =>
        {
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_accept",
                "replayed");
            telemetry.RecordOrganizationOwnerInvitation(bearer, bearer);
        });

        Assert.AreEqual(
            "organization_owner_invitation_accept",
            recorded[0]["identity.operation"]);
        Assert.AreEqual("replayed", recorded[0]["identity.outcome"]);
        Assert.AreEqual("other", recorded[1]["identity.operation"]);
        Assert.AreEqual("other", recorded[1]["identity.outcome"]);
        Assert.IsFalse(string.Join(
            '|',
            recorded.SelectMany(tags => tags.Values)).Contains(
                bearer,
            StringComparison.Ordinal));
    }

    [TestMethod]
    public void OwnerMembershipTelemetryCannotExposeTenantOrMembershipData()
    {
        const string untrusted = "tenant-membership-d2719f32-42fc-4a73-8267-a8b8400eb31a";
        IReadOnlyDictionary<string, object?>[] recorded = CaptureMeasurements(telemetry =>
        {
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                "replayed");
            telemetry.RecordOrganizationOwnerMembership(untrusted, untrusted);
        });

        Assert.AreEqual(
            "organization_owner_membership_remove",
            recorded[0]["identity.operation"]);
        Assert.AreEqual("replayed", recorded[0]["identity.outcome"]);
        Assert.AreEqual("other", recorded[1]["identity.operation"]);
        Assert.AreEqual("other", recorded[1]["identity.outcome"]);
        Assert.IsFalse(string.Join(
            '|',
            recorded.SelectMany(tags => tags.Values)).Contains(
                untrusted,
            StringComparison.Ordinal));
    }

    [TestMethod]
    public void BulkSessionRevocationTelemetryIsBoundedAndContainsNoTargetIdentifier()
    {
        const string targetIdentifier = "d2719f32-42fc-4a73-8267-a8b8400eb31a";
        IReadOnlyDictionary<string, object?>[] recorded = CaptureMeasurements(telemetry =>
        {
            telemetry.Record("session_revoke_all_others", "succeeded");
            telemetry.Record(targetIdentifier, targetIdentifier);
        });

        Assert.AreEqual("session_revoke_all_others", recorded[0]["identity.operation"]);
        Assert.AreEqual("succeeded", recorded[0]["identity.outcome"]);
        Assert.AreEqual("other", recorded[1]["identity.operation"]);
        Assert.AreEqual("other", recorded[1]["identity.outcome"]);
        Assert.IsFalse(string.Join(
            '|',
            recorded.SelectMany(tags => tags.Values)).Contains(
                targetIdentifier,
                StringComparison.Ordinal));
    }

    private static IReadOnlyDictionary<string, object?>[] CaptureMeasurements(
        Action<IdentityTelemetry> record)
    {
        var measurements = new ConcurrentQueue<IReadOnlyDictionary<string, object?>>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == IdentityTelemetry.SourceName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var measurement = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                measurement[tag.Key] = tag.Value;
            }

            measurements.Enqueue(measurement);
        });
        listener.Start();

        using ServiceProvider services = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        IdentityTelemetry telemetry = new(services.GetRequiredService<IMeterFactory>());
        record(telemetry);
        return measurements.ToArray();
    }
}

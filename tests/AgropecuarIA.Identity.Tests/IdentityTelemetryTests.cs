using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using AgropecuarIA.Identity.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class IdentityTelemetryTests
{
    [TestMethod]
    public void StepUpPurposeTagIsBoundedByDomainAllowList()
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
        telemetry.Record(
            "step_up_started",
            "succeeded",
            purpose: StepUpPurposes.ManageAuthenticationMethods);
        telemetry.Record(
            "step_up_started",
            "rejected",
            purpose: "user-controlled-purpose");

        IReadOnlyDictionary<string, object?>[] recorded = measurements.ToArray();
        Assert.HasCount(2, recorded);
        Assert.AreEqual(
            StepUpPurposes.ManageAuthenticationMethods,
            recorded[0]["identity.step_up_purpose"]);
        Assert.IsFalse(recorded[1].ContainsKey("identity.step_up_purpose"));
        Assert.IsFalse(recorded.Any(tags => tags.ContainsKey("identity.user_id")));
        Assert.IsFalse(recorded.Any(tags => tags.ContainsKey("identity.session_id")));
    }
}

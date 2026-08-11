using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
[DoNotParallelize]
public sealed class TerritoryTelemetryTests
{
    [TestMethod]
    public void TelemetryBoundsTagsAndCannotExposeCoordinatesOrNames()
    {
        const string canary = "Estancia Norte -34.6037,-58.3816";
        IReadOnlyDictionary<string, object?>[] measurements = Capture(telemetry =>
        {
            telemetry.RecordSearch("fresh", "province", 3, TimeSpan.FromHours(4));
            telemetry.RecordResolve(canary, canary, canary);
        });

        Assert.IsGreaterThanOrEqualTo(3, measurements.Length);
        Assert.IsTrue(measurements.Any(tags => Equals(tags["operation"], "search")));
        IReadOnlyDictionary<string, object?> resolve = measurements.Single(tags =>
            Equals(tags["operation"], "resolve"));
        Assert.AreEqual("other", resolve["status"]);
        Assert.AreEqual("other", resolve["source"]);
        Assert.AreEqual("other", resolve["provider"]);
        Assert.IsFalse(string.Join(
            '|',
            measurements.SelectMany(tags => tags.Values)).Contains(
                canary,
                StringComparison.Ordinal));
    }

    private static IReadOnlyDictionary<string, object?>[] Capture(
        Action<TerritoryTelemetry> record)
    {
        ConcurrentQueue<IReadOnlyDictionary<string, object?>> measurements = new();
        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == TerritoryTelemetry.SourceName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
            measurements.Enqueue(ToDictionary(tags)));
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
            measurements.Enqueue(ToDictionary(tags)));
        listener.Start();

        using ServiceProvider services = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        TerritoryTelemetry telemetry = new(services.GetRequiredService<IMeterFactory>());
        record(telemetry);
        return measurements.ToArray();
    }

    private static Dictionary<string, object?> ToDictionary(
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            result[tag.Key] = tag.Value;
        }

        return result;
    }
}

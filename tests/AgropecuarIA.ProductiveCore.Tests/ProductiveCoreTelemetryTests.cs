using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ProductiveCoreTelemetryTests
{
    [TestMethod]
    public void RenameUsesBoundedOperationAndOutcomeDimensions()
    {
        var measurements = new List<(string Operation, string Outcome)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter.Name == ProductiveCoreTelemetry.SourceName &&
                instrument.Name == "productive_core.operations")
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string operation = string.Empty;
            string outcome = string.Empty;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "operation")
                {
                    operation = tag.Value?.ToString() ?? string.Empty;
                }
                else if (tag.Key == "outcome")
                {
                    outcome = tag.Value?.ToString() ?? string.Empty;
                }
            }

            measurements.Add((operation, outcome));
        });
        listener.Start();
        using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var telemetry = new ProductiveCoreTelemetry(
            provider.GetRequiredService<IMeterFactory>());

        telemetry.Record("field_rename", "stale");
        telemetry.Record("unbounded-operation", "unbounded-outcome");

        CollectionAssert.Contains(measurements, ("field_rename", "stale"));
        CollectionAssert.Contains(measurements, ("other", "other"));
    }
}

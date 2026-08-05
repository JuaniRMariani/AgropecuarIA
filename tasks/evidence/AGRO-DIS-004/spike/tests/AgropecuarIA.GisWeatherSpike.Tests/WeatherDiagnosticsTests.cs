using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgropecuarIA.GisWeatherSpike.Tests;

[TestClass]
[DoNotParallelize]
public sealed class WeatherDiagnosticsTests
{
    [TestMethod]
    public void OpenMeteoParserEmitsCorrelatedActivityAndMetricsForSuccessAndFailure()
    {
        var activities = new ConcurrentQueue<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WeatherDiagnostics.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Enqueue,
        };
        ActivitySource.AddActivityListener(activityListener);

        long parsedValues = 0;
        long parseErrors = 0;
        var durationSamples = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == WeatherDiagnostics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "agropecuaria.weather.parsed_values")
            {
                Interlocked.Add(ref parsedValues, measurement);
            }
            else if (instrument.Name == "agropecuaria.weather.parse_errors")
            {
                Interlocked.Add(ref parseErrors, measurement);
            }
        });
        meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
        {
            if (instrument.Name == "agropecuaria.weather.parse_duration")
            {
                Interlocked.Increment(ref durationSamples);
            }
        });
        meterListener.Start();

        var parser = new OpenMeteoParser(new FixedFreshnessPolicy(TimeSpan.FromHours(2)));
        var points = FixtureFiles.LoadNationalPoints();
        using (var valid = FixtureFiles.Open(Path.Combine("open-meteo", "valid-24-points.json")))
        {
            var success = parser.Parse(new OpenMeteoParseRequest(
                valid,
                200,
                points,
                "ecmwf_ifs025",
                new DateTimeOffset(2026, 8, 5, 11, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero)));
            Assert.IsTrue(success.IsSuccess, success.Error?.SafeMessage);
        }

        using (var failurePayload = FixtureFiles.Open(Path.Combine("open-meteo", "rate-limited.json")))
        {
            var failure = parser.Parse(new OpenMeteoParseRequest(
                failurePayload,
                429,
                [points[0]],
                "ecmwf_ifs025",
                new DateTimeOffset(2026, 8, 5, 11, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero),
                60));
            Assert.IsFalse(failure.IsSuccess);
        }

        Assert.AreEqual(168L, parsedValues);
        Assert.AreEqual(1L, parseErrors);
        Assert.AreEqual(2, durationSamples);
        Assert.HasCount(2, activities);
        Assert.IsTrue(activities.Any(activity => activity.Status == ActivityStatusCode.Ok));
        Assert.IsTrue(activities.Any(activity => activity.Status == ActivityStatusCode.Error));
        Assert.IsTrue(activities.All(activity => activity.GetTagItem("weather.provider")?.ToString() == "open-meteo"));
    }
}

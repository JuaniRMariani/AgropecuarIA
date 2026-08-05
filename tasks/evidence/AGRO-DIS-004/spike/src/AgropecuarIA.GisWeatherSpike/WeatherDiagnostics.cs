using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgropecuarIA.GisWeatherSpike;

public static class WeatherDiagnostics
{
    public const string ActivitySourceName = "AgropecuarIA.GisWeatherSpike";
    public const string MeterName = "AgropecuarIA.GisWeatherSpike";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    internal static readonly Meter Meter = new(MeterName, "1.0.0");
    internal static readonly Counter<long> ParsedValues = Meter.CreateCounter<long>(
        "agropecuaria.weather.parsed_values",
        unit: "{value}",
        description: "Weather values accepted after provider validation.");
    internal static readonly Counter<long> ParseErrors = Meter.CreateCounter<long>(
        "agropecuaria.weather.parse_errors",
        unit: "{error}",
        description: "Provider payloads rejected by error category.");
    internal static readonly Histogram<double> ParseDuration = Meter.CreateHistogram<double>(
        "agropecuaria.weather.parse_duration",
        unit: "ms",
        description: "Provider payload parsing latency.");
}

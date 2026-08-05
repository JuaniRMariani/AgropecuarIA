using System.Text;

namespace AgropecuarIA.GisWeatherSpike.Tests;

[TestClass]
public sealed class OpenMeteoParserTests
{
    private static readonly DateTimeOffset RetrievedAt = new(2026, 8, 5, 11, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
    private readonly OpenMeteoParser _parser = new(new FixedFreshnessPolicy(TimeSpan.FromHours(2)));

    [TestMethod]
    public void ParseValidNationalFixtureProducesSevenTraceableValuesForAll24Jurisdictions()
    {
        var points = FixtureFiles.LoadNationalPoints();
        using var payload = FixtureFiles.Open(Path.Combine("open-meteo", "valid-24-points.json"));

        var result = _parser.Parse(CreateRequest(payload, 200, points));

        Assert.IsTrue(result.IsSuccess, result.Error?.SafeMessage);
        var snapshots = result.Value ?? throw new AssertFailedException("Snapshots are required.");
        Assert.HasCount(24, points);
        Assert.HasCount(24 * 7, snapshots);
        Assert.IsTrue(snapshots.All(snapshot => snapshot.Provider == "open-meteo"));
        Assert.IsTrue(snapshots.All(snapshot => snapshot.Nature == WeatherNature.Forecast));
        Assert.IsTrue(snapshots.All(snapshot => snapshot.Availability == WeatherAvailability.Fresh));
        Assert.IsTrue(snapshots.All(snapshot => snapshot.Value is not null && snapshot.Error is null));
        Assert.AreEqual(24, snapshots.Select(snapshot => snapshot.RequestedPoint).Distinct().Count());
        Assert.HasCount(7, snapshots.Select(snapshot => snapshot.Variable).Distinct());
    }

    [TestMethod]
    [DataRow(429, ProviderErrorCode.RateLimited)]
    [DataRow(500, ProviderErrorCode.ProviderError)]
    [DataRow(504, ProviderErrorCode.Timeout)]
    public void ParseTransportFailureReturnsTypedErrorWithoutValues(int statusCode, ProviderErrorCode expected)
    {
        var fixture = statusCode == 429 ? "rate-limited.json" : "server-error.json";
        using var payload = FixtureFiles.Open(Path.Combine("open-meteo", fixture));

        var result = _parser.Parse(CreateRequest(payload, statusCode, [new GeoPoint(-34.61, -58.44)]));

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Value);
        Assert.AreEqual(expected, result.Error?.Code);
    }

    [TestMethod]
    public void ParseUnsupportedUnitReturnsSchemaInvalidInsteadOfConverting()
    {
        using var payload = FixtureFiles.Open(Path.Combine("open-meteo", "schema-drift.json"));

        var result = _parser.Parse(CreateRequest(payload, 200, [new GeoPoint(-34.61, -58.44)]));

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ProviderErrorCode.SchemaInvalid, result.Error?.Code);
        Assert.IsNull(result.Value);
    }

    [TestMethod]
    public void ParseNullProviderValueEmitsUnavailableAndNeverZero()
    {
        const string json = """
            {
              "latitude": -34.61,
              "longitude": -58.44,
              "utc_offset_seconds": 0,
              "hourly_units": {
                "time": "iso8601", "temperature_2m": "°C", "precipitation": "mm",
                "precipitation_probability": "%", "wind_speed_10m": "km/h", "relative_humidity_2m": "%",
                "wind_gusts_10m": "km/h", "et0_fao_evapotranspiration": "mm"
              },
              "hourly": {
                "time": ["2026-08-05T12:00"], "temperature_2m": [null], "precipitation": [0.0],
                "precipitation_probability": [0], "wind_speed_10m": [10.0], "relative_humidity_2m": [60]
                , "wind_gusts_10m": [15.0], "et0_fao_evapotranspiration": [0.1]
              }
            }
            """;
        using var payload = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = _parser.Parse(CreateRequest(payload, 200, [new GeoPoint(-34.61, -58.44)]));

        Assert.IsTrue(result.IsSuccess, result.Error?.SafeMessage);
        var temperature = (result.Value ?? throw new AssertFailedException())
            .Single(snapshot => snapshot.Variable == WeatherVariable.Temperature2m);
        Assert.AreEqual(WeatherAvailability.Unavailable, temperature.Availability);
        Assert.IsNull(temperature.Value);
        Assert.IsNull(temperature.Unit);
        Assert.AreEqual(ProviderErrorCode.Unavailable, temperature.Error?.Code);
    }

    [TestMethod]
    public void ParseOversizedPayloadFailsClosed()
    {
        using var payload = new MemoryStream(new byte[(4 * 1024 * 1024) + 1]);

        var result = _parser.Parse(CreateRequest(payload, 200, [new GeoPoint(-34.61, -58.44)]));

        Assert.AreEqual(ProviderErrorCode.PayloadTooLarge, result.Error?.Code);
    }

    [TestMethod]
    public void ParseFutureIngestionReturnsTypedUnavailableSnapshotsInsteadOfThrowing()
    {
        var points = FixtureFiles.LoadNationalPoints();
        using var payload = FixtureFiles.Open(Path.Combine("open-meteo", "valid-24-points.json"));

        var result = _parser.Parse(new OpenMeteoParseRequest(
            payload,
            200,
            points,
            "ecmwf_ifs025",
            Now.AddMinutes(6),
            Now));

        Assert.IsTrue(result.IsSuccess, result.Error?.SafeMessage);
        var snapshots = result.Value ?? throw new AssertFailedException("Snapshots are required.");
        Assert.IsTrue(snapshots.All(snapshot => snapshot.Availability == WeatherAvailability.Unavailable));
        Assert.IsTrue(snapshots.All(snapshot => snapshot.Value is null && snapshot.Unit is null));
        Assert.IsTrue(snapshots.All(snapshot => snapshot.Error?.Code == ProviderErrorCode.Unavailable));
    }

    private static OpenMeteoParseRequest CreateRequest(
        Stream payload,
        int statusCode,
        IReadOnlyList<GeoPoint> points) =>
        new(payload, statusCode, points, "ecmwf_ifs025", RetrievedAt, Now, statusCode == 429 ? 60 : null);
}

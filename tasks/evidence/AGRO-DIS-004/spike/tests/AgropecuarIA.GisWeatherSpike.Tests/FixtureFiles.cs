using System.Text.Json;

namespace AgropecuarIA.GisWeatherSpike.Tests;

internal static class FixtureFiles
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static Stream Open(string relativePath) => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "fixtures", relativePath));

    public static IReadOnlyList<GeoPoint> LoadNationalPoints()
    {
        using var stream = Open(Path.Combine("territory", "national-points.json"));
        var fixture = JsonSerializer.Deserialize<NationalPointsFixture>(stream, JsonOptions)
            ?? throw new InvalidOperationException("National points fixture could not be read.");
        return fixture.Points.Select(point => new GeoPoint(point.Latitude, point.Longitude)).ToArray();
    }

    private sealed record NationalPointsFixture(IReadOnlyList<NationalPointFixture> Points);

    private sealed record NationalPointFixture(double Latitude, double Longitude);
}

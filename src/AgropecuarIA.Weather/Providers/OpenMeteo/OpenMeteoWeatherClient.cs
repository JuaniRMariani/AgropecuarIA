using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgropecuarIA.Weather.Application;

namespace AgropecuarIA.Weather.Providers.OpenMeteo;

public sealed class OpenMeteoWeatherClient(HttpClient httpClient) : IWeatherForecastClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<WeatherProviderForecast?> FetchForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        try
        {
            string uri = FormattableString.Invariant(
                $"https://api.open-meteo.com/v1/forecast?latitude={latitude:F4}&longitude={longitude:F4}&hourly=temperature_2m,relative_humidity_2m,precipitation_probability,precipitation,wind_speed_10m&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max&timezone=auto");

            using HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument doc = JsonDocument.Parse(content);
            JsonElement root = doc.RootElement;

            string hourlyJson = root.TryGetProperty("hourly", out var hourly) ? hourly.GetRawText() : "{}";
            string dailyJson = root.TryGetProperty("daily", out var daily) ? daily.GetRawText() : "{}";

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset validUntil = now.AddHours(2);

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            string hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return new WeatherProviderForecast(
                "open-meteo",
                "gfs_seamless",
                now,
                validUntil,
                hourlyJson,
                dailyJson,
                hashHex);
        }
        catch
        {
            return null;
        }
    }
}

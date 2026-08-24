namespace AgropecuarIA.Weather.Application;

public sealed record WeatherProviderForecast(
    string Provider,
    string ModelName,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ValidUntilUtc,
    string HourlyVariablesJson,
    string DailyVariablesJson,
    string SnapshotHash);

public interface IWeatherForecastClient
{
    Task<WeatherProviderForecast?> FetchForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}

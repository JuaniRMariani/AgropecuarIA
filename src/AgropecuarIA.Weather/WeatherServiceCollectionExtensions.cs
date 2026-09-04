using AgropecuarIA.Weather.Application;
using AgropecuarIA.Weather.Infrastructure;
using AgropecuarIA.Weather.Providers.OpenMeteo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgropecuarIA.Weather;

public static class WeatherServiceCollectionExtensions
{
    public static IServiceCollection AddAgropecuariaWeather(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Weather")
            ?? throw new InvalidOperationException("ConnectionStrings:Weather is required.");

        services.AddDbContext<WeatherDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "weather")));

        services.AddHttpClient<IWeatherForecastClient, OpenMeteoWeatherClient>()
            .RemoveAllLoggers();
        services.AddScoped<WeatherForecastApplicationService>();
        services.AddScoped<WeatherAlertApplicationService>();
        services.AddScoped<WeatherActivityApplicationService>();

        return services;
    }
}

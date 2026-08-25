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
            ?? "Host=localhost;Database=agro_weather_dev";

        services.AddDbContext<WeatherDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(WeatherDbContext).Assembly.FullName)));

        services.AddHttpClient<IWeatherForecastClient, OpenMeteoWeatherClient>();
        services.AddScoped<WeatherForecastApplicationService>();
        services.AddScoped<WeatherAlertApplicationService>();
        services.AddScoped<WeatherActivityApplicationService>();

        return services;
    }
}

using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Delivery;
using AgropecuarIA.Territory.Infrastructure;
using AgropecuarIA.Territory.Providers.Georef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Territory;

public static class TerritoryServiceCollectionExtensions
{
    public static IServiceCollection AddTerritoryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Territory") ??
            throw new InvalidOperationException(
                "ConnectionStrings:Territory must be configured.");
        services.AddOptions<TerritoryReferenceOptions>()
            .Bind(configuration.GetSection(TerritoryReferenceOptions.SectionName))
            .Validate(IsValid, "Territory reference cache and freshness options are invalid.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(serviceProvider =>
        {
            TerritoryReferenceOptions options = serviceProvider
                .GetRequiredService<IOptions<TerritoryReferenceOptions>>()
                .Value;
            return new TerritoryResolutionCache(options);
        });
        services.AddScoped<TerritoryReferenceService>();
        services.AddSingleton<TerritoryTelemetry>();
        services.AddDbContext<TerritoryDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(TerritoryDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "territory");
            }));
        services.AddScoped<PostgresTerritoryReferenceRepository>();
        services.AddScoped<ITerritoryReferenceReader>(serviceProvider =>
            serviceProvider.GetRequiredService<PostgresTerritoryReferenceRepository>());
        services.AddScoped<ITerritorySnapshotImporter>(serviceProvider =>
            serviceProvider.GetRequiredService<PostgresTerritoryReferenceRepository>());
        services.AddExceptionHandler<TerritoryExceptionHandler>();
        services.AddHttpClient<ITerritoryCoordinateProvider, GeorefTerritoryClient>(client =>
            {
                client.BaseAddress = GeorefTerritoryClient.ServiceBaseAddress;
                client.Timeout = GeorefTerritoryClient.RequestTimeout;
            })
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                UseCookies = false,
            });

        return services;
    }

    private static bool IsValid(TerritoryReferenceOptions options) =>
        options.SnapshotFreshFor > TimeSpan.Zero &&
        options.ResolutionFreshFor > TimeSpan.Zero &&
        options.ResolutionStaleFor >= options.ResolutionFreshFor &&
        options.ResolutionCacheEntries is >= 1 and <= 10_000;
}

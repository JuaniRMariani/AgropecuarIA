using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgropecuarIA.Catalog;

public static class CatalogServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Catalog")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Catalog must be configured.");

        services.AddDbContextFactory<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    "catalog");
            }));

        services.AddScoped<CatalogIngestionApplicationService>();
        services.AddScoped<CatalogDiffApplicationService>();
        services.AddScoped<CatalogPublicationApplicationService>();
        services.AddScoped<CatalogSearchApplicationService>();

        return services;
    }
}

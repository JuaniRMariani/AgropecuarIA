using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Delivery;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgropecuarIA.ProductiveCore;

public static class ProductiveCoreServiceCollectionExtensions
{
    public static IServiceCollection AddProductiveCoreModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("ProductiveCore")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:ProductiveCore must be configured.");
        services.AddOptions<ManagementUnitCreationOptions>()
            .Bind(configuration.GetSection(ManagementUnitCreationOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                Microsoft.Extensions.Options.IValidateOptions<ManagementUnitCreationOptions>,
                ManagementUnitCreationOptionsValidator>());
        services.AddOptions<ManagementUnitRenameOptions>()
            .Bind(configuration.GetSection(ManagementUnitRenameOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                Microsoft.Extensions.Options.IValidateOptions<ManagementUnitRenameOptions>,
                ManagementUnitRenameOptionsValidator>());
        services.AddDbContextFactory<ProductiveCoreDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ProductiveCoreDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    "productive_core");
            }));
        services.AddScoped<IProductiveCoreUnitOfWorkFactory,
            PostgresProductiveCoreUnitOfWorkFactory>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ProductiveCoreTelemetry>();
        services.AddScoped<ProductiveCoreApplicationService>();
        services.AddScoped<ProductiveCoreRenameApplicationService>();
        services.AddExceptionHandler<ProductiveCoreExceptionHandler>();
        return services;
    }
}

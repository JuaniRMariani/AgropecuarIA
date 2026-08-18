using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgropecuarIA.ProductiveCore.Infrastructure;

internal sealed class ProductiveCoreDbContextFactory
    : IDesignTimeDbContextFactory<ProductiveCoreDbContext>
{
    public ProductiveCoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProductiveCoreDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=agropecuaria_design;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    "productive_core"))
            .Options;

        return new ProductiveCoreDbContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgropecuarIA.Catalog.Infrastructure;

internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=agropecuaria_design;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "catalog"))
            .Options;

        return new CatalogDbContext(options);
    }
}

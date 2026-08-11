using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgropecuarIA.Territory.Infrastructure;

internal sealed class TerritoryDbContextFactory : IDesignTimeDbContextFactory<TerritoryDbContext>
{
    public TerritoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TerritoryDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=agropecuaria_design;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    "territory"))
            .Options;

        return new TerritoryDbContext(options);
    }
}

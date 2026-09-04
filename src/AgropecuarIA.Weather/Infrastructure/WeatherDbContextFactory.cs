using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgropecuarIA.Weather.Infrastructure;

internal sealed class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
{
    public WeatherDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=agropecuaria_design;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "weather"))
            .Options;
        return new WeatherDbContext(options);
    }
}

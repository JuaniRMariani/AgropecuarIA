using AgropecuarIA.Weather.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Weather.Infrastructure;

public sealed class WeatherDbContext(DbContextOptions<WeatherDbContext> options) : DbContext(options)
{
    public DbSet<WeatherForecastSnapshot> ForecastSnapshots => Set<WeatherForecastSnapshot>();
    public DbSet<WeatherObservedRain> ObservedRains => Set<WeatherObservedRain>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("weather");

        modelBuilder.Entity<WeatherForecastSnapshot>(entity =>
        {
            entity.ToTable("forecast_snapshots");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CentroidLatitude).IsRequired();
            entity.Property(item => item.CentroidLongitude).IsRequired();
            entity.Property(item => item.Provider).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ModelName).HasMaxLength(64).IsRequired();
            entity.Property(item => item.IssuedAtUtc).IsRequired();
            entity.Property(item => item.ValidUntilUtc).IsRequired();
            entity.Property(item => item.HourlyVariablesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(item => item.DailyVariablesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(item => item.SnapshotHash).HasMaxLength(64).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();

            entity.HasIndex(item => new { item.CentroidLatitude, item.CentroidLongitude, item.ValidUntilUtc });
            entity.HasIndex(item => item.SnapshotHash).IsUnique();
        });

        modelBuilder.Entity<WeatherObservedRain>(entity =>
        {
            entity.ToTable("observed_rains");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OrganizationId).IsRequired();
            entity.Property(item => item.FieldId).IsRequired();
            entity.Property(item => item.ObservedDateUtc).IsRequired();
            entity.Property(item => item.AmountMillimeters).HasPrecision(10, 2).IsRequired();
            entity.Property(item => item.Method).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(512);
            entity.Property(item => item.RecordedByUserId).IsRequired();
            entity.Property(item => item.RecordedAtUtc).IsRequired();

            entity.HasIndex(item => new { item.OrganizationId, item.FieldId, item.ObservedDateUtc });
        });
    }
}

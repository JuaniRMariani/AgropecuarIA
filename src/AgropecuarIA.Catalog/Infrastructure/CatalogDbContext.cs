using AgropecuarIA.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<CatalogSourceSnapshot> CatalogSourceSnapshots => Set<CatalogSourceSnapshot>();
    public DbSet<CatalogStagingEntry> CatalogStagingEntries => Set<CatalogStagingEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.Entity<CatalogSourceSnapshot>(entity =>
        {
            entity.ToTable("catalog_source_snapshots");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SourceId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ContentHash).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasIndex(item => new { item.SourceId, item.ContentHash }).IsUnique();
        });

        modelBuilder.Entity<CatalogStagingEntry>(entity =>
        {
            entity.ToTable("catalog_staging_entries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SourceId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.SourceHash).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(64).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(item => item.Jurisdiction).HasMaxLength(64);
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasIndex(item => new { item.SourceId, item.SourceHash, item.Code }).IsUnique();
        });
    }
}
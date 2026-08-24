using System.Text.Json;
using AgropecuarIA.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<CatalogSourceSnapshot> CatalogSourceSnapshots => Set<CatalogSourceSnapshot>();
    public DbSet<CatalogStagingEntry> CatalogStagingEntries => Set<CatalogStagingEntry>();
    public DbSet<CatalogPublishedVersion> CatalogPublishedVersions => Set<CatalogPublishedVersion>();
    public DbSet<CatalogPublishedItem> CatalogPublishedItems => Set<CatalogPublishedItem>();

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

        modelBuilder.Entity<CatalogPublishedVersion>(entity =>
        {
            entity.ToTable("catalog_published_versions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.VersionTag).HasMaxLength(64).IsRequired();
            entity.Property(item => item.PublishedBy).HasMaxLength(128).IsRequired();
            entity.Property(item => item.IsActive).IsRequired();
            entity.Property(item => item.ItemsCount).IsRequired();
            entity.Property(item => item.PublishedAtUtc).IsRequired();
            entity.HasIndex(item => item.VersionTag).IsUnique();
            entity.HasIndex(item => item.IsActive);
        });

        modelBuilder.Entity<CatalogPublishedItem>(entity =>
        {
            entity.ToTable("catalog_published_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.VersionId).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(64).IsRequired();
            entity.Property(item => item.NormalizedCode).HasMaxLength(64).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(item => item.NormalizedDisplayName).HasMaxLength(256).IsRequired();
            entity.Property(item => item.Jurisdiction).HasMaxLength(64).IsRequired();
            entity.Property(item => item.SupportLevel).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Category).HasMaxLength(64).IsRequired();
            entity.Property(item => item.IsActive).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();

            entity.Property(x => x.Synonyms)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("jsonb");

            entity.HasIndex(item => new { item.VersionId, item.Code }).IsUnique();
            entity.HasIndex(item => item.NormalizedCode);
            entity.HasIndex(item => item.NormalizedDisplayName);
            entity.HasIndex(item => item.Jurisdiction);
            entity.HasIndex(item => item.SupportLevel);
        });
    }
}
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
    public DbSet<CatalogPublishedSource> CatalogPublishedSources => Set<CatalogPublishedSource>();
    public DbSet<CatalogEditorialAudit> CatalogEditorialAudits => Set<CatalogEditorialAudit>();
    public DbSet<CatalogOutboxMessage> CatalogOutboxMessages => Set<CatalogOutboxMessage>();

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
            entity.Property(item => item.IngestionSequence).UseIdentityAlwaysColumn();
            entity.HasIndex(item => item.IngestionSequence).IsUnique();
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
            entity.Property(item => item.NormalizedCode).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Category).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Synonyms).HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()).HasColumnType("jsonb");
            entity.HasOne<CatalogSourceSnapshot>().WithMany().HasForeignKey(item => item.SourceSnapshotId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.SourceSnapshotId, item.NormalizedCode }).IsUnique().HasFilter("\"SourceSnapshotId\" IS NOT NULL");
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
            entity.Property(item => item.CandidateHash).HasMaxLength(64);
            entity.HasIndex(item => item.VersionTag).IsUnique();
            entity.HasIndex(item => item.IsActive).IsUnique().HasFilter("\"IsActive\" = TRUE");
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
            entity.Property(item => item.NormalizedSynonyms).HasColumnType("text[]");
            entity.HasOne<CatalogPublishedVersion>().WithMany().HasForeignKey(item => item.VersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CatalogSourceSnapshot>().WithMany().HasForeignKey(item => item.SourceSnapshotId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.VersionId, item.NormalizedCode }).IsUnique();
        });

        modelBuilder.Entity<CatalogPublishedSource>(entity =>
        {
            entity.ToTable("catalog_published_sources");
            entity.HasKey(item => new { item.VersionId, item.SourceSnapshotId });
            entity.HasOne<CatalogPublishedVersion>().WithMany().HasForeignKey(item => item.VersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CatalogSourceSnapshot>().WithMany().HasForeignKey(item => item.SourceSnapshotId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CatalogEditorialAudit>(entity =>
        {
            entity.ToTable("catalog_editorial_audits");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Action).HasMaxLength(64);
            entity.Property(item => item.CorrelationId).HasMaxLength(128);
            entity.HasOne<CatalogPublishedVersion>().WithMany().HasForeignKey(item => item.VersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CatalogSourceSnapshot>().WithMany().HasForeignKey(item => item.SourceSnapshotId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CatalogOutboxMessage>(entity =>
        {
            entity.ToTable("catalog_outbox_messages");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EventType).HasMaxLength(64);
            entity.Property(item => item.SchemaVersion).HasMaxLength(16);
            entity.Property(item => item.Source).HasMaxLength(64);
            entity.Property(item => item.Scope).HasMaxLength(16);
            entity.Property(item => item.AggregateType).HasMaxLength(64);
            entity.Property(item => item.CorrelationId).HasMaxLength(128);
            entity.Property(item => item.PayloadJson).HasColumnType("jsonb");
            entity.HasIndex(item => item.AuditId).IsUnique();
            entity.HasOne<CatalogEditorialAudit>().WithMany().HasForeignKey(item => item.AuditId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CatalogPublishedVersion>().WithMany().HasForeignKey(item => item.AggregateId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

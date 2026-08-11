using AgropecuarIA.Territory.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Territory.Infrastructure;

public sealed class TerritoryDbContext(DbContextOptions<TerritoryDbContext> options)
    : DbContext(options)
{
    public DbSet<OfficialTerritorySnapshot> OfficialTerritorySnapshots =>
        Set<OfficialTerritorySnapshot>();

    public DbSet<OfficialTerritoryUnit> OfficialTerritoryUnits => Set<OfficialTerritoryUnit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("territory");

        modelBuilder.Entity<OfficialTerritorySnapshot>(entity =>
        {
            entity.ToTable(
                "snapshots",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_snapshots_ContentHash",
                        "octet_length(\"ContentHash\") = 32");
                    table.HasCheckConstraint(
                        "CK_snapshots_Status",
                        "\"Status\" IN ('staging', 'active', 'retired')");
                    table.HasCheckConstraint(
                        "CK_snapshots_Activation",
                        "(\"Status\" = 'staging' AND \"ActivatedAtUtc\" IS NULL) OR " +
                        "(\"Status\" IN ('active', 'retired') AND \"ActivatedAtUtc\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_snapshots_Source",
                        "length(btrim(\"Provider\")) > 0 AND length(btrim(\"Version\")) > 0");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Provider).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Version).HasMaxLength(80).IsRequired();
            entity.Property(item => item.CapturedAtUtc).IsRequired();
            entity.Property(item => item.ContentHash).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(16).IsRequired();
            entity.Property(item => item.ImportedAtUtc).IsRequired();
            entity.HasIndex(item => new { item.Provider, item.Version }).IsUnique();
            entity.HasIndex(item => item.Status)
                .IsUnique()
                .HasFilter("\"Status\" = 'active'");
        });

        modelBuilder.Entity<OfficialTerritoryUnit>(entity =>
        {
            entity.ToTable(
                "official_units",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_official_units_Level",
                        "\"Level\" IN ('province', 'department', 'municipality', 'locality')");
                    table.HasCheckConstraint(
                        "CK_official_units_Parent",
                        "(\"Level\" = 'province' AND \"ParentCode\" IS NULL) OR " +
                        "(\"Level\" <> 'province' AND \"ParentCode\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_official_units_Identity",
                        "length(btrim(\"OfficialCode\")) > 0 AND " +
                        "length(btrim(\"Name\")) > 0 AND " +
                        "length(btrim(\"NormalizedName\")) > 0");
                    table.HasCheckConstraint(
                        "CK_official_units_Centroid",
                        "(\"CentroidLatitude\" IS NULL AND \"CentroidLongitude\" IS NULL) OR " +
                        "(\"CentroidLatitude\" IS NOT NULL AND \"CentroidLongitude\" IS NOT NULL AND " +
                        "\"CentroidLatitude\" BETWEEN -90 AND 90 AND " +
                        "\"CentroidLongitude\" BETWEEN -180 AND 180)");
                    table.HasCheckConstraint(
                        "CK_official_units_NotSelfParent",
                        "\"ParentCode\" IS NULL OR \"ParentCode\" <> \"OfficialCode\"");
                });
            entity.HasKey(item => new { item.SnapshotId, item.OfficialCode });
            entity.Property(item => item.OfficialCode).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.NormalizedName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Level).HasMaxLength(16).IsRequired();
            entity.Property(item => item.ParentCode).HasMaxLength(16);
            entity.HasIndex(item => new
            {
                item.SnapshotId,
                item.NormalizedName,
                item.OfficialCode,
            });
            entity.HasIndex(item => new { item.SnapshotId, item.Level, item.ParentCode });
            entity.HasOne<OfficialTerritorySnapshot>()
                .WithMany()
                .HasForeignKey(item => item.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OfficialTerritoryUnit>()
                .WithMany()
                .HasForeignKey(item => new { item.SnapshotId, item.ParentCode })
                .HasPrincipalKey(item => new { item.SnapshotId, item.OfficialCode })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

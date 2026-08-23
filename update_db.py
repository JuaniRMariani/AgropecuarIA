import os

db_context_path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Infrastructure\ProductiveCoreDbContext.cs'
with open(db_context_path, 'r', encoding='utf-8-sig') as f:
    content = f.read()

# Add DbSets
db_sets = '''    public DbSet<ManagementUnitArchiveLedger> ManagementUnitArchiveLedgers =>
        Set<ManagementUnitArchiveLedger>();

    public DbSet<ManagementUnitArchiveKeyAlias> ManagementUnitArchiveKeyAliases =>
        Set<ManagementUnitArchiveKeyAlias>();

'''
content = content.replace('    public DbSet<ProductiveJournalEntry>', db_sets + '    public DbSet<ProductiveJournalEntry>')

# Add Model Configurations
# We will find the end of ManagementUnitRenameKeyAlias and inject Archive configurations
archive_config = '''        modelBuilder.Entity<ManagementUnitArchiveLedger>(entity =>
        {
            entity.ToTable(
                "management_unit_archive_ledgers",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_Protocol",
                        "\\"ScopeKind\\" = 'tenant' AND " +
                        "\\"Namespace\\" = 'management_unit' AND " +
                        "\\"Operation\\" = 'archive_field' AND " +
                        "\\"ContractVersion\\" = 1 AND \\"CanonicalizationVersion\\" = 1");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_DigestLength",
                        "octet_length(\\"RequestFingerprint\\") = 32");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_Fence",
                        "\\"FenceToken\\" > 0 AND \\"LeaseUntilUtc\\" > \\"StartedAtUtc\\"");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_State",
                        "(\\"State\\" = 'in_progress' " +
                        "AND \\"ResultVersion\\" IS NULL AND \\"ResultRevision\\" IS NULL " +
                        "AND \\"CompletedAtUtc\\" IS NULL) OR " +
                        "(\\"State\\" IN ('succeeded', 'response_expired') " +
                        "AND \\"ResultVersion\\" IS NOT NULL " +
                        "AND \\"ResultRevision\\" >= 2 " +
                        "AND \\"CompletedAtUtc\\" IS NOT NULL) OR " +
                        "(\\"State\\" = 'failed_terminal' " +
                        "AND \\"ResultVersion\\" IS NULL AND \\"ResultRevision\\" IS NULL " +
                        "AND \\"CompletedAtUtc\\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_Times",
                        "(\\"CompletedAtUtc\\" IS NULL OR \\"CompletedAtUtc\\" >= \\"StartedAtUtc\\")");
                });
            entity.HasKey(item => item.Id);
            entity.HasAlternateKey(item => new { item.Id, item.OrganizationId });
            entity.Property(item => item.ScopeKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Namespace).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(64).IsRequired();
            entity.Property(item => item.RequestFingerprint).HasMaxLength(32).IsRequired();
            entity.Property(item => item.State).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new
            {
                item.OrganizationId,
                item.ActorUserId,
                item.StartedAtUtc,
            });
            entity.HasIndex(item => new
            {
                item.OrganizationId,
                item.ManagementUnitId,
                item.ExpectedVersion,
            });
            entity.HasIndex(item => new
            {
                item.OrganizationId,
                item.State,
                item.LeaseUntilUtc,
            });
            entity.HasOne<ManagementUnit>()
                .WithMany()
                .HasForeignKey(item => new { item.ManagementUnitId, item.OrganizationId })
                .HasPrincipalKey(item => new { item.Id, item.OrganizationId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ManagementUnitArchiveKeyAlias>(entity =>
        {
            entity.ToTable(
                "management_unit_archive_key_aliases",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_key_aliases_Protocol",
                        "\\"ScopeKind\\" = 'tenant' AND " +
                        "\\"Namespace\\" = 'management_unit' AND " +
                        "\\"Operation\\" = 'archive_field'");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_key_aliases_DigestLength",
                        "octet_length(\\"KeyDigest\\") = 32");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ScopeKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Namespace).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(64).IsRequired();
            entity.Property(item => item.KeyVersion).HasMaxLength(32).IsRequired();
            entity.Property(item => item.KeyDigest).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasIndex(item => new
            {
                item.OrganizationId,
                item.ScopeKind,
                item.Namespace,
                item.Operation,
                item.KeyVersion,
                item.KeyDigest,
            }).IsUnique();
            entity.HasIndex(item => new { item.LedgerId, item.KeyVersion }).IsUnique();
            entity.HasOne<ManagementUnitArchiveLedger>()
                .WithMany()
                .HasForeignKey(item => new { item.LedgerId, item.OrganizationId })
                .HasPrincipalKey(item => new { item.Id, item.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade);
        });

'''
content = content.replace('        modelBuilder.Entity<ProductiveJournalEntry>', archive_config + '        modelBuilder.Entity<ProductiveJournalEntry>')

with open(db_context_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Updated DbContext.")

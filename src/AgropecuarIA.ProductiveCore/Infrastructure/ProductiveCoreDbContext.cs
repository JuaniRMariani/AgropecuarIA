using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.ProductiveCore.Infrastructure;

public sealed class ProductiveCoreDbContext(DbContextOptions<ProductiveCoreDbContext> options)
    : DbContext(options)
{
    public DbSet<ManagementUnit> ManagementUnits => Set<ManagementUnit>();

    public DbSet<ManagementUnitCreationLedger> ManagementUnitCreationLedgers =>
        Set<ManagementUnitCreationLedger>();

    public DbSet<ManagementUnitCreationKeyAlias> ManagementUnitCreationKeyAliases =>
        Set<ManagementUnitCreationKeyAlias>();

    public DbSet<ManagementUnitRenameLedger> ManagementUnitRenameLedgers =>
        Set<ManagementUnitRenameLedger>();

    public DbSet<ManagementUnitRenameKeyAlias> ManagementUnitRenameKeyAliases =>
        Set<ManagementUnitRenameKeyAlias>();

    public DbSet<ManagementUnitArchiveLedger> ManagementUnitArchiveLedgers =>
        Set<ManagementUnitArchiveLedger>();

    public DbSet<ManagementUnitArchiveKeyAlias> ManagementUnitArchiveKeyAliases =>
        Set<ManagementUnitArchiveKeyAlias>();

    public DbSet<ProductiveJournalEntry> ProductiveJournalEntries =>
        Set<ProductiveJournalEntry>();

    public DbSet<ProductiveOutboxMessage> ProductiveOutboxMessages =>
        Set<ProductiveOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("productive_core");

        modelBuilder.Entity<ManagementUnit>(entity =>
        {
            entity.ToTable(
                "management_units",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_units_UnitType",
                        "\"UnitType\" = 'field'");
                    table.HasCheckConstraint(
                        "CK_management_units_Status",
                        "\"Status\" = 'draft'");
                    table.HasCheckConstraint(
                        "CK_management_units_SpatialStatus",
                        "\"SpatialStatus\" = 'not_configured'");
                    table.HasCheckConstraint(
                        "CK_management_units_DisplayName",
                        "char_length(\"DisplayName\") BETWEEN 2 AND 120");
                    table.HasCheckConstraint(
                        "CK_management_units_Revision",
                        "\"Revision\" >= 1");
                });
            entity.HasKey(item => item.Id);
            entity.HasAlternateKey(item => new { item.Id, item.OrganizationId });
            entity.Property(item => item.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.UnitType).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired();
            entity.Property(item => item.SpatialStatus).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.Property(item => item.Revision).HasDefaultValue(1L).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new { item.OrganizationId, item.CreatedAtUtc, item.Id });
        });

        modelBuilder.Entity<ManagementUnitCreationLedger>(entity =>
        {
            entity.ToTable(
                "management_unit_creation_ledgers",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_unit_creation_ledgers_Protocol",
                        "\"ScopeKind\" = 'tenant' AND " +
                        "\"Namespace\" = 'management_unit' AND " +
                        "\"Operation\" = 'create_field' AND " +
                        "\"ContractVersion\" = 1 AND \"CanonicalizationVersion\" = 1");
                    table.HasCheckConstraint(
                        "CK_management_unit_creation_ledgers_DigestLength",
                        "octet_length(\"RequestFingerprint\") = 32");
                    table.HasCheckConstraint(
                        "CK_management_unit_creation_ledgers_Fence",
                        "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_management_unit_creation_ledgers_State",
                        "(\"State\" = 'in_progress' AND \"ManagementUnitId\" IS NULL " +
                        "AND \"ResultVersion\" IS NULL AND \"CompletedAtUtc\" IS NULL) OR " +
                        "(\"State\" IN ('succeeded', 'response_expired') " +
                        "AND \"ManagementUnitId\" IS NOT NULL AND \"ResultVersion\" IS NOT NULL " +
                        "AND \"CompletedAtUtc\" IS NOT NULL) OR " +
                        "(\"State\" = 'failed_terminal' AND \"ManagementUnitId\" IS NULL " +
                        "AND \"ResultVersion\" IS NULL AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_management_unit_creation_ledgers_Times",
                        "(\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\")");
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
                item.State,
                item.LeaseUntilUtc,
            });
            entity.HasOne<ManagementUnit>()
                .WithMany()
                .HasForeignKey(item => new { item.ManagementUnitId, item.OrganizationId })
                .HasPrincipalKey(item => new { item.Id, item.OrganizationId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ManagementUnitCreationKeyAlias>(entity =>
        {
            entity.ToTable(
                "management_unit_creation_key_aliases",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_unit_creation_key_aliases_Protocol",
                        "\"ScopeKind\" = 'tenant' AND " +
                        "\"Namespace\" = 'management_unit' AND " +
                        "\"Operation\" = 'create_field'");
                    table.HasCheckConstraint(
                        "CK_management_unit_creation_key_aliases_DigestLength",
                        "octet_length(\"KeyDigest\") = 32");
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
            entity.HasOne<ManagementUnitCreationLedger>()
                .WithMany()
                .HasForeignKey(item => new { item.LedgerId, item.OrganizationId })
                .HasPrincipalKey(item => new { item.Id, item.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ManagementUnitRenameLedger>(entity =>
        {
            entity.ToTable(
                "management_unit_rename_ledgers",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_unit_rename_ledgers_Protocol",
                        "\"ScopeKind\" = 'tenant' AND " +
                        "\"Namespace\" = 'management_unit' AND " +
                        "\"Operation\" = 'rename_field' AND " +
                        "\"ContractVersion\" = 1 AND \"CanonicalizationVersion\" = 1");
                    table.HasCheckConstraint(
                        "CK_management_unit_rename_ledgers_DigestLength",
                        "octet_length(\"RequestFingerprint\") = 32");
                    table.HasCheckConstraint(
                        "CK_management_unit_rename_ledgers_Fence",
                        "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_management_unit_rename_ledgers_State",
                        "(\"State\" = 'in_progress' AND \"ResultDisplayName\" IS NULL " +
                        "AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL " +
                        "AND \"CompletedAtUtc\" IS NULL) OR " +
                        "(\"State\" IN ('succeeded', 'response_expired') " +
                        "AND \"ResultDisplayName\" IS NOT NULL " +
                        "AND \"ResultVersion\" IS NOT NULL " +
                        "AND \"ResultRevision\" >= 2 " +
                        "AND \"CompletedAtUtc\" IS NOT NULL) OR " +
                        "(\"State\" = 'failed_terminal' AND \"ResultDisplayName\" IS NULL " +
                        "AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL " +
                        "AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_management_unit_rename_ledgers_Times",
                        "(\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\")");
                });
            entity.HasKey(item => item.Id);
            entity.HasAlternateKey(item => new { item.Id, item.OrganizationId });
            entity.Property(item => item.ScopeKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Namespace).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(64).IsRequired();
            entity.Property(item => item.RequestFingerprint).HasMaxLength(32).IsRequired();
            entity.Property(item => item.State).HasMaxLength(32).IsRequired();
            entity.Property(item => item.ResultDisplayName).HasMaxLength(120);
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

        modelBuilder.Entity<ManagementUnitRenameKeyAlias>(entity =>
        {
            entity.ToTable(
                "management_unit_rename_key_aliases",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_unit_rename_key_aliases_Protocol",
                        "\"ScopeKind\" = 'tenant' AND " +
                        "\"Namespace\" = 'management_unit' AND " +
                        "\"Operation\" = 'rename_field'");
                    table.HasCheckConstraint(
                        "CK_management_unit_rename_key_aliases_DigestLength",
                        "octet_length(\"KeyDigest\") = 32");
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
            entity.HasOne<ManagementUnitRenameLedger>()
                .WithMany()
                .HasForeignKey(item => new { item.LedgerId, item.OrganizationId })
                .HasPrincipalKey(item => new { item.Id, item.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ManagementUnitArchiveLedger>(entity =>
        {
            entity.ToTable(
                "management_unit_archive_ledgers",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_Protocol",
                        "\"ScopeKind\" = 'tenant' AND " +
                        "\"Namespace\" = 'management_unit' AND " +
                        "\"Operation\" = 'archive_field' AND " +
                        "\"ContractVersion\" = 1 AND \"CanonicalizationVersion\" = 1");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_DigestLength",
                        "octet_length(\"RequestFingerprint\") = 32");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_Fence",
                        "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_State",
                        "(\"State\" = 'in_progress' " +
                        "AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL " +
                        "AND \"CompletedAtUtc\" IS NULL) OR " +
                        "(\"State\" IN ('succeeded', 'response_expired') " +
                        "AND \"ResultVersion\" IS NOT NULL " +
                        "AND \"ResultRevision\" >= 2 " +
                        "AND \"CompletedAtUtc\" IS NOT NULL) OR " +
                        "(\"State\" = 'failed_terminal' " +
                        "AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL " +
                        "AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_ledgers_Times",
                        "(\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\")");
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
                        "\"ScopeKind\" = 'tenant' AND " +
                        "\"Namespace\" = 'management_unit' AND " +
                        "\"Operation\" = 'archive_field'");
                    table.HasCheckConstraint(
                        "CK_management_unit_archive_key_aliases_DigestLength",
                        "octet_length(\"KeyDigest\") = 32");
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

        modelBuilder.Entity<ProductiveJournalEntry>(entity =>
        {
            entity.ToTable(
                "journal_entries",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_journal_entries_Action",
                        "\"Action\" IN ('management_unit_created', " +
                        "'management_unit_display_name_changed')");
                    table.HasCheckConstraint(
                        "CK_journal_entries_Outcome",
                        "\"Outcome\" = 'succeeded'");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Action).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Outcome).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.OccurredAtUtc).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.OccurredAtUtc });
        });

        modelBuilder.Entity<ProductiveOutboxMessage>(entity =>
        {
            entity.ToTable(
                "outbox_messages",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_productive_outbox_messages_EventType",
                        "\"EventType\" IN ('ManagementUnitCreated', " +
                        "'ManagementUnitDisplayNameChanged')");
                    table.HasCheckConstraint(
                        "CK_productive_outbox_messages_AggregateType",
                        "\"AggregateType\" = 'ManagementUnit'");
                    table.HasCheckConstraint(
                        "CK_productive_outbox_messages_Versions",
                        "\"SchemaVersion\" = '1.0.0' AND " +
                        "((\"EventType\" = 'ManagementUnitCreated' AND \"AggregateVersion\" = 1) OR " +
                        "(\"EventType\" = 'ManagementUnitDisplayNameChanged' " +
                        "AND \"AggregateVersion\" >= 2))");
                    table.HasCheckConstraint(
                        "CK_productive_outbox_messages_SourceScope",
                        "\"Source\" = 'productive-core' AND \"Scope\" = 'tenant'");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EventType).HasMaxLength(128).IsRequired();
            entity.Property(item => item.SchemaVersion).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Source).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Scope).HasMaxLength(16).IsRequired();
            entity.Property(item => item.AggregateType).HasMaxLength(128).IsRequired();
            entity.Property(item => item.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.OccurredAtUtc).IsRequired();
            entity.Property(item => item.AvailableAtUtc).IsRequired();
            entity.Property(item => item.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(item => new { item.AvailableAtUtc, item.OccurredAtUtc });
            entity.HasIndex(item => new
            {
                item.OrganizationId,
                item.AggregateType,
                item.AggregateId,
                item.AggregateVersion,
            }).IsUnique();
        });
    }
}

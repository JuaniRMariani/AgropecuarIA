using AgropecuarIA.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<PlatformUser> Users => Set<PlatformUser>();

    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    public DbSet<OrganizationMembership> Memberships => Set<OrganizationMembership>();

    public DbSet<UserSession> Sessions => Set<UserSession>();

    public DbSet<LinkAttempt> LinkAttempts => Set<LinkAttempt>();

    public DbSet<StepUpAttempt> StepUpAttempts => Set<StepUpAttempt>();

    public DbSet<IdentitySecurityJournalEntry> SecurityJournalEntries =>
        Set<IdentitySecurityJournalEntry>();

    public DbSet<IdentityOutboxMessage> OutboxMessages => Set<IdentityOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<PlatformUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.ToTable("external_identities");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Connection).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Issuer).HasMaxLength(512).IsRequired();
            entity.Property(item => item.Subject).HasMaxLength(512).IsRequired();
            entity.Property(item => item.Label).HasMaxLength(160).IsRequired();
            entity.Property(item => item.VerifiedAtUtc).IsRequired();
            entity.HasIndex(item => new { item.Issuer, item.Subject }).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.Connection }).IsUnique();
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.ToTable("organization_memberships");
            entity.HasKey(item => new { item.UserId, item.OrganizationId });
            entity.Property(item => item.OrganizationName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Role).HasMaxLength(64).IsRequired();
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable(
                "sessions",
                table => table.HasCheckConstraint(
                    "CK_sessions_StrongAuthentication",
                    "(\"StrongAuthenticatedAtUtc\" IS NULL AND \"StrongAuthenticationPurpose\" IS NULL) OR " +
                    "(\"StrongAuthenticatedAtUtc\" IS NOT NULL AND " +
                    "\"StrongAuthenticationPurpose\" IS NOT NULL AND " +
                    $"\"StrongAuthenticationPurpose\" = '{StepUpPurposes.ManageAuthenticationMethods}')"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TokenHash).HasMaxLength(32).IsRequired();
            entity.Property(item => item.AuthenticatedAtUtc).IsRequired();
            entity.Property(item => item.ExpiresAtUtc).IsRequired();
            entity.Property(item => item.IsAuthenticationAssuranceVerified)
                .HasDefaultValue(false)
                .IsRequired();
            entity.Property(item => item.StrongAuthenticationPurpose).HasMaxLength(64);
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => item.TokenHash).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.ExpiresAtUtc });
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StepUpAttempt>(entity =>
        {
            entity.ToTable(
                "step_up_attempts",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_step_up_attempts_Purpose",
                        $"\"Purpose\" = '{StepUpPurposes.ManageAuthenticationMethods}'");
                    table.HasCheckConstraint(
                        "CK_step_up_attempts_Expiry",
                        "\"ExpiresAtUtc\" > \"StartedAtUtc\"");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Purpose).HasMaxLength(64).IsRequired();
            entity.Property(item => item.StartedAtUtc).IsRequired();
            entity.Property(item => item.ExpiresAtUtc).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new { item.UserId, item.ExpiresAtUtc });
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(item => item.InitiatingSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LinkAttempt>(entity =>
        {
            entity.ToTable("link_attempts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Connection).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CandidateIssuer).HasMaxLength(512);
            entity.Property(item => item.CandidateSubject).HasMaxLength(512);
            entity.Property(item => item.CandidateLabel).HasMaxLength(160);
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new { item.UserId, item.ExpiresAtUtc });
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(item => item.InitiatingSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentitySecurityJournalEntry>(entity =>
        {
            // Keep the original physical table during the N/N-1 expansion window.
            entity.ToTable("audit_events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Action).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Outcome).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Connection).HasMaxLength(32);
            entity.Property(item => item.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.OccurredAtUtc).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.OccurredAtUtc });
        });

        modelBuilder.Entity<IdentityOutboxMessage>(entity =>
        {
            entity.ToTable(
                "outbox_messages",
                table => table.HasCheckConstraint(
                    "CK_outbox_messages_Scope",
                    "(\"ScopeKind\" = 'platform' AND \"TenantId\" IS NULL) OR " +
                    "(\"ScopeKind\" = 'tenant' AND \"TenantId\" IS NOT NULL)"));
            entity.HasKey(item => item.EventId);
            entity.Property(item => item.Type).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Version).IsRequired();
            entity.Property(item => item.SchemaVersion).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Source).HasMaxLength(80).IsRequired();
            entity.Property(item => item.ScopeKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.OccurredAtUtc).IsRequired();
            entity.Property(item => item.RecordedAtUtc).IsRequired();
            entity.Property(item => item.ActorId).IsRequired();
            entity.Property(item => item.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(item => item.AggregateType).HasMaxLength(128).IsRequired();
            entity.Property(item => item.AggregateId).IsRequired();
            entity.Property(item => item.AggregateVersion).IsRequired();
            entity.Property(item => item.Payload).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(item => new { item.DispatchedAtUtc, item.OccurredAtUtc });
            entity.HasIndex(item => new
            {
                item.Source,
                item.ScopeKind,
                item.TenantId,
                item.AggregateType,
                item.AggregateId,
                item.AggregateVersion,
            })
                .IsUnique()
                .HasFilter(
                    "\"Source\" IS NOT NULL AND \"ScopeKind\" IS NOT NULL AND " +
                    "\"AggregateType\" IS NOT NULL AND \"AggregateVersion\" IS NOT NULL")
                .AreNullsDistinct(false);
        });
    }
}

using AgropecuarIA.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<PlatformUser> Users => Set<PlatformUser>();

    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    public DbSet<OrganizationMembership> Memberships => Set<OrganizationMembership>();

    public DbSet<OrganizationDirectoryEntry> Organizations => Set<OrganizationDirectoryEntry>();

    public DbSet<OrganizationMembershipAssignment> AuthoritativeMemberships =>
        Set<OrganizationMembershipAssignment>();

    public DbSet<OrganizationFieldScopeAssignment> FieldScopes =>
        Set<OrganizationFieldScopeAssignment>();

    public DbSet<OrganizationCreationLedger> OrganizationCreationLedgers =>
        Set<OrganizationCreationLedger>();

    public DbSet<OrganizationCreationKeyAlias> OrganizationCreationKeyAliases =>
        Set<OrganizationCreationKeyAlias>();

    public DbSet<OrganizationOwnerRemovalLedger> OrganizationOwnerRemovalLedgers =>
        Set<OrganizationOwnerRemovalLedger>();

    public DbSet<OrganizationOwnerRemovalKeyAlias> OrganizationOwnerRemovalKeyAliases =>
        Set<OrganizationOwnerRemovalKeyAlias>();

    public DbSet<OrganizationOwnerInvitation> OrganizationOwnerInvitations =>
        Set<OrganizationOwnerInvitation>();

    public DbSet<UserSession> Sessions => Set<UserSession>();

    public DbSet<LinkAttempt> LinkAttempts => Set<LinkAttempt>();

    public DbSet<StepUpAttempt> StepUpAttempts => Set<StepUpAttempt>();

    public DbSet<IdentitySecurityJournalEntry> SecurityJournalEntries =>
        Set<IdentitySecurityJournalEntry>();

    public DbSet<IdentityOutboxMessage> OutboxMessages => Set<IdentityOutboxMessage>();

    public DbSet<UserTotpCredential> TotpCredentials => Set<UserTotpCredential>();
    public DbSet<UserPasskeyCredential> PasskeyCredentials => Set<UserPasskeyCredential>();
    public DbSet<UserRecoveryCode> RecoveryCodes => Set<UserRecoveryCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<UserTotpCredential>(entity =>
        {
            entity.ToTable("totp_credentials");
            entity.HasKey(item => item.UserId);
            entity.Property(item => item.ProtectedSecret).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasOne<PlatformUser>().WithOne().HasForeignKey<UserTotpCredential>(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPasskeyCredential>(entity =>
        {
            entity.ToTable("passkey_credentials");
            entity.HasKey(item => item.CredentialId);
            entity.Property(item => item.UserId).IsRequired();
            entity.Property(item => item.PublicKey).IsRequired();
            entity.Property(item => item.SignCount).IsRequired();
            entity.Property(item => item.Aaguid).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasIndex(item => item.UserId);
            entity.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRecoveryCode>(entity =>
        {
            entity.ToTable("recovery_codes");
            entity.HasKey(item => new { item.UserId, item.CodeHash });
            entity.Property(item => item.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

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

        modelBuilder.Entity<OrganizationDirectoryEntry>(entity =>
        {
            entity.ToTable(
                "organizations",
                table => table.HasCheckConstraint(
                    "CK_organizations_Status",
                    $"\"Status\" = '{OrganizationStatuses.Active}'"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(16).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new { item.CreatedByUserId, item.CreatedAtUtc });
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationMembershipAssignment>(entity =>
        {
            entity.ToTable(
                "memberships",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_memberships_Role",
                        $"\"Role\" IN ('{OrganizationMembershipRoles.Owner}', '{OrganizationMembershipRoles.Admin}', '{OrganizationMembershipRoles.Agronomist}', '{OrganizationMembershipRoles.Operator}', '{OrganizationMembershipRoles.Accountant}', '{OrganizationMembershipRoles.Viewer}')");
                    table.HasCheckConstraint(
                        "CK_memberships_Status",
                        $"(\"Status\" = '{OrganizationMembershipStatuses.Active}' AND " +
                        "\"RemovedAtUtc\" IS NULL AND \"RemovedByUserId\" IS NULL) OR " +
                        $"(\"Status\" = '{OrganizationMembershipStatuses.Removed}' AND " +
                        "\"RemovedAtUtc\" >= \"CreatedAtUtc\" AND \"RemovedByUserId\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_memberships_SecurityVersion",
                        "\"SecurityVersion\" > 0");
                });

        modelBuilder.Entity<OrganizationFieldScopeAssignment>(entity =>
        {
            entity.ToTable("organization_field_scopes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OrganizationId).IsRequired();
            entity.Property(item => item.MembershipId).IsRequired();
            entity.Property(item => item.FieldId).IsRequired();
            entity.Property(item => item.GrantedByUserId).IsRequired();
            entity.Property(item => item.GrantedAtUtc).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.MembershipId, item.FieldId }).IsUnique();
            entity.HasOne<OrganizationMembershipAssignment>()
                .WithMany()
                .HasForeignKey(item => item.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Role).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(16).IsRequired();
            entity.Property(item => item.SecurityVersion).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new { item.OrganizationId, item.UserId }).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.Status });
            entity.HasIndex(item => new { item.OrganizationId, item.Status });
            entity.HasOne<OrganizationDirectoryEntry>()
                .WithMany()
                .HasForeignKey(item => item.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.RemovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationOwnerRemovalLedger>(entity =>
        {
            entity.ToTable(
                "organization_owner_removal_ledgers",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_organization_owner_removal_ledgers_Protocol",
                        $"\"ScopeKind\" = '{OrganizationOwnerRemovalProtocol.ScopeKind}' AND " +
                        $"\"Namespace\" = '{OrganizationOwnerRemovalProtocol.Namespace}' AND " +
                        $"\"Operation\" = '{OrganizationOwnerRemovalProtocol.Operation}' AND " +
                        "\"ContractVersion\" > 0 AND \"CanonicalizationVersion\" > 0");
                    table.HasCheckConstraint(
                        "CK_organization_owner_removal_ledgers_State",
                        $"(\"State\" = '{OrganizationOwnerRemovalProtocol.States.InProgress}' AND " +
                        "\"ResultMembershipVersion\" IS NULL AND \"ResultAuthorizationVersion\" IS NULL AND " +
                        "\"RemovedAtUtc\" IS NULL AND " +
                        "\"CompletedAtUtc\" IS NULL) OR " +
                        $"(\"State\" = '{OrganizationOwnerRemovalProtocol.States.Succeeded}' AND " +
                        "\"ResultMembershipVersion\" IS NOT NULL AND \"ResultAuthorizationVersion\" IS NOT NULL AND " +
                        "\"RemovedAtUtc\" IS NOT NULL AND " +
                        "\"CompletedAtUtc\" IS NOT NULL) OR " +
                        $"(\"State\" = '{OrganizationOwnerRemovalProtocol.States.FailedTerminal}' AND " +
                        "\"ResultMembershipVersion\" IS NULL AND \"ResultAuthorizationVersion\" IS NULL AND " +
                        "\"RemovedAtUtc\" IS NULL AND " +
                        "\"CompletedAtUtc\" IS NOT NULL) OR " +
                        $"(\"State\" = '{OrganizationOwnerRemovalProtocol.States.ResponseExpired}' AND " +
                        "\"ResultMembershipVersion\" IS NOT NULL AND \"ResultAuthorizationVersion\" IS NOT NULL AND " +
                        "\"RemovedAtUtc\" IS NOT NULL AND " +
                        "\"CompletedAtUtc\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_organization_owner_removal_ledgers_Fence",
                        "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_organization_owner_removal_ledgers_Result",
                        "(\"ResultAuthorizationVersion\" IS NULL OR \"ResultAuthorizationVersion\" > 1) AND " +
                        "(\"RemovedAtUtc\" IS NULL OR \"RemovedAtUtc\" >= \"StartedAtUtc\") AND " +
                        "(\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\") AND " +
                        "(\"RemovedAtUtc\" IS NULL OR \"CompletedAtUtc\" IS NULL OR " +
                        "\"CompletedAtUtc\" >= \"RemovedAtUtc\")");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ScopeKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Namespace).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(64).IsRequired();
            entity.Property(item => item.RequestFingerprint).HasMaxLength(32).IsRequired();
            entity.Property(item => item.State).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new { item.OrganizationId, item.ActorUserId, item.StartedAtUtc });
            entity.HasIndex(item => new { item.OrganizationId, item.State, item.LeaseUntilUtc });
            entity.HasOne<OrganizationDirectoryEntry>()
                .WithMany()
                .HasForeignKey(item => item.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(item => item.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<OrganizationMembershipAssignment>()
                .WithMany()
                .HasForeignKey(item => item.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationOwnerRemovalKeyAlias>(entity =>
        {
            entity.ToTable(
                "organization_owner_removal_key_aliases",
                table => table.HasCheckConstraint(
                    "CK_organization_owner_removal_key_aliases_Protocol",
                    $"\"ScopeKind\" = '{OrganizationOwnerRemovalProtocol.ScopeKind}' AND " +
                    $"\"Namespace\" = '{OrganizationOwnerRemovalProtocol.Namespace}' AND " +
                    $"\"Operation\" = '{OrganizationOwnerRemovalProtocol.Operation}'"));
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
            entity.HasOne<OrganizationOwnerRemovalLedger>()
                .WithMany()
                .HasForeignKey(item => item.LedgerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OrganizationDirectoryEntry>()
                .WithMany()
                .HasForeignKey(item => item.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationCreationLedger>(entity =>
        {
            entity.ToTable(
                "organization_creation_ledgers",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_organization_creation_ledgers_Protocol",
                        $"\"ScopeKind\" = '{OrganizationCreationProtocol.ScopeKind}' AND " +
                        $"\"Namespace\" = '{OrganizationCreationProtocol.Namespace}' AND " +
                        $"\"Operation\" = '{OrganizationCreationProtocol.Operation}' AND " +
                        "\"ContractVersion\" > 0 AND \"CanonicalizationVersion\" > 0");
                    table.HasCheckConstraint(
                        "CK_organization_creation_ledgers_State",
                        $"(\"State\" = '{OrganizationCreationProtocol.States.InProgress}' AND " +
                        "\"OrganizationId\" IS NULL AND \"MembershipId\" IS NULL AND " +
                        "\"CompletedAtUtc\" IS NULL) OR " +
                        $"(\"State\" = '{OrganizationCreationProtocol.States.Succeeded}' AND " +
                        "\"OrganizationId\" IS NOT NULL AND \"MembershipId\" IS NOT NULL AND " +
                        "\"CompletedAtUtc\" IS NOT NULL) OR " +
                        $"(\"State\" = '{OrganizationCreationProtocol.States.FailedTerminal}' AND " +
                        "\"OrganizationId\" IS NULL AND \"MembershipId\" IS NULL AND " +
                        "\"CompletedAtUtc\" IS NOT NULL) OR " +
                        $"(\"State\" = '{OrganizationCreationProtocol.States.ResponseExpired}' AND " +
                        "\"OrganizationId\" IS NOT NULL AND \"MembershipId\" IS NOT NULL AND " +
                        "\"CompletedAtUtc\" IS NOT NULL)");
                    table.HasCheckConstraint(
                        "CK_organization_creation_ledgers_Fence",
                        "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ScopeKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Namespace).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(64).IsRequired();
            entity.Property(item => item.RequestFingerprint).HasMaxLength(32).IsRequired();
            entity.Property(item => item.State).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new { item.ActorUserId, item.StartedAtUtc });
            entity.HasIndex(item => new { item.State, item.LeaseUntilUtc });
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(item => item.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationCreationKeyAlias>(entity =>
        {
            entity.ToTable(
                "organization_creation_key_aliases",
                table => table.HasCheckConstraint(
                    "CK_organization_creation_key_aliases_Protocol",
                    $"\"ScopeKind\" = '{OrganizationCreationProtocol.ScopeKind}' AND " +
                    $"\"Namespace\" = '{OrganizationCreationProtocol.Namespace}' AND " +
                    $"\"Operation\" = '{OrganizationCreationProtocol.Operation}'"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ScopeKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Namespace).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(64).IsRequired();
            entity.Property(item => item.KeyVersion).HasMaxLength(32).IsRequired();
            entity.Property(item => item.KeyDigest).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.HasIndex(item => new
            {
                item.ScopeKind,
                item.Namespace,
                item.Operation,
                item.KeyVersion,
                item.KeyDigest,
            }).IsUnique();
            entity.HasIndex(item => new { item.LedgerId, item.KeyVersion }).IsUnique();
            entity.HasOne<OrganizationCreationLedger>()
                .WithMany()
                .HasForeignKey(item => item.LedgerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationOwnerInvitation>(entity =>
        {
            entity.ToTable(
                "organization_owner_invitations",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_organization_owner_invitations_Digests",
                        "octet_length(\"CreationKeyDigest\") = 32 AND " +
                        "octet_length(\"TokenDigest\") = 32");
                    table.HasCheckConstraint(
                        "CK_organization_owner_invitations_Expiry",
                        "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");
                    table.HasCheckConstraint(
                        "CK_organization_owner_invitations_State",
                        $"(\"Status\" = '{OrganizationOwnerInvitationStatuses.Pending}' AND " +
                        "\"AcceptedAtUtc\" IS NULL AND \"AcceptedByUserId\" IS NULL AND " +
                        "\"AcceptedMembershipId\" IS NULL AND \"RevokedAtUtc\" IS NULL AND " +
                        "\"RevokedByUserId\" IS NULL) OR " +
                        $"(\"Status\" = '{OrganizationOwnerInvitationStatuses.Accepted}' AND " +
                        "\"AcceptedAtUtc\" >= \"CreatedAtUtc\" AND " +
                        "\"AcceptedAtUtc\" < \"ExpiresAtUtc\" AND " +
                        "\"AcceptedByUserId\" IS NOT NULL AND \"AcceptedMembershipId\" IS NOT NULL AND " +
                        "\"RevokedAtUtc\" IS NULL AND \"RevokedByUserId\" IS NULL) OR " +
                        $"(\"Status\" = '{OrganizationOwnerInvitationStatuses.Revoked}' AND " +
                        "\"RevokedAtUtc\" >= \"CreatedAtUtc\" AND " +
                        "\"RevokedAtUtc\" < \"ExpiresAtUtc\" AND " +
                        "\"RevokedByUserId\" IS NOT NULL AND \"AcceptedAtUtc\" IS NULL AND " +
                        "\"AcceptedByUserId\" IS NULL AND \"AcceptedMembershipId\" IS NULL)");
                });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CreationKeyVersion).HasMaxLength(32).IsRequired();
            entity.Property(item => item.CreationKeyDigest).HasMaxLength(32).IsRequired();
            entity.Property(item => item.TokenKeyVersion).HasMaxLength(32).IsRequired();
            entity.Property(item => item.TokenDigest).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(16).IsRequired();
            entity.Property(item => item.CreatedAtUtc).IsRequired();
            entity.Property(item => item.ExpiresAtUtc).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => new
            {
                item.OrganizationId,
                item.CreationKeyVersion,
                item.CreationKeyDigest,
            }).IsUnique();
            entity.HasIndex(item => new { item.TokenKeyVersion, item.TokenDigest }).IsUnique();
            entity.HasIndex(item => new { item.OrganizationId, item.Status, item.ExpiresAtUtc });
            entity.HasOne<OrganizationDirectoryEntry>()
                .WithMany()
                .HasForeignKey(item => item.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(item => item.CreationSessionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.AcceptedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<OrganizationMembershipAssignment>()
                .WithMany()
                .HasForeignKey(item => item.AcceptedMembershipId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(item => item.RevokedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
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
                    $"\"StrongAuthenticationPurpose\" IN " +
                    $"('{StepUpPurposes.ManageAuthenticationMethods}', " +
                    $"'{StepUpPurposes.ManageOrganizationOwners}', " +
                    $"'{StepUpPurposes.ManageSessions}'))"));
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
                        $"\"Purpose\" IN ('{StepUpPurposes.ManageAuthenticationMethods}', " +
                        $"'{StepUpPurposes.ManageOrganizationOwners}', " +
                        $"'{StepUpPurposes.ManageSessions}')");
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

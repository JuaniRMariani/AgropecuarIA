import sys

path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.Identity\Infrastructure\IdentityDbContext.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

db_sets = '''    public DbSet<UserTotpCredential> TotpCredentials => Set<UserTotpCredential>();
    public DbSet<UserPasskeyCredential> PasskeyCredentials => Set<UserPasskeyCredential>();
    public DbSet<UserRecoveryCode> RecoveryCodes => Set<UserRecoveryCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)'''

content = content.replace('    protected override void OnModelCreating(ModelBuilder modelBuilder)', db_sets)

config = '''        modelBuilder.Entity<UserTotpCredential>(entity =>
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

        modelBuilder.Entity<PlatformUser>('''

content = content.replace('        modelBuilder.Entity<PlatformUser>(', config)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

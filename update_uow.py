import os

repo_path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Infrastructure\PostgresProductiveCoreUnitOfWork.cs'
with open(repo_path, 'r', encoding='utf-8-sig') as f:
    content = f.read()

# Add to Interface in IProductiveCoreUnitOfWork (wait, that is in ProductiveCorePorts.cs, let's update it too!)
# First, update PostgresProductiveCoreUnitOfWork

archive_methods = '''
    public Task<Guid?> FindArchiveLedgerIdAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken) =>
        FindLedgerIdAsync(
            organizationId,
            aliases,
            "management_unit_archive_key_aliases",
            "archive idempotency ledger",
            cancellationToken);

    public async Task<ManagementUnitArchiveLedger?> GetArchiveLedgerAsync(
        Guid organizationId,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ManagementUnitArchiveLedgers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.OrganizationId == organizationId && item.Id == ledgerId,
                    cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("read the archive ledger", exception);
        }
    }

    public void AddArchive(
        ManagementUnitArchiveLedger ledger,
        IReadOnlyCollection<ManagementUnitArchiveKeyAlias> aliases,
        ProductiveJournalEntry journalEntry,
        ProductiveOutboxMessage outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(aliases);
        ArgumentNullException.ThrowIfNull(journalEntry);
        ArgumentNullException.ThrowIfNull(outboxMessage);
        dbContext.ManagementUnitArchiveLedgers.Add(ledger);
        dbContext.ManagementUnitArchiveKeyAliases.AddRange(aliases);
        dbContext.ProductiveJournalEntries.Add(journalEntry);
        dbContext.ProductiveOutboxMessages.Add(outboxMessage);
    }

    public async Task AddMissingArchiveAliasesAsync(
        Guid organizationId,
        Guid ledgerId,
        IReadOnlyDictionary<string, byte[]> aliases,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        try
        {
            string[] versions = aliases.Keys.ToArray();
            ManagementUnitArchiveKeyAlias[] existing = await dbContext
                .ManagementUnitArchiveKeyAliases
                .AsNoTracking()
                .Where(item => item.OrganizationId == organizationId &&
                    item.LedgerId == ledgerId &&
                    versions.Contains(item.KeyVersion))
                .ToArrayAsync(cancellationToken);
            foreach ((string version, byte[] digest) in aliases)
            {
                ManagementUnitArchiveKeyAlias? current = existing.SingleOrDefault(
                    item => string.Equals(item.KeyVersion, version, StringComparison.Ordinal));
                if (current is not null)
                {
                    if (!CryptographicOperations.FixedTimeEquals(current.KeyDigest, digest))
                    {
                        throw new ProductiveIdempotencyRaceException(
                            "An archive idempotency key version is already bound to another digest.");
                    }

                    continue;
                }

                dbContext.ManagementUnitArchiveKeyAliases.Add(
                    new ManagementUnitArchiveKeyAlias(
                        Guid.NewGuid(),
                        ledgerId,
                        organizationId,
                        version,
                        digest,
                        createdAtUtc));
            }
        }
        catch (ProductiveIdempotencyRaceException)
        {
            throw;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("reconcile archive idempotency key aliases", exception);
        }
    }
'''

content = content.replace('    public async Task SaveChangesAsync', archive_methods + '\\n    public async Task SaveChangesAsync')

with open(repo_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Updated Postgres Unit of Work.")

# Update Ports
ports_path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Application\ProductiveCorePorts.cs'
with open(ports_path, 'r', encoding='utf-8-sig') as f:
    ports_content = f.read()

archive_ports = '''
    Task<Guid?> FindArchiveLedgerIdAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken);

    Task<ManagementUnitArchiveLedger?> GetArchiveLedgerAsync(
        Guid organizationId,
        Guid ledgerId,
        CancellationToken cancellationToken);

    void AddArchive(
        ManagementUnitArchiveLedger ledger,
        IReadOnlyCollection<ManagementUnitArchiveKeyAlias> aliases,
        ProductiveJournalEntry journalEntry,
        ProductiveOutboxMessage outboxMessage);

    Task AddMissingArchiveAliasesAsync(
        Guid organizationId,
        Guid ledgerId,
        IReadOnlyDictionary<string, byte[]> aliases,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken);
'''
ports_content = ports_content.replace('    Task SaveChangesAsync', archive_ports + '\\n    Task SaveChangesAsync')

with open(ports_path, 'w', encoding='utf-8') as f:
    f.write(ports_content)
print("Updated Ports.")

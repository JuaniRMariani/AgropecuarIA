using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Application;

public enum ProductiveTransactionMode
{
    Read,
    SerializableWrite,
}

public interface IProductiveCoreUnitOfWorkFactory
{
    ValueTask<IProductiveCoreUnitOfWork> BeginAsync(
        ProductiveTransactionMode mode,
        CancellationToken cancellationToken);
}

public interface IProductiveCoreUnitOfWork : IAsyncDisposable
{
    Task<Guid?> AuthorizeOwnerAsync(
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<bool> RetainedKeyVersionsCoveredAsync(
        IReadOnlyCollection<string> retainedKeyVersions,
        CancellationToken cancellationToken);

    Task<Guid?> FindCreationLedgerIdAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken);

    Task<ManagementUnitCreationLedger?> GetCreationLedgerAsync(
        Guid organizationId,
        Guid ledgerId,
        CancellationToken cancellationToken);

    Task<ManagementUnit?> GetManagementUnitAsync(
        Guid organizationId,
        Guid managementUnitId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ManagementUnit>> ListManagementUnitsAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<int> CountManagementUnitsAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    void AddCreation(
        ManagementUnit managementUnit,
        ManagementUnitCreationLedger ledger,
        IReadOnlyCollection<ManagementUnitCreationKeyAlias> aliases,
        ProductiveJournalEntry journalEntry,
        ProductiveOutboxMessage outboxMessage);

    Task AddMissingAliasesAsync(
        Guid organizationId,
        Guid ledgerId,
        IReadOnlyDictionary<string, byte[]> aliases,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}

public sealed class ProductiveIdempotencyRaceException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

public sealed class ProductiveSerializationRaceException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

public sealed class ProductiveCommitOutcomeUnknownException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

public sealed class ProductivePersistenceUnavailableException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

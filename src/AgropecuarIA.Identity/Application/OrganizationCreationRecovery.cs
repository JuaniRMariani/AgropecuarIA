using AgropecuarIA.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Identity.Application;

public interface IOrganizationCreationCommitBoundary
{
    Task CommitAsync(
        Func<CancellationToken, Task> commit,
        Func<CancellationToken, Task> rollback,
        CancellationToken cancellationToken);
}

public sealed class OrganizationCreationCommitBoundary : IOrganizationCreationCommitBoundary
{
    public Task CommitAsync(
        Func<CancellationToken, Task> commit,
        Func<CancellationToken, Task> rollback,
        CancellationToken cancellationToken) =>
        commit(cancellationToken);
}

public interface IOrganizationCreationRecoveryContextFactory
{
    ValueTask<IdentityDbContext> CreateDbContextAsync(CancellationToken cancellationToken);
}

public sealed class OrganizationCreationRecoveryContextFactory(
    IdentityDbContext currentDbContext) : IOrganizationCreationRecoveryContextFactory
{
    private readonly string connectionString =
        currentDbContext.Database.GetConnectionString()
        ?? throw new InvalidOperationException(
            "The identity connection string is required for commit reconciliation.");

    public ValueTask<IdentityDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DbContextOptions<IdentityDbContext> options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(connectionString)
                .Options;
        return ValueTask.FromResult(new IdentityDbContext(options));
    }
}

public sealed class OrganizationCommitOutcomeUnknownException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);

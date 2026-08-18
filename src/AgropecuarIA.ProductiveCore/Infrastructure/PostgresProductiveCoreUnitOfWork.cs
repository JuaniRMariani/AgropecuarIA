using System.Data;
using System.Security.Cryptography;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace AgropecuarIA.ProductiveCore.Infrastructure;

public sealed class PostgresProductiveCoreUnitOfWorkFactory(
    IDbContextFactory<ProductiveCoreDbContext> dbContextFactory)
    : IProductiveCoreUnitOfWorkFactory
{
    public async ValueTask<IProductiveCoreUnitOfWork> BeginAsync(
        ProductiveTransactionMode mode,
        CancellationToken cancellationToken)
    {
        ProductiveCoreDbContext? dbContext = null;
        IDbContextTransaction? transaction = null;
        try
        {
            dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            transaction = await dbContext.Database.BeginTransactionAsync(
                mode == ProductiveTransactionMode.SerializableWrite
                    ? IsolationLevel.Serializable
                    : IsolationLevel.ReadCommitted,
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET LOCAL ROLE agro_productive_app",
                cancellationToken);
            return new PostgresProductiveCoreUnitOfWork(dbContext, transaction);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }

            if (dbContext is not null)
            {
                await dbContext.DisposeAsync();
            }

            throw new ProductivePersistenceUnavailableException(
                "Productive Core could not begin a database transaction.",
                exception);
        }
    }

    private static bool IsPersistenceFailure(Exception exception) =>
        exception is NpgsqlException or InvalidOperationException or TimeoutException;
}

internal sealed class PostgresProductiveCoreUnitOfWork(
    ProductiveCoreDbContext dbContext,
    IDbContextTransaction transaction) : IProductiveCoreUnitOfWork
{
    private bool completed;

    public async Task<Guid?> AuthorizeOwnerAsync(
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.current_scope_kind', {"tenant"}, true)",
                cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.current_actor_id', {requestContext.ActorUserId.ToString("D")}, true)",
                cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.current_organization_id', {requestContext.OrganizationId.ToString("D")}, true)",
                cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.current_session_id', {requestContext.SessionId.ToString("D")}, true)",
                cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.current_authorization_version', {string.Empty}, true)",
                cancellationToken);

            Guid? authorizationVersion = await ExecuteAuthorizationPortAsync(cancellationToken);
            if (authorizationVersion is not null)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT set_config('app.current_authorization_version', {authorizationVersion.Value.ToString("D")}, true)",
                    cancellationToken);
            }

            return authorizationVersion;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw new ProductivePersistenceUnavailableException(
                "Productive Core authorization could not be revalidated.",
                exception);
        }
    }

    public async Task<bool> RetainedKeyVersionsCoveredAsync(
        IReadOnlyCollection<string> retainedKeyVersions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(retainedKeyVersions);
        try
        {
            NpgsqlConnection connection = await GetOpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT productive_core.management_unit_creation_retained_key_covered(@versions)",
                connection,
                GetTransaction());
            command.Parameters.AddWithValue(
                "versions",
                NpgsqlDbType.Array | NpgsqlDbType.Text,
                retainedKeyVersions.ToArray());
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw new ProductivePersistenceUnavailableException(
                "Productive Core idempotency key coverage could not be verified.",
                exception);
        }
    }

    public async Task<Guid?> FindCreationLedgerIdAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        if (aliases.Count == 0)
        {
            return null;
        }

        try
        {
            NpgsqlConnection connection = await GetOpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand
            {
                Connection = connection,
                Transaction = GetTransaction(),
            };
            command.CommandText = BuildAliasLookup(command, organizationId, aliases);
            var ledgerIds = new List<Guid>(2);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ledgerIds.Add(reader.GetGuid(0));
            }

            if (ledgerIds.Count > 1)
            {
                throw new ProductiveIdempotencyRaceException(
                    "Idempotency aliases resolved to more than one ledger.");
            }

            return ledgerIds.Count == 1 ? ledgerIds[0] : null;
        }
        catch (ProductiveIdempotencyRaceException)
        {
            throw;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw new ProductivePersistenceUnavailableException(
                "Productive Core could not resolve the idempotency ledger.",
                exception);
        }
    }

    public async Task<ManagementUnitCreationLedger?> GetCreationLedgerAsync(
        Guid organizationId,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ManagementUnitCreationLedgers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.OrganizationId == organizationId && item.Id == ledgerId,
                    cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("read the creation ledger", exception);
        }
    }

    public async Task<ManagementUnit?> GetManagementUnitAsync(
        Guid organizationId,
        Guid managementUnitId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ManagementUnits
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.OrganizationId == organizationId && item.Id == managementUnitId,
                    cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("read the management unit", exception);
        }
    }

    public async Task<IReadOnlyList<ManagementUnit>> ListManagementUnitsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ManagementUnits
                .AsNoTracking()
                .Where(item => item.OrganizationId == organizationId)
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.Id)
                .Take(ManagementUnitLimits.MaximumPerOrganization + 1)
                .ToArrayAsync(cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("list management units", exception);
        }
    }

    public async Task<int> CountManagementUnitsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ManagementUnits.CountAsync(
                item => item.OrganizationId == organizationId,
                cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("count management units", exception);
        }
    }

    public async Task<ManagementUnit?> GetManagementUnitForUpdateAsync(
        Guid organizationId,
        Guid managementUnitId,
        CancellationToken cancellationToken)
    {
        try
        {
            ManagementUnit[] matches = await dbContext.ManagementUnits
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM productive_core.management_units
                    WHERE "OrganizationId" = {organizationId}
                      AND "Id" = {managementUnitId}
                    FOR UPDATE
                    """)
                .ToArrayAsync(cancellationToken);
            return matches.SingleOrDefault();
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            throw new ProductiveSerializationRaceException(
                "A Productive Core management unit lock lost serialization.",
                exception);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("lock the management unit for update", exception);
        }
    }

    public async Task<bool> RetainedRenameKeyVersionsCoveredAsync(
        IReadOnlyCollection<string> retainedKeyVersions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(retainedKeyVersions);
        try
        {
            NpgsqlConnection connection = await GetOpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT productive_core.management_unit_rename_retained_key_covered(@versions)",
                connection,
                GetTransaction());
            command.Parameters.AddWithValue(
                "versions",
                NpgsqlDbType.Array | NpgsqlDbType.Text,
                retainedKeyVersions.ToArray());
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw new ProductivePersistenceUnavailableException(
                "Productive Core rename idempotency key coverage could not be verified.",
                exception);
        }
    }

    public Task<Guid?> FindRenameLedgerIdAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken) =>
        FindLedgerIdAsync(
            organizationId,
            aliases,
            "management_unit_rename_key_aliases",
            "rename idempotency ledger",
            cancellationToken);

    public async Task<ManagementUnitRenameLedger?> GetRenameLedgerAsync(
        Guid organizationId,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.ManagementUnitRenameLedgers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.OrganizationId == organizationId && item.Id == ledgerId,
                    cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("read the rename ledger", exception);
        }
    }

    public void AddCreation(
        ManagementUnit managementUnit,
        ManagementUnitCreationLedger ledger,
        IReadOnlyCollection<ManagementUnitCreationKeyAlias> aliases,
        ProductiveJournalEntry journalEntry,
        ProductiveOutboxMessage outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(managementUnit);
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(aliases);
        ArgumentNullException.ThrowIfNull(journalEntry);
        ArgumentNullException.ThrowIfNull(outboxMessage);
        dbContext.ManagementUnits.Add(managementUnit);
        dbContext.ManagementUnitCreationLedgers.Add(ledger);
        dbContext.ManagementUnitCreationKeyAliases.AddRange(aliases);
        dbContext.ProductiveJournalEntries.Add(journalEntry);
        dbContext.ProductiveOutboxMessages.Add(outboxMessage);
    }

    public async Task AddMissingAliasesAsync(
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
            ManagementUnitCreationKeyAlias[] existing = await dbContext
                .ManagementUnitCreationKeyAliases
                .AsNoTracking()
                .Where(item => item.OrganizationId == organizationId &&
                    item.LedgerId == ledgerId &&
                    versions.Contains(item.KeyVersion))
                .ToArrayAsync(cancellationToken);
            foreach ((string version, byte[] digest) in aliases)
            {
                ManagementUnitCreationKeyAlias? current = existing.SingleOrDefault(
                    item => string.Equals(item.KeyVersion, version, StringComparison.Ordinal));
                if (current is not null)
                {
                    if (!CryptographicOperations.FixedTimeEquals(current.KeyDigest, digest))
                    {
                        throw new ProductiveIdempotencyRaceException(
                            "An idempotency key version is already bound to another digest.");
                    }

                    continue;
                }

                dbContext.ManagementUnitCreationKeyAliases.Add(
                    new ManagementUnitCreationKeyAlias(
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
            throw Unavailable("reconcile idempotency key aliases", exception);
        }
    }

    public void AddRename(
        ManagementUnitRenameLedger ledger,
        IReadOnlyCollection<ManagementUnitRenameKeyAlias> aliases,
        ProductiveJournalEntry journalEntry,
        ProductiveOutboxMessage outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(aliases);
        ArgumentNullException.ThrowIfNull(journalEntry);
        ArgumentNullException.ThrowIfNull(outboxMessage);
        dbContext.ManagementUnitRenameLedgers.Add(ledger);
        dbContext.ManagementUnitRenameKeyAliases.AddRange(aliases);
        dbContext.ProductiveJournalEntries.Add(journalEntry);
        dbContext.ProductiveOutboxMessages.Add(outboxMessage);
    }

    public async Task AddMissingRenameAliasesAsync(
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
            ManagementUnitRenameKeyAlias[] existing = await dbContext
                .ManagementUnitRenameKeyAliases
                .AsNoTracking()
                .Where(item => item.OrganizationId == organizationId &&
                    item.LedgerId == ledgerId &&
                    versions.Contains(item.KeyVersion))
                .ToArrayAsync(cancellationToken);
            foreach ((string version, byte[] digest) in aliases)
            {
                ManagementUnitRenameKeyAlias? current = existing.SingleOrDefault(
                    item => string.Equals(item.KeyVersion, version, StringComparison.Ordinal));
                if (current is not null)
                {
                    if (!CryptographicOperations.FixedTimeEquals(current.KeyDigest, digest))
                    {
                        throw new ProductiveIdempotencyRaceException(
                            "A rename idempotency key version is already bound to another digest.");
                    }

                    continue;
                }

                dbContext.ManagementUnitRenameKeyAliases.Add(
                    new ManagementUnitRenameKeyAlias(
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
            throw Unavailable("reconcile rename idempotency key aliases", exception);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ProductiveStaleVersionException(
                "The Productive Core management unit version is stale.",
                exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
            })
        {
            throw new ProductiveIdempotencyRaceException(
                "A Productive Core idempotency uniqueness race occurred.",
                exception);
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            throw new ProductiveSerializationRaceException(
                "A Productive Core serialization race occurred.",
                exception);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("save the transaction", exception);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (completed)
        {
            return;
        }

        try
        {
            await transaction.CommitAsync(cancellationToken);
            completed = true;
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.SerializationFailure ||
            exception.SqlState == PostgresErrorCodes.DeadlockDetected)
        {
            throw new ProductiveSerializationRaceException(
                "A Productive Core transaction lost serialization.",
                exception);
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ProductiveIdempotencyRaceException(
                "A Productive Core idempotency uniqueness race occurred at commit.",
                exception);
        }
        catch (NpgsqlException exception)
        {
            throw new ProductiveCommitOutcomeUnknownException(
                "The Productive Core commit outcome is unknown.",
                exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new ProductiveCommitOutcomeUnknownException(
                "The Productive Core commit outcome is unknown.",
                exception);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (completed)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync(cancellationToken);
            completed = true;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("roll back the transaction", exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
        await dbContext.DisposeAsync();
    }

    private async Task<Guid?> ExecuteAuthorizationPortAsync(CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = await GetOpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT identity.authorize_productive_owner()",
            connection,
            GetTransaction());
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid version ? version : null;
    }

    private async Task<NpgsqlConnection> GetOpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.GetDbConnection() is not NpgsqlConnection connection)
        {
            throw new ProductivePersistenceUnavailableException(
                "Productive Core requires an Npgsql connection.");
        }

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private NpgsqlTransaction GetTransaction() =>
        transaction.GetDbTransaction() as NpgsqlTransaction
        ?? throw new ProductivePersistenceUnavailableException(
            "Productive Core requires an Npgsql transaction.");

    private static string BuildAliasLookup(
        NpgsqlCommand command,
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases)
    {
        command.Parameters.AddWithValue("organizationId", organizationId);
        var predicates = new List<string>(aliases.Count);
        int index = 0;
        foreach ((string version, byte[] digest) in aliases)
        {
            string versionParameter = $"version{index}";
            string digestParameter = $"digest{index}";
            predicates.Add(
                $"(\"KeyVersion\" = @{versionParameter} AND \"KeyDigest\" = @{digestParameter})");
            command.Parameters.AddWithValue(versionParameter, NpgsqlDbType.Text, version);
            command.Parameters.AddWithValue(digestParameter, NpgsqlDbType.Bytea, digest);
            index++;
        }

        return $"""
            SELECT DISTINCT "LedgerId"
            FROM productive_core.management_unit_creation_key_aliases
            WHERE "OrganizationId" = @organizationId
              AND ({string.Join(" OR ", predicates)})
            LIMIT 2
            """;
    }

    private async Task<Guid?> FindLedgerIdAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        string aliasTable,
        string operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        if (aliases.Count == 0)
        {
            return null;
        }

        if (!string.Equals(
                aliasTable,
                "management_unit_rename_key_aliases",
                StringComparison.Ordinal))
        {
            throw new ArgumentException("Unexpected Productive Core alias table.", nameof(aliasTable));
        }

        try
        {
            NpgsqlConnection connection = await GetOpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand
            {
                Connection = connection,
                Transaction = GetTransaction(),
            };
            command.CommandText = BuildAliasLookup(
                command,
                organizationId,
                aliases,
                aliasTable);
            var ledgerIds = new List<Guid>(2);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ledgerIds.Add(reader.GetGuid(0));
            }

            if (ledgerIds.Count > 1)
            {
                throw new ProductiveIdempotencyRaceException(
                    "Idempotency aliases resolved to more than one ledger.");
            }

            return ledgerIds.Count == 1 ? ledgerIds[0] : null;
        }
        catch (ProductiveIdempotencyRaceException)
        {
            throw;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable($"resolve the {operation}", exception);
        }
    }

    private static string BuildAliasLookup(
        NpgsqlCommand command,
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        string aliasTable)
    {
        command.Parameters.AddWithValue("organizationId", organizationId);
        var predicates = new List<string>(aliases.Count);
        int index = 0;
        foreach ((string version, byte[] digest) in aliases)
        {
            string versionParameter = $"version{index}";
            string digestParameter = $"digest{index}";
            predicates.Add(
                $"(\"KeyVersion\" = @{versionParameter} AND \"KeyDigest\" = @{digestParameter})");
            command.Parameters.AddWithValue(versionParameter, NpgsqlDbType.Text, version);
            command.Parameters.AddWithValue(digestParameter, NpgsqlDbType.Bytea, digest);
            index++;
        }

        return $"""
            SELECT DISTINCT "LedgerId"
            FROM productive_core.{aliasTable}
            WHERE "OrganizationId" = @organizationId
              AND ({string.Join(" OR ", predicates)})
            LIMIT 2
            """;
    }

    private static bool IsSerializationFailure(Exception exception) =>
        exception is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or
                PostgresErrorCodes.DeadlockDetected,
        } || exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or
                PostgresErrorCodes.DeadlockDetected,
        };

    private static bool IsPersistenceFailure(Exception exception) =>
        exception is NpgsqlException or DbUpdateException or InvalidOperationException or
            TimeoutException;

    private static ProductivePersistenceUnavailableException Unavailable(
        string operation,
        Exception exception) =>
        new($"Productive Core could not {operation}.", exception);
}

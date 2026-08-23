using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed class ProductiveCoreArchiveApplicationService(
    IProductiveCoreUnitOfWorkFactory unitOfWorkFactory,
    ProductiveCoreTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<ManagementUnitRenameOptions> configuredOptions)
{
    private readonly ManagementUnitRenameOptions options = configuredOptions.Value;

    public Task<ArchivedManagementUnitResult> ArchiveFieldDraftAsync(
        ArchiveFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken) =>
        ArchiveFieldDraftAsync(command, requestContext, retryAfterRace: true, cancellationToken);

    private async Task<ArchivedManagementUnitResult> ArchiveFieldDraftAsync(
        ArchiveFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        bool retryAfterRace,
        CancellationToken cancellationToken)
    {
        RequireRequestScope(command, requestContext);
        ValidateIdempotencyKey(command.IdempotencyKey);
        
        

        using Activity? activity = ProductiveCoreTelemetry.Start("productive_core.field_archive");
        DateTimeOffset now = ToPostgresPrecision(timeProvider.GetUtcNow());
        await using IProductiveCoreUnitOfWork unitOfWork = await BeginAsync(cancellationToken);
        try
        {
            Guid? authorizationVersion = await unitOfWork.AuthorizeOwnerAsync(
                requestContext,
                cancellationToken);
            if (authorizationVersion is null)
            {
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            Dictionary<string, byte[]> keyRing = GetKeyRing();
            if (!await unitOfWork.RetainedRenameKeyVersionsCoveredAsync(
                    keyRing.Keys.ToArray(),
                    cancellationToken))
            {
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            Dictionary<string, byte[]> aliases = CreateAliases(
                command.OrganizationId,
                command.IdempotencyKey,
                keyRing);
            byte[] fingerprint = CreateRequestFingerprint(
                command,
                
                requestContext,
                authorizationVersion.Value);
            Guid? existingLedgerId = await unitOfWork.FindArchiveLedgerIdAsync(
                command.OrganizationId,
                aliases,
                cancellationToken);
            if (existingLedgerId is not null)
            {
                ArchivedManagementUnitResult replay = await ResolveReplayAsync(
                    unitOfWork,
                    command,
                    requestContext,
                    authorizationVersion.Value,
                    existingLedgerId.Value,
                    fingerprint,
                    now,
                    cancellationToken);
                await unitOfWork.AddMissingArchiveAliasesAsync(
                    command.OrganizationId,
                    existingLedgerId.Value,
                    aliases,
                    now,
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                telemetry.Record("field_archive", "replayed");
                return replay;
            }

            ManagementUnit field = await unitOfWork.GetManagementUnitForUpdateAsync(
                command.OrganizationId,
                command.FieldId,
                cancellationToken)
                ?? throw ProductiveCoreErrors.FieldNotAvailable();
            if (field.Version != command.ExpectedVersion)
            {
                throw ProductiveCoreErrors.FieldVersionStale();
            }

            

            Guid resultVersion = Guid.NewGuid();
            field.Archive(command.ExpectedVersion, resultVersion);
            Guid ledgerId = Guid.NewGuid();
            Guid leaseOwner = Guid.NewGuid();
            var ledger = new ManagementUnitArchiveLedger(
                ledgerId,
                command.OrganizationId,
                requestContext.ActorUserId,
                requestContext.SessionId,
                authorizationVersion.Value,
                field.Id,
                command.ExpectedVersion,
                fingerprint,
                leaseOwner,
                now,
                now.Add(ValidLeaseLifetime()));
            ledger.Complete(
                leaseOwner,
                ledger.FenceToken,
                field.Version,
                field.Revision,
                now);
            ManagementUnitArchiveKeyAlias[] keyAliases = aliases
                .OrderBy(alias => alias.Key, StringComparer.Ordinal)
                .Select(alias => new ManagementUnitArchiveKeyAlias(
                    Guid.NewGuid(),
                    ledger.Id,
                    command.OrganizationId,
                    alias.Key,
                    alias.Value,
                    now))
                .ToArray();
            ProductiveJournalEntry journal =
                ProductiveJournalEntry.CreateManagementUnitDisplayNameChanged(
                    Guid.NewGuid(),
                    command.OrganizationId,
                    requestContext.ActorUserId,
                    requestContext.SessionId,
                    requestContext.CorrelationId,
                    now);
            ProductiveOutboxMessage outbox =
                ProductiveOutboxMessage.CreateManagementUnitDisplayNameChanged(
                    Guid.NewGuid(),
                    requestContext.CorrelationId,
                    new ManagementUnitDisplayNameChangedIntegrationEventPayload(
                        command.OrganizationId,
                        field.Id,
                        field.Revision,
                        now));
            unitOfWork.AddArchive(ledger, keyAliases, journal, outbox);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            telemetry.Record("field_archive", "succeeded");
            return ToResult(field, isReplay: false);
        }
        catch (ManagementUnitVersionConflictException)
        {
            telemetry.Record("field_archive", "stale");
            throw ProductiveCoreErrors.FieldVersionStale();
        }
        catch (ProductiveStaleVersionException)
        {
            telemetry.Record("field_archive", "stale");
            throw ProductiveCoreErrors.FieldVersionStale();
        }
        catch (ProductiveIdempotencyRaceException)
        {
            await SafeRollbackAsync(unitOfWork, cancellationToken);
            telemetry.Record("field_archive", "race");
            return await ResolveRaceAsync(
                command,
                requestContext,
                
                missingIsConflict: true,
                cancellationToken);
        }
        catch (ProductiveSerializationRaceException)
        {
            await SafeRollbackAsync(unitOfWork, cancellationToken);
            if (retryAfterRace)
            {
                return await ArchiveFieldDraftAsync(
                    command,
                    requestContext,
                    retryAfterRace: false,
                    cancellationToken);
            }

            telemetry.Record("field_archive", "reconciliation_required");
            throw ProductiveCoreErrors.ReconciliationRequired();
        }
        catch (ProductiveCommitOutcomeUnknownException)
        {
            telemetry.Record("field_archive", "commit_unknown");
            return await RecoverUnknownCommitAsync(
                command,
                requestContext,
                
                retryAfterRace,
                cancellationToken);
        }
        catch (ProductivePersistenceUnavailableException)
        {
            telemetry.Record("field_archive", "unavailable");
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
        catch (ProductiveCoreOperationException exception)
        {
            telemetry.Record("field_archive", OutcomeFor(exception.Code));
            throw;
        }
    }

    private async Task<ArchivedManagementUnitResult> ResolveRaceAsync(
        ArchiveFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        
        bool missingIsConflict,
        CancellationToken cancellationToken)
    {
        await using IProductiveCoreUnitOfWork recovery = await BeginAsync(cancellationToken);
        try
        {
            Guid? authorizationVersion = await recovery.AuthorizeOwnerAsync(
                requestContext,
                cancellationToken);
            if (authorizationVersion is null)
            {
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            Dictionary<string, byte[]> keyRing = GetKeyRing();
            if (!await recovery.RetainedRenameKeyVersionsCoveredAsync(
                    keyRing.Keys.ToArray(),
                    cancellationToken))
            {
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            Dictionary<string, byte[]> aliases = CreateAliases(
                command.OrganizationId,
                command.IdempotencyKey,
                keyRing);
            Guid? ledgerId = await recovery.FindArchiveLedgerIdAsync(
                command.OrganizationId,
                aliases,
                cancellationToken);
            if (ledgerId is null)
            {
                throw missingIsConflict
                    ? ProductiveCoreErrors.IdempotencyKeyReused()
                    : ProductiveCoreErrors.ReconciliationRequired();
            }

            byte[] fingerprint = CreateRequestFingerprint(
                command,
                
                requestContext,
                authorizationVersion.Value);
            ArchivedManagementUnitResult result = await ResolveReplayAsync(
                recovery,
                command,
                requestContext,
                authorizationVersion.Value,
                ledgerId.Value,
                fingerprint,
                ToPostgresPrecision(timeProvider.GetUtcNow()),
                cancellationToken);
            await recovery.CommitAsync(cancellationToken);
            telemetry.Record("field_archive", "replayed");
            return result;
        }
        catch (ProductivePersistenceUnavailableException)
        {
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
        catch (ProductiveIdempotencyRaceException)
        {
            telemetry.Record("field_archive", "reconciliation_required");
            throw ProductiveCoreErrors.ReconciliationRequired();
        }
    }

    private async Task<ArchivedManagementUnitResult> RecoverUnknownCommitAsync(
        ArchiveFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        
        bool mayRetry,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ResolveRaceAsync(
                command,
                requestContext,
                
                missingIsConflict: false,
                cancellationToken);
        }
        catch (ProductiveCoreOperationException exception) when (
            exception.Code == "idempotency.reconciliation_required" && mayRetry)
        {
            return await ArchiveFieldDraftAsync(
                command,
                requestContext,
                retryAfterRace: false,
                cancellationToken);
        }
        catch (ProductiveCoreOperationException)
        {
            throw;
        }
        catch (ProductiveCommitOutcomeUnknownException)
        {
            throw ProductiveCoreErrors.ReconciliationRequired();
        }
    }

    private static async Task<ArchivedManagementUnitResult> ResolveReplayAsync(
        IProductiveCoreUnitOfWork unitOfWork,
        ArchiveFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        Guid authorizationVersion,
        Guid ledgerId,
        byte[] fingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ManagementUnitArchiveLedger? ledger = await unitOfWork.GetArchiveLedgerAsync(
            command.OrganizationId,
            ledgerId,
            cancellationToken);
        if (ledger is null ||
            ledger.ActorUserId != requestContext.ActorUserId ||
            ledger.SessionId != requestContext.SessionId ||
            ledger.AuthorizationVersion != authorizationVersion ||
            ledger.ManagementUnitId != command.FieldId ||
            ledger.ExpectedVersion != command.ExpectedVersion ||
            !CryptographicOperations.FixedTimeEquals(ledger.RequestFingerprint, fingerprint))
        {
            throw ProductiveCoreErrors.IdempotencyKeyReused();
        }

        if (ledger.State == ManagementUnitArchiveProtocol.States.InProgress)
        {
            throw now < ledger.LeaseUntilUtc
                ? ProductiveCoreErrors.IdempotencyInProgress()
                : ProductiveCoreErrors.ReconciliationRequired();
        }

        if (ledger.State == ManagementUnitArchiveProtocol.States.FailedTerminal)
        {
            throw ProductiveCoreErrors.IdempotencyFailedTerminal();
        }

        if (ledger.State != ManagementUnitArchiveProtocol.States.Succeeded ||
            
            ledger.ResultVersion is null ||
            ledger.ResultRevision is null)
        {
            throw ProductiveCoreErrors.ReconciliationRequired();
        }

        ManagementUnit? field = await unitOfWork.GetManagementUnitAsync(
            command.OrganizationId,
            command.FieldId,
            cancellationToken);
        if (field is null)
        {
            throw ProductiveCoreErrors.ReconciliationRequired();
        }

        return new ArchivedManagementUnitResult(
            field.Id,
            field.OrganizationId,
            field.DisplayName,
            field.UnitType,
            field.Status,
            field.SpatialStatus,
            field.CreatedAtUtc,
            ledger.ResultRevision.Value,
            ledger.ResultVersion.Value,
            IsReplay: true);
    }

    private Dictionary<string, byte[]> GetKeyRing()
    {
        if (!options.Enabled ||
            options.HmacKeys.Count is < 1 or > 8 ||
            string.IsNullOrWhiteSpace(options.CurrentKeyVersion) ||
            !options.HmacKeys.ContainsKey(options.CurrentKeyVersion))
        {
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }

        Dictionary<string, byte[]> decoded = new(StringComparer.Ordinal);
        HashSet<string> uniqueKeys = new(StringComparer.Ordinal);
        foreach ((string version, string encodedKey) in options.HmacKeys)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32 ||
                string.IsNullOrWhiteSpace(encodedKey))
            {
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(encodedKey);
            }
            catch (FormatException)
            {
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            if (key.Length < 32 || !uniqueKeys.Add(Convert.ToHexString(key)))
            {
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            decoded.Add(version, key);
        }

        return decoded;
    }

    private static Dictionary<string, byte[]> CreateAliases(
        Guid organizationId,
        string idempotencyKey,
        IReadOnlyDictionary<string, byte[]> keyRing)
    {
        byte[] message = Encoding.ASCII.GetBytes(string.Join(
            '|',
            "rename-field-idempotency-v1",
            organizationId.ToString("D"),
            idempotencyKey));
        Dictionary<string, byte[]> aliases = new(StringComparer.Ordinal);
        foreach ((string version, byte[] key) in keyRing)
        {
            aliases.Add(version, HMACSHA256.HashData(key, message));
        }

        return aliases;
    }

    private static byte[] CreateRequestFingerprint(
        ArchiveFieldDraftCommand command,
        
        ProductiveRequestContext requestContext,
        Guid authorizationVersion)
    {
        string canonical = string.Join(
            '|',
            "rename-field-v1",
            command.OrganizationId.ToString("D"),
            command.FieldId.ToString("D"),
            command.ExpectedVersion.ToString("D"),
            requestContext.ActorUserId.ToString("D"),
            requestContext.SessionId.ToString("D"),
            authorizationVersion.ToString("D"),
            "archive");
        return SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
    }

    private static void ValidateIdempotencyKey(string? idempotencyKey)
    {
        if (idempotencyKey is null ||
            idempotencyKey.Length is < 32 or > 128 ||
            idempotencyKey.Any(character =>
                character is not (>= 'A' and <= 'Z') and
                    not (>= 'a' and <= 'z') and
                    not (>= '0' and <= '9') and
                    not '_' and
                    not '-'))
        {
            throw ProductiveCoreErrors.InvalidIdempotencyKey();
        }
    }

    private static void RequireRequestScope(
        ArchiveFieldDraftCommand command,
        ProductiveRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        if (command.OrganizationId == Guid.Empty ||
            command.FieldId == Guid.Empty ||
            command.ExpectedVersion == Guid.Empty ||
            command.OrganizationId != requestContext.OrganizationId)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }
    }

    private async ValueTask<IProductiveCoreUnitOfWork> BeginAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWorkFactory.BeginAsync(
                ProductiveTransactionMode.SerializableWrite,
                cancellationToken);
        }
        catch (ProductivePersistenceUnavailableException)
        {
            telemetry.Record("field_archive", "unavailable");
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
    }

    private TimeSpan ValidLeaseLifetime()
    {
        if (options.LeaseLifetime < TimeSpan.FromSeconds(10) ||
            options.LeaseLifetime > TimeSpan.FromMinutes(5))
        {
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }

        return options.LeaseLifetime;
    }

    private static ArchivedManagementUnitResult ToResult(
        ManagementUnit field,
        bool isReplay) =>
        new(
            field.Id,
            field.OrganizationId,
            field.DisplayName,
            field.UnitType,
            field.Status,
            field.SpatialStatus,
            field.CreatedAtUtc,
            field.Revision,
            field.Version,
            isReplay);

    private static DateTimeOffset ToPostgresPrecision(DateTimeOffset value)
    {
        DateTimeOffset utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - (utc.Ticks % 10), TimeSpan.Zero);
    }

    private static async Task SafeRollbackAsync(
        IProductiveCoreUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.RollbackAsync(cancellationToken);
        }
        catch (ProductivePersistenceUnavailableException)
        {
            // A fresh unit of work performs recovery; the current operation remains fail-closed.
        }
    }

    private static string OutcomeFor(string code) => code switch
    {
        "productive_core.field_not_available" => "not_available",
        "productive_core.field_version_stale" => "stale",
        "productive_core.management_unit_unavailable" => "unavailable",
        "idempotency.key_reused" => "conflict",
        "idempotency.in_progress" => "in_progress",
        "idempotency.failed_terminal" => "failed_terminal",
        "idempotency.reconciliation_required" => "reconciliation_required",
        _ => "rejected",
    };
}

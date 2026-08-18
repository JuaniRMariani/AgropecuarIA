using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed class ProductiveCoreRenameApplicationService(
    IProductiveCoreUnitOfWorkFactory unitOfWorkFactory,
    ProductiveCoreTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<ManagementUnitRenameOptions> configuredOptions)
{
    private readonly ManagementUnitRenameOptions options = configuredOptions.Value;

    public Task<RenamedManagementUnitResult> RenameFieldDraftAsync(
        RenameFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken) =>
        RenameFieldDraftAsync(command, requestContext, retryAfterRace: true, cancellationToken);

    private async Task<RenamedManagementUnitResult> RenameFieldDraftAsync(
        RenameFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        bool retryAfterRace,
        CancellationToken cancellationToken)
    {
        RequireRequestScope(command, requestContext);
        ValidateIdempotencyKey(command.IdempotencyKey);
        string displayName;
        try
        {
            displayName = ManagementUnit.NormalizeDisplayName(command.DisplayName);
        }
        catch (ArgumentException)
        {
            throw ProductiveCoreErrors.InvalidFieldDisplayName();
        }

        using Activity? activity = ProductiveCoreTelemetry.Start("productive_core.field_rename");
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
                displayName,
                requestContext,
                authorizationVersion.Value);
            Guid? existingLedgerId = await unitOfWork.FindRenameLedgerIdAsync(
                command.OrganizationId,
                aliases,
                cancellationToken);
            if (existingLedgerId is not null)
            {
                RenamedManagementUnitResult replay = await ResolveReplayAsync(
                    unitOfWork,
                    command,
                    requestContext,
                    authorizationVersion.Value,
                    existingLedgerId.Value,
                    fingerprint,
                    now,
                    cancellationToken);
                await unitOfWork.AddMissingRenameAliasesAsync(
                    command.OrganizationId,
                    existingLedgerId.Value,
                    aliases,
                    now,
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                telemetry.Record("field_rename", "replayed");
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

            if (string.Equals(field.DisplayName, displayName, StringComparison.Ordinal))
            {
                throw ProductiveCoreErrors.FieldDisplayNameUnchanged();
            }

            Guid resultVersion = Guid.NewGuid();
            field.Rename(displayName, command.ExpectedVersion, resultVersion);
            Guid ledgerId = Guid.NewGuid();
            Guid leaseOwner = Guid.NewGuid();
            var ledger = new ManagementUnitRenameLedger(
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
                field.DisplayName,
                field.Version,
                field.Revision,
                now);
            ManagementUnitRenameKeyAlias[] keyAliases = aliases
                .OrderBy(alias => alias.Key, StringComparer.Ordinal)
                .Select(alias => new ManagementUnitRenameKeyAlias(
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
            unitOfWork.AddRename(ledger, keyAliases, journal, outbox);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            telemetry.Record("field_rename", "succeeded");
            return ToResult(field, isReplay: false);
        }
        catch (ManagementUnitVersionConflictException)
        {
            telemetry.Record("field_rename", "stale");
            throw ProductiveCoreErrors.FieldVersionStale();
        }
        catch (ProductiveStaleVersionException)
        {
            telemetry.Record("field_rename", "stale");
            throw ProductiveCoreErrors.FieldVersionStale();
        }
        catch (ProductiveIdempotencyRaceException)
        {
            await SafeRollbackAsync(unitOfWork, cancellationToken);
            telemetry.Record("field_rename", "race");
            return await ResolveRaceAsync(
                command,
                requestContext,
                displayName,
                missingIsConflict: true,
                cancellationToken);
        }
        catch (ProductiveSerializationRaceException)
        {
            await SafeRollbackAsync(unitOfWork, cancellationToken);
            if (retryAfterRace)
            {
                return await RenameFieldDraftAsync(
                    command,
                    requestContext,
                    retryAfterRace: false,
                    cancellationToken);
            }

            telemetry.Record("field_rename", "reconciliation_required");
            throw ProductiveCoreErrors.ReconciliationRequired();
        }
        catch (ProductiveCommitOutcomeUnknownException)
        {
            telemetry.Record("field_rename", "commit_unknown");
            return await RecoverUnknownCommitAsync(
                command,
                requestContext,
                displayName,
                retryAfterRace,
                cancellationToken);
        }
        catch (ProductivePersistenceUnavailableException)
        {
            telemetry.Record("field_rename", "unavailable");
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
        catch (ProductiveCoreOperationException exception)
        {
            telemetry.Record("field_rename", OutcomeFor(exception.Code));
            throw;
        }
    }

    private async Task<RenamedManagementUnitResult> ResolveRaceAsync(
        RenameFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        string displayName,
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
            Guid? ledgerId = await recovery.FindRenameLedgerIdAsync(
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
                displayName,
                requestContext,
                authorizationVersion.Value);
            RenamedManagementUnitResult result = await ResolveReplayAsync(
                recovery,
                command,
                requestContext,
                authorizationVersion.Value,
                ledgerId.Value,
                fingerprint,
                ToPostgresPrecision(timeProvider.GetUtcNow()),
                cancellationToken);
            await recovery.CommitAsync(cancellationToken);
            telemetry.Record("field_rename", "replayed");
            return result;
        }
        catch (ProductivePersistenceUnavailableException)
        {
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
        catch (ProductiveIdempotencyRaceException)
        {
            telemetry.Record("field_rename", "reconciliation_required");
            throw ProductiveCoreErrors.ReconciliationRequired();
        }
    }

    private async Task<RenamedManagementUnitResult> RecoverUnknownCommitAsync(
        RenameFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        string displayName,
        bool mayRetry,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ResolveRaceAsync(
                command,
                requestContext,
                displayName,
                missingIsConflict: false,
                cancellationToken);
        }
        catch (ProductiveCoreOperationException exception) when (
            exception.Code == "idempotency.reconciliation_required" && mayRetry)
        {
            return await RenameFieldDraftAsync(
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

    private static async Task<RenamedManagementUnitResult> ResolveReplayAsync(
        IProductiveCoreUnitOfWork unitOfWork,
        RenameFieldDraftCommand command,
        ProductiveRequestContext requestContext,
        Guid authorizationVersion,
        Guid ledgerId,
        byte[] fingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ManagementUnitRenameLedger? ledger = await unitOfWork.GetRenameLedgerAsync(
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

        if (ledger.State == ManagementUnitRenameProtocol.States.InProgress)
        {
            throw now < ledger.LeaseUntilUtc
                ? ProductiveCoreErrors.IdempotencyInProgress()
                : ProductiveCoreErrors.ReconciliationRequired();
        }

        if (ledger.State == ManagementUnitRenameProtocol.States.FailedTerminal)
        {
            throw ProductiveCoreErrors.IdempotencyFailedTerminal();
        }

        if (ledger.State != ManagementUnitRenameProtocol.States.Succeeded ||
            ledger.ResultDisplayName is null ||
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

        return new RenamedManagementUnitResult(
            field.Id,
            field.OrganizationId,
            ledger.ResultDisplayName,
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
        RenameFieldDraftCommand command,
        string displayName,
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
            Convert.ToBase64String(Encoding.UTF8.GetBytes(displayName)));
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
        RenameFieldDraftCommand command,
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
            telemetry.Record("field_rename", "unavailable");
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

    private static RenamedManagementUnitResult ToResult(
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

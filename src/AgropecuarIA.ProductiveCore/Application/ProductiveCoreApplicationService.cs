using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AgropecuarIA.ProductiveCore;
using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed class ProductiveCoreApplicationService(
    IProductiveCoreUnitOfWorkFactory unitOfWorkFactory,
    ProductiveCoreTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<ManagementUnitCreationOptions> creationOptions)
{
    private readonly ManagementUnitCreationOptions options = creationOptions.Value;

    public Task<CreatedManagementUnitResult> CreateFieldAsync(
        CreateFieldCommand command,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken) =>
        CreateFieldAsync(command, requestContext, retryAfterRace: true, cancellationToken);

    public async Task<IReadOnlyList<ManagementUnitResult>> ListFieldsAsync(
        Guid organizationId,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        RequireRequestScope(organizationId, requestContext);
        using Activity? activity = ProductiveCoreTelemetry.Start("productive_core.field_list");
        await using IProductiveCoreUnitOfWork unitOfWork =
            await BeginUnitOfWorkAsync(
                ProductiveTransactionMode.Read,
                "field_list",
                cancellationToken);
        try
        {
            Guid? authorizationVersion = await unitOfWork.AuthorizeOwnerAsync(
                requestContext,
                cancellationToken);
            if (authorizationVersion is null)
            {
                telemetry.Record("field_list", "not_available");
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            IReadOnlyList<ManagementUnit> fields = await unitOfWork.ListManagementUnitsAsync(
                organizationId,
                cancellationToken);
            if (fields.Count > ManagementUnitLimits.MaximumPerOrganization)
            {
                telemetry.Record("field_list", "unavailable");
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            await unitOfWork.CommitAsync(cancellationToken);
            telemetry.Record("field_list", "succeeded", fields.Count);
            return fields
                .OrderBy(field => field.CreatedAtUtc)
                .ThenBy(field => field.Id)
                .Select(ToResult)
                .ToArray();
        }
        catch (Exception exception) when (IsReadTransactionUnavailable(exception))
        {
            telemetry.Record("field_list", "unavailable");
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
    }

    public async Task<ManagementUnitResult> GetFieldAsync(
        Guid organizationId,
        Guid fieldId,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        RequireRequestScope(organizationId, requestContext);
        if (fieldId == Guid.Empty)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }

        using Activity? activity = ProductiveCoreTelemetry.Start("productive_core.field_detail");
        await using IProductiveCoreUnitOfWork unitOfWork =
            await BeginUnitOfWorkAsync(
                ProductiveTransactionMode.Read,
                "field_detail",
                cancellationToken);
        try
        {
            Guid? authorizationVersion = await unitOfWork.AuthorizeOwnerAsync(
                requestContext,
                cancellationToken);
            if (authorizationVersion is null)
            {
                telemetry.Record("field_detail", "not_available");
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            ManagementUnit? field = await unitOfWork.GetManagementUnitAsync(
                organizationId,
                fieldId,
                cancellationToken);
            if (field is null)
            {
                telemetry.Record("field_detail", "not_available");
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            await unitOfWork.CommitAsync(cancellationToken);
            telemetry.Record("field_detail", "succeeded");
            return ToResult(field);
        }
        catch (Exception exception) when (IsReadTransactionUnavailable(exception))
        {
            telemetry.Record("field_detail", "unavailable");
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
    }

    public async Task<ConfiguredFieldGeometryResult> ConfigureGeometryAsync(
        ConfigureFieldGeometryCommand command,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        RequireRequestScope(command.OrganizationId, requestContext);
        if (command.FieldId == Guid.Empty)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }

        using Activity? activity = ProductiveCoreTelemetry.Start("productive_core.field_geometry_configure");
        await using IProductiveCoreUnitOfWork unitOfWork =
            await BeginUnitOfWorkAsync(
                ProductiveTransactionMode.SerializableWrite,
                "field_geometry_configure",
                cancellationToken);
        try
        {
            Guid? authorizationVersion = await unitOfWork.AuthorizeOwnerAsync(
                requestContext,
                cancellationToken);
            if (authorizationVersion is null)
            {
                telemetry.Record("field_geometry_configure", "not_available");
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            ManagementUnit? field = await unitOfWork.GetManagementUnitForUpdateAsync(
                command.OrganizationId,
                command.FieldId,
                cancellationToken);
            if (field is null)
            {
                telemetry.Record("field_geometry_configure", "not_available");
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            Guid newVersion = Guid.NewGuid();
            field.ConfigureSpatialGeometry(
                command.BoundaryGeoJson,
                command.DeclaredAreaHectares,
                command.CalculatedAreaHectares,
                command.CentroidLatitude,
                command.CentroidLongitude,
                command.OfficialProvinceCode,
                command.OfficialDepartmentCode,
                command.ExpectedVersion,
                newVersion);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            telemetry.Record("field_geometry_configure", "succeeded");
            return new ConfiguredFieldGeometryResult(
                field.Id,
                field.OrganizationId,
                field.DisplayName,
                field.UnitType,
                field.Status,
                field.SpatialStatus,
                field.DeclaredAreaHectares,
                field.CalculatedAreaHectares,
                field.CentroidLatitude,
                field.CentroidLongitude,
                field.BoundaryGeoJson,
                field.OfficialProvinceCode,
                field.OfficialDepartmentCode,
                field.CreatedAtUtc,
                field.Revision,
                field.Version);
        }
        catch (ManagementUnitVersionConflictException)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            telemetry.Record("field_geometry_configure", "conflict");
            throw ProductiveCoreErrors.FieldVersionStale();
        }
        catch (Exception exception) when (IsReadTransactionUnavailable(exception))
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            telemetry.Record("field_geometry_configure", "unavailable");
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
    }

    private async Task<CreatedManagementUnitResult> CreateFieldAsync(
        CreateFieldCommand command,
        ProductiveRequestContext requestContext,
        bool retryAfterRace,
        CancellationToken cancellationToken)
    {
        RequireRequestScope(command.OrganizationId, requestContext);
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

        using Activity? activity = ProductiveCoreTelemetry.Start("productive_core.field_create");
        DateTimeOffset now = ToPostgresPrecision(timeProvider.GetUtcNow());
        await using IProductiveCoreUnitOfWork unitOfWork =
            await BeginUnitOfWorkAsync(
                ProductiveTransactionMode.SerializableWrite,
                "field_create",
                cancellationToken);
        try
        {
            Guid? authorizationVersion = await unitOfWork.AuthorizeOwnerAsync(
                requestContext,
                cancellationToken);
            if (authorizationVersion is null)
            {
                telemetry.Record("field_create", "not_available");
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            if (!options.Enabled)
            {
                telemetry.Record("field_create", "unavailable");
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            Dictionary<string, byte[]> keyRing = GetKeyRing();
            if (!await unitOfWork.RetainedKeyVersionsCoveredAsync(
                    keyRing.Keys.ToArray(),
                    cancellationToken))
            {
                telemetry.Record("field_create", "unavailable");
                throw ProductiveCoreErrors.ManagementUnitUnavailable();
            }

            Dictionary<string, byte[]> aliases = CreateAliases(
                command.OrganizationId,
                command.IdempotencyKey,
                keyRing);
            byte[] fingerprint = CreateRequestFingerprint(
                command.OrganizationId,
                displayName,
                requestContext,
                authorizationVersion.Value);
            Guid? existingLedgerId = await unitOfWork.FindCreationLedgerIdAsync(
                command.OrganizationId,
                aliases,
                cancellationToken);
            if (existingLedgerId is not null)
            {
                CreatedManagementUnitResult replay = await ResolveReplayAsync(
                    unitOfWork,
                    existingLedgerId.Value,
                    fingerprint,
                    authorizationVersion.Value,
                    requestContext,
                    now,
                    cancellationToken);
                await unitOfWork.AddMissingAliasesAsync(
                    command.OrganizationId,
                    existingLedgerId.Value,
                    aliases,
                    now,
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                telemetry.Record("field_create", "replayed");
                return replay;
            }

            int currentFieldCount = await unitOfWork.CountManagementUnitsAsync(
                command.OrganizationId,
                cancellationToken);
            if (currentFieldCount >= ManagementUnitLimits.MaximumPerOrganization)
            {
                throw ProductiveCoreErrors.ManagementUnitCapacityReached();
            }

            Guid managementUnitId = Guid.NewGuid();
            Guid resultVersion = Guid.NewGuid();
            Guid ledgerId = Guid.NewGuid();
            Guid leaseOwner = Guid.NewGuid();
            ManagementUnit field = new(
                managementUnitId,
                command.OrganizationId,
                displayName,
                now,
                resultVersion);
            ManagementUnitCreationLedger ledger = new(
                ledgerId,
                command.OrganizationId,
                requestContext.ActorUserId,
                requestContext.SessionId,
                authorizationVersion.Value,
                fingerprint,
                leaseOwner,
                now,
                now.Add(ValidLeaseLifetime()));
            ledger.Complete(
                leaseOwner,
                ledger.FenceToken,
                field.Id,
                field.Version,
                now);
            ManagementUnitCreationKeyAlias[] keyAliases = aliases
                .OrderBy(alias => alias.Key, StringComparer.Ordinal)
                .Select(alias => new ManagementUnitCreationKeyAlias(
                    Guid.NewGuid(),
                    ledgerId,
                    command.OrganizationId,
                    alias.Key,
                    alias.Value,
                    now))
                .ToArray();
            ProductiveJournalEntry journal = new(
                Guid.NewGuid(),
                command.OrganizationId,
                requestContext.ActorUserId,
                requestContext.SessionId,
                requestContext.CorrelationId,
                now);
            ProductiveOutboxMessage outbox = ProductiveOutboxMessage.CreateManagementUnitCreated(
                Guid.NewGuid(),
                requestContext.CorrelationId,
                new ManagementUnitCreatedIntegrationEventPayload(
                    command.OrganizationId,
                    field.Id,
                    field.UnitType,
                    field.Status,
                    field.CreatedAtUtc));
            unitOfWork.AddCreation(field, ledger, keyAliases, journal, outbox);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            telemetry.Record("field_create", "succeeded");
            return ToCreatedResult(field, isReplay: false);
        }
        catch (ProductiveIdempotencyRaceException)
        {
            await SafeRollbackAsync(unitOfWork, cancellationToken);
            telemetry.Record("field_create", "race");
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
                return await CreateFieldAsync(
                    command,
                    requestContext,
                    retryAfterRace: false,
                    cancellationToken);
            }

            telemetry.Record("field_create", "reconciliation_required");
            throw ProductiveCoreErrors.ReconciliationRequired();
        }
        catch (ProductiveCommitOutcomeUnknownException)
        {
            telemetry.Record("field_create", "commit_unknown");
            return await RecoverUnknownCommitAsync(
                command,
                requestContext,
                displayName,
                retryAfterRace,
                cancellationToken);
        }
        catch (ProductivePersistenceUnavailableException)
        {
            telemetry.Record("field_create", "unavailable");
            throw ProductiveCoreErrors.ManagementUnitUnavailable();
        }
        catch (ProductiveCoreOperationException exception)
        {
            telemetry.Record("field_create", OutcomeFor(exception.Code));
            throw;
        }
    }

    private async Task<CreatedManagementUnitResult> ResolveRaceAsync(
        CreateFieldCommand command,
        ProductiveRequestContext requestContext,
        string displayName,
        bool missingIsConflict,
        CancellationToken cancellationToken)
    {
        await using IProductiveCoreUnitOfWork recovery =
            await BeginUnitOfWorkAsync(
                ProductiveTransactionMode.Read,
                "field_create",
                cancellationToken);
        Guid? authorizationVersion = await recovery.AuthorizeOwnerAsync(requestContext, cancellationToken);
        if (authorizationVersion is null)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }

        Dictionary<string, byte[]> keyRing = GetKeyRing();
        Dictionary<string, byte[]> aliases = CreateAliases(
            command.OrganizationId,
            command.IdempotencyKey,
            keyRing);
        Guid? ledgerId = await recovery.FindCreationLedgerIdAsync(
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
            command.OrganizationId,
            displayName,
            requestContext,
            authorizationVersion.Value);
        CreatedManagementUnitResult result = await ResolveReplayAsync(
            recovery,
            ledgerId.Value,
            fingerprint,
            authorizationVersion.Value,
            requestContext,
            ToPostgresPrecision(timeProvider.GetUtcNow()),
            cancellationToken);
        await recovery.CommitAsync(cancellationToken);
        telemetry.Record("field_create", "replayed");
        return result;
    }

    private async Task<CreatedManagementUnitResult> RecoverUnknownCommitAsync(
        CreateFieldCommand command,
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
            return await CreateFieldAsync(
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
        catch (ProductivePersistenceUnavailableException)
        {
            throw ProductiveCoreErrors.ReconciliationRequired();
        }
    }

    private static async Task<CreatedManagementUnitResult> ResolveReplayAsync(
        IProductiveCoreUnitOfWork unitOfWork,
        Guid ledgerId,
        byte[] fingerprint,
        Guid authorizationVersion,
        ProductiveRequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ManagementUnitCreationLedger? ledger = await unitOfWork.GetCreationLedgerAsync(
            requestContext.OrganizationId,
            ledgerId,
            cancellationToken);
        if (ledger is null ||
            ledger.ActorUserId != requestContext.ActorUserId ||
            ledger.SessionId != requestContext.SessionId ||
            ledger.AuthorizationVersion != authorizationVersion ||
            !CryptographicOperations.FixedTimeEquals(ledger.RequestFingerprint, fingerprint))
        {
            throw ProductiveCoreErrors.IdempotencyKeyReused();
        }

        if (ledger.State == ManagementUnitCreationProtocol.States.InProgress)
        {
            throw now < ledger.LeaseUntilUtc
                ? ProductiveCoreErrors.IdempotencyInProgress()
                : ProductiveCoreErrors.ReconciliationRequired();
        }

        if (ledger.State == ManagementUnitCreationProtocol.States.FailedTerminal)
        {
            throw ProductiveCoreErrors.IdempotencyFailedTerminal();
        }

        if (ledger.State != ManagementUnitCreationProtocol.States.Succeeded ||
            ledger.ManagementUnitId is null ||
            ledger.ResultVersion is null)
        {
            throw ProductiveCoreErrors.ReconciliationRequired();
        }

        ManagementUnit? field = await unitOfWork.GetManagementUnitAsync(
            requestContext.OrganizationId,
            ledger.ManagementUnitId.Value,
            cancellationToken);
        if (field is null || field.Version != ledger.ResultVersion)
        {
            throw ProductiveCoreErrors.ReconciliationRequired();
        }

        return ToCreatedResult(field, isReplay: true);
    }

    private Dictionary<string, byte[]> GetKeyRing()
    {
        if (options.HmacKeys.Count is < 1 or > 8 ||
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
            "create-field-idempotency-v1",
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
        Guid organizationId,
        string displayName,
        ProductiveRequestContext requestContext,
        Guid authorizationVersion)
    {
        string canonical = string.Join(
            '|',
            "create-field-v1",
            organizationId.ToString("D"),
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
        Guid organizationId,
        ProductiveRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        if (organizationId == Guid.Empty || organizationId != requestContext.OrganizationId)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }
    }

    private async ValueTask<IProductiveCoreUnitOfWork> BeginUnitOfWorkAsync(
        ProductiveTransactionMode mode,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWorkFactory.BeginAsync(mode, cancellationToken);
        }
        catch (ProductivePersistenceUnavailableException)
        {
            telemetry.Record(operation, "unavailable");
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

    private static DateTimeOffset ToPostgresPrecision(DateTimeOffset value)
    {
        DateTimeOffset utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - (utc.Ticks % 10), TimeSpan.Zero);
    }

    private static ManagementUnitResult ToResult(ManagementUnit field) =>
        new(
            field.Id,
            field.OrganizationId,
            field.DisplayName,
            field.UnitType,
            field.Status,
            field.SpatialStatus,
            field.CreatedAtUtc,
            field.Version);

    private static CreatedManagementUnitResult ToCreatedResult(
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
            field.Version,
            isReplay);

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
            // The operation is already failing closed; recovery uses a fresh unit of work.
        }
    }

    private static string OutcomeFor(string code) => code switch
    {
        "productive_core.field_not_available" => "not_available",
        "productive_core.management_unit_unavailable" => "unavailable",
        "productive_core.management_unit_capacity_reached" => "conflict",
        "idempotency.key_reused" => "conflict",
        "idempotency.in_progress" => "in_progress",
        "idempotency.failed_terminal" => "failed_terminal",
        "idempotency.reconciliation_required" => "reconciliation_required",
        _ => "rejected",
    };

    private static bool IsReadTransactionUnavailable(Exception exception) =>
        exception is ProductivePersistenceUnavailableException or
            ProductiveCommitOutcomeUnknownException;
}

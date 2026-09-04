using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ProductiveCoreRenameApplicationServiceTests
{
    private const string IdempotencyKey = "rename_key_123456789012345678901";
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse(
        "2026-08-18T18:00:00.1234567Z",
        CultureInfo.InvariantCulture);

    [TestMethod]
    public async Task RenameAndReplayReturnTheSameVersionWithoutDuplicateEffects()
    {
        TestFixture fixture = CreateFixture();
        RenameFieldDraftCommand command = fixture.Command(" Campo Sur e\u0301lite ");

        RenamedManagementUnitResult renamed = await fixture.Service.RenameFieldDraftAsync(
            command,
            fixture.RequestContext,
            CancellationToken.None);
        RenamedManagementUnitResult replay = await fixture.Service.RenameFieldDraftAsync(
            command,
            fixture.RequestContext,
            CancellationToken.None);

        Assert.AreEqual("Campo Sur \u00e9lite", renamed.DisplayName);
        Assert.AreEqual(2L, renamed.Revision);
        Assert.IsFalse(renamed.IsReplay);
        Assert.IsTrue(replay.IsReplay);
        Assert.AreEqual(renamed.Version, replay.Version);
        Assert.AreEqual(renamed.DisplayName, replay.DisplayName);
        Assert.HasCount(1, fixture.Store.RenameLedgers);
        Assert.HasCount(1, fixture.Store.Journals);
        Assert.HasCount(1, fixture.Store.Outbox);
        Assert.AreEqual("ManagementUnitDisplayNameChanged", fixture.Store.Outbox[0].EventType);

        ProductiveCoreOperationException mismatch =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    command with { DisplayName = "Campo Este" },
                    fixture.RequestContext,
                    CancellationToken.None));
        Assert.AreEqual("idempotency.key_reused", mismatch.Code);
        Assert.HasCount(1, fixture.Store.RenameLedgers);
    }

    [TestMethod]
    public async Task AuthorizationDenialPrecedesAliasAndResourceLookup()
    {
        TestFixture fixture = CreateFixture(authorized: false);

        ProductiveCoreOperationException error =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    fixture.Command("Campo Sur"),
                    fixture.RequestContext,
                    CancellationToken.None));

        Assert.AreEqual("productive_core.field_not_available", error.Code);
        Assert.AreEqual(0, fixture.Store.RenameLookupCount);
        Assert.AreEqual(0, fixture.Store.ResourceLookupCount);
        Assert.HasCount(0, fixture.Store.RenameLedgers);
    }

    [TestMethod]
    public async Task StaleAndCanonicalNoChangeFailWithoutAuxiliaryEffects()
    {
        TestFixture fixture = CreateFixture();

        ProductiveCoreOperationException stale =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    fixture.Command("Campo Sur") with { ExpectedVersion = Guid.NewGuid() },
                    fixture.RequestContext,
                    CancellationToken.None));
        ProductiveCoreOperationException unchanged =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    fixture.Command("\uFEFF Campo Norte \u0085"),
                    fixture.RequestContext,
                    CancellationToken.None));

        Assert.AreEqual("productive_core.field_version_stale", stale.Code);
        Assert.AreEqual(412, stale.StatusCode);
        Assert.AreEqual("productive_core.field_display_name_unchanged", unchanged.Code);
        Assert.AreEqual(400, unchanged.StatusCode);
        Assert.HasCount(0, fixture.Store.RenameLedgers);
        Assert.HasCount(0, fixture.Store.Journals);
        Assert.HasCount(0, fixture.Store.Outbox);
        Assert.AreEqual(1L, fixture.Field.Revision);
    }

    [TestMethod]
    public async Task MissingOrForeignFieldIsNeutralAndCreatesNoLedger()
    {
        TestFixture fixture = CreateFixture();
        RenameFieldDraftCommand command = fixture.Command("Campo Sur") with
        {
            FieldId = Guid.NewGuid(),
        };

        ProductiveCoreOperationException error =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    command,
                    fixture.RequestContext,
                    CancellationToken.None));

        Assert.AreEqual("productive_core.field_not_available", error.Code);
        Assert.AreEqual(404, error.StatusCode);
        Assert.HasCount(0, fixture.Store.RenameLedgers);
        Assert.HasCount(0, fixture.Store.Journals);
        Assert.HasCount(0, fixture.Store.Outbox);
    }

    [TestMethod]
    public async Task MatchingInProgressLedgerReturnsRetryableConflict()
    {
        TestFixture fixture = CreateFixture();
        RenameFieldDraftCommand command = fixture.Command("Campo Sur");
        fixture.Store.SeedInProgress(command, fixture.RequestContext, Now);

        ProductiveCoreOperationException error =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    command,
                    fixture.RequestContext,
                    CancellationToken.None));

        Assert.AreEqual("idempotency.in_progress", error.Code);
        Assert.AreEqual(409, error.StatusCode);
        Assert.IsTrue(error.Retryable);
        Assert.AreEqual("Campo Norte", fixture.Field.DisplayName);
        Assert.HasCount(0, fixture.Store.Journals);
    }

    [TestMethod]
    public async Task AliasSplitDuringRecoveryFailsClosedAsReconciliationRequired()
    {
        TestFixture fixture = CreateFixture();
        fixture.Store.RenameLookupRacesRemaining = 2;

        ProductiveCoreOperationException error =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    fixture.Command("Campo Sur"),
                    fixture.RequestContext,
                    CancellationToken.None));

        Assert.AreEqual("idempotency.reconciliation_required", error.Code);
        Assert.AreEqual(503, error.StatusCode);
        Assert.IsTrue(error.Retryable);
        Assert.AreEqual(0, fixture.Store.RenameLookupRacesRemaining);
        Assert.AreEqual("Campo Norte", fixture.Field.DisplayName);
        Assert.AreEqual(1L, fixture.Field.Revision);
        Assert.HasCount(0, fixture.Store.RenameLedgers);
        Assert.HasCount(0, fixture.Store.RenameAliases);
        Assert.HasCount(0, fixture.Store.Journals);
        Assert.HasCount(0, fixture.Store.Outbox);
    }

    [TestMethod]
    public async Task CommitUnknownRecoversPersistedRenameWithoutDuplicateEffects()
    {
        TestFixture fixture = CreateFixture();
        fixture.Store.ThrowCommitUnknownOnce = true;

        RenamedManagementUnitResult result = await fixture.Service.RenameFieldDraftAsync(
            fixture.Command("Campo Sur"),
            fixture.RequestContext,
            CancellationToken.None);

        Assert.IsTrue(result.IsReplay);
        Assert.AreEqual(2L, result.Revision);
        Assert.HasCount(1, fixture.Store.RenameLedgers);
        Assert.HasCount(1, fixture.Store.Journals);
        Assert.HasCount(1, fixture.Store.Outbox);
    }

    [TestMethod]
    public async Task KeyRotationBackfillsAliasAndEarlyRetirementFailsClosed()
    {
        TestFixture transition = CreateFixture();
        RenameFieldDraftCommand transitionCommand = transition.Command("Campo Sur");
        _ = await transition.Service.RenameFieldDraftAsync(
            transitionCommand,
            transition.RequestContext,
            CancellationToken.None);

        RenamedManagementUnitResult lazyReplay = await transition
            .ServiceWithKeyVersions("v1", "v2")
            .RenameFieldDraftAsync(
                transitionCommand,
                transition.RequestContext,
                CancellationToken.None);
        RenamedManagementUnitResult retainedReplay = await transition
            .ServiceWithKeyVersions("v2")
            .RenameFieldDraftAsync(
                transitionCommand,
                transition.RequestContext,
                CancellationToken.None);

        Assert.IsTrue(lazyReplay.IsReplay);
        Assert.IsTrue(retainedReplay.IsReplay);
        Assert.AreEqual(lazyReplay.Version, retainedReplay.Version);
        Assert.IsTrue(transition.Store.RenameAliases.Keys.Any(key =>
            key.Contains("|v2|", StringComparison.Ordinal)));
        Assert.HasCount(1, transition.Store.RenameLedgers);

        TestFixture earlyRetirement = CreateFixture();
        RenameFieldDraftCommand earlyCommand = earlyRetirement.Command("Campo Sur");
        _ = await earlyRetirement.Service.RenameFieldDraftAsync(
            earlyCommand,
            earlyRetirement.RequestContext,
            CancellationToken.None);
        ProductiveCoreOperationException unavailable =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                earlyRetirement.ServiceWithKeyVersions("v2").RenameFieldDraftAsync(
                    earlyCommand,
                    earlyRetirement.RequestContext,
                    CancellationToken.None));

        Assert.AreEqual("productive_core.management_unit_unavailable", unavailable.Code);
        Assert.AreEqual(503, unavailable.StatusCode);
        Assert.HasCount(1, earlyRetirement.Store.RenameLedgers);
        Assert.HasCount(1, earlyRetirement.Store.Journals);
        Assert.HasCount(1, earlyRetirement.Store.Outbox);
    }

    [TestMethod]
    public async Task BeginFailureIsTypedRetryableUnavailable()
    {
        TestFixture fixture = CreateFixture(beginUnavailable: true);

        ProductiveCoreOperationException error =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.RenameFieldDraftAsync(
                    fixture.Command("Campo Sur"),
                    fixture.RequestContext,
                    CancellationToken.None));

        Assert.AreEqual("productive_core.management_unit_unavailable", error.Code);
        Assert.AreEqual(503, error.StatusCode);
        Assert.IsTrue(error.Retryable);
    }

    private static TestFixture CreateFixture(bool authorized = true, bool beginUnavailable = false)
    {
        Guid organizationId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        var store = new RenameStore { Authorized = authorized };
        ManagementUnit field = new(
            Guid.NewGuid(),
            organizationId,
            "Campo Norte",
            Now,
            Guid.NewGuid());
        store.Units.Add(field.Id, field);
        ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        ProductiveCoreRenameApplicationService service = CreateService(
            store,
            beginUnavailable,
            provider,
            "v1");
        ProductiveRequestContext requestContext = new(
            "rename-test-correlation",
            actorId,
            sessionId,
            organizationId);
        return new TestFixture(service, store, field, requestContext);
    }

    private sealed record TestFixture(
        ProductiveCoreRenameApplicationService Service,
        RenameStore Store,
        ManagementUnit Field,
        ProductiveRequestContext RequestContext)
    {
        public RenameFieldDraftCommand Command(string displayName) =>
            new(
                RequestContext.OrganizationId,
                Field.Id,
                displayName,
                Field.Version,
                IdempotencyKey);

        public ProductiveCoreRenameApplicationService ServiceWithKeyVersions(
            params string[] versions)
        {
            ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
            return CreateService(Store, beginUnavailable: false, provider, versions);
        }
    }

    private static ProductiveCoreRenameApplicationService CreateService(
        RenameStore store,
        bool beginUnavailable,
        ServiceProvider provider,
        params string[] versions)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string version in versions)
        {
            byte[] key = version switch
            {
                "v1" => RenameStore.HmacKey,
                "v2" => RenameStore.HmacKeyTwo,
                _ => throw new ArgumentOutOfRangeException(nameof(versions)),
            };
            keys.Add(version, Convert.ToBase64String(key));
        }

        return new ProductiveCoreRenameApplicationService(
            new RenameUnitOfWorkFactory(store, beginUnavailable),
            new ProductiveCoreTelemetry(
                provider.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()),
            new FixedTimeProvider(Now),
            Options.Create(new ManagementUnitRenameOptions
            {
                Enabled = true,
                CurrentKeyVersion = versions[^1],
                LeaseLifetime = TimeSpan.FromMinutes(1),
                HmacKeys = keys,
            }));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RenameStore
    {
        public static readonly byte[] HmacKey =
            Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        public static readonly byte[] HmacKeyTwo =
            Enumerable.Range(33, 32).Select(value => (byte)value).ToArray();

        public bool Authorized { get; init; }

        public bool ThrowCommitUnknownOnce { get; set; }

        public Guid AuthorizationVersion { get; } = Guid.NewGuid();

        public Dictionary<Guid, ManagementUnit> Units { get; } = [];

        public Dictionary<Guid, ManagementUnitRenameLedger> RenameLedgers { get; } = [];

        public Dictionary<string, Guid> RenameAliases { get; } = new(StringComparer.Ordinal);

        public List<ProductiveJournalEntry> Journals { get; } = [];

        public List<ProductiveOutboxMessage> Outbox { get; } = [];

        public int RenameLookupCount { get; set; }

        public int ResourceLookupCount { get; set; }

        public int RenameLookupRacesRemaining { get; set; }

        public void SeedInProgress(
            RenameFieldDraftCommand command,
            ProductiveRequestContext requestContext,
            DateTimeOffset now)
        {
            byte[] fingerprint = Fingerprint(
                command,
                ManagementUnit.NormalizeDisplayName(command.DisplayName),
                requestContext,
                AuthorizationVersion);
            ManagementUnitRenameLedger ledger = new(
                Guid.NewGuid(),
                command.OrganizationId,
                requestContext.ActorUserId,
                requestContext.SessionId,
                AuthorizationVersion,
                command.FieldId,
                command.ExpectedVersion,
                fingerprint,
                Guid.NewGuid(),
                now,
                now.AddMinutes(1));
            RenameLedgers.Add(ledger.Id, ledger);
            byte[] digest = AliasDigest(command.OrganizationId, command.IdempotencyKey);
            RenameAliases.Add(AliasKey(command.OrganizationId, "v1", digest), ledger.Id);
        }

        private static byte[] Fingerprint(
            RenameFieldDraftCommand command,
            string displayName,
            ProductiveRequestContext context,
            Guid authorizationVersion)
        {
            string canonical = string.Join(
                '|',
                "rename-field-v1",
                command.OrganizationId.ToString("D"),
                command.FieldId.ToString("D"),
                command.ExpectedVersion.ToString("D"),
                context.ActorUserId.ToString("D"),
                context.SessionId.ToString("D"),
                authorizationVersion.ToString("D"),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(displayName)));
            return SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        }

        public static byte[] AliasDigest(Guid organizationId, string key)
        {
            byte[] message = Encoding.ASCII.GetBytes(string.Join(
                '|',
                "rename-field-idempotency-v1",
                organizationId.ToString("D"),
                key));
            return HMACSHA256.HashData(HmacKey, message);
        }

        public static string AliasKey(Guid organizationId, string version, byte[] digest) =>
            string.Concat(
                organizationId.ToString("D"),
                "|",
                version,
                "|",
                Convert.ToHexString(digest));
    }

    private sealed class RenameUnitOfWorkFactory(RenameStore store, bool beginUnavailable)
        : IProductiveCoreUnitOfWorkFactory
    {
        public ValueTask<IProductiveCoreUnitOfWork> BeginAsync(
            ProductiveTransactionMode mode,
            CancellationToken cancellationToken)
        {
            Assert.AreEqual(ProductiveTransactionMode.SerializableWrite, mode);
            cancellationToken.ThrowIfCancellationRequested();
            return beginUnavailable
                ? ValueTask.FromException<IProductiveCoreUnitOfWork>(
                    new ProductivePersistenceUnavailableException("Synthetic begin failure."))
                : ValueTask.FromResult<IProductiveCoreUnitOfWork>(new RenameUnitOfWork(store));
        }
    }

    private sealed class RenameUnitOfWork(RenameStore store) : IProductiveCoreUnitOfWork
    {
        public Task<IReadOnlyList<ProductionCycle>> ListProductionCyclePageAsync(Guid organizationId, Guid managementUnitId, ProductionHistoryWindow window, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductionEvent>> ListProductionEventPageAsync(Guid organizationId, Guid cycleId, ProductionHistoryWindow window, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductionCycle?> GetProductionCycleAsync(Guid organizationId, Guid cycleId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductionCycle?> GetProductionCycleForUpdateAsync(Guid organizationId, Guid cycleId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductionCycle>> ListProductionCyclesAsync(Guid organizationId, Guid managementUnitId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductionEvent>> ListProductionEventsAsync(Guid organizationId, Guid cycleId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void AddProductionCycle(ProductionCycle cycle) => throw new NotSupportedException();

        public void AddProductionEvent(ProductionEvent productionEvent) => throw new NotSupportedException();

        public Task<Guid?> AuthorizeOwnerAsync(
            ProductiveRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            _ = requestContext;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Guid?>(store.Authorized ? store.AuthorizationVersion : null);
        }

        public Task<bool> RetainedKeyVersionsCoveredAsync(
            IReadOnlyCollection<string> retainedKeyVersions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(retainedKeyVersions.Count > 0);
        }

        public Task<Guid?> FindCreationLedgerIdAsync(
            Guid organizationId,
            IReadOnlyDictionary<string, byte[]> aliases,
            CancellationToken cancellationToken)
        {
            _ = organizationId;
            _ = aliases;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Guid?>(null);
        }

        public Task<ManagementUnitCreationLedger?> GetCreationLedgerAsync(
            Guid organizationId,
            Guid ledgerId,
            CancellationToken cancellationToken)
        {
            _ = organizationId;
            _ = ledgerId;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ManagementUnitCreationLedger?>(null);
        }

        public Task<ManagementUnit?> GetManagementUnitAsync(
            Guid organizationId,
            Guid managementUnitId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.Units.TryGetValue(managementUnitId, out ManagementUnit? unit);
            return Task.FromResult(unit?.OrganizationId == organizationId ? unit : null);
        }

        public Task<IReadOnlyList<ManagementUnit>> ListManagementUnitsAsync(
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<ManagementUnit> units = store.Units.Values
                .Where(unit => unit.OrganizationId == organizationId)
                .ToArray();
            return Task.FromResult(units);
        }

        public Task<int> CountManagementUnitsAsync(
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                store.Units.Values.Count(unit => unit.OrganizationId == organizationId));
        }

        public Task<ManagementUnit?> GetManagementUnitForUpdateAsync(
            Guid organizationId,
            Guid managementUnitId,
            CancellationToken cancellationToken)
        {
            store.ResourceLookupCount++;
            return GetManagementUnitAsync(organizationId, managementUnitId, cancellationToken);
        }

        public Task<bool> RetainedRenameKeyVersionsCoveredAsync(
            IReadOnlyCollection<string> retainedKeyVersions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool covered = store.RenameLedgers.Values.All(ledger =>
                retainedKeyVersions.Any(version =>
                    store.RenameAliases.Any(alias =>
                        alias.Value == ledger.Id &&
                        alias.Key.StartsWith(
                            string.Concat(ledger.OrganizationId.ToString("D"), "|", version, "|"),
                            StringComparison.Ordinal))));
            return Task.FromResult(covered);
        }

        public Task<bool> RetainedArchiveKeyVersionsCoveredAsync(
            IReadOnlyCollection<string> retainedKeyVersions,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("This rename-only fake does not implement archival.");

        public Task<ValidatedFieldGeometry> ValidateInitialGeometryAsync(string geoJson, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Geometry uses real PostGIS proofs.");

        public Task AddInitialGeometryAsync(InitialFieldGeometrySnapshot snapshot, ProductiveJournalEntry journalEntry,
            ProductiveOutboxMessage outboxMessage, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Geometry uses real PostGIS proofs.");

        public Task<InitialFieldGeometrySnapshot?> GetInitialGeometryAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Geometry uses real PostGIS proofs.");

        public Task<Guid?> FindRenameLedgerIdAsync(
            Guid organizationId,
            IReadOnlyDictionary<string, byte[]> aliases,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.RenameLookupCount++;
            if (store.RenameLookupRacesRemaining > 0)
            {
                store.RenameLookupRacesRemaining--;
                throw new ProductiveIdempotencyRaceException(
                    "Synthetic split alias resolution.");
            }

            Guid[] matches = aliases
                .Select(alias => RenameStore.AliasKey(organizationId, alias.Key, alias.Value))
                .Where(store.RenameAliases.ContainsKey)
                .Select(key => store.RenameAliases[key])
                .Distinct()
                .ToArray();
            return Task.FromResult<Guid?>(matches.Length == 1 ? matches[0] : null);
        }

        public Task<ManagementUnitRenameLedger?> GetRenameLedgerAsync(
            Guid organizationId,
            Guid ledgerId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.RenameLedgers.TryGetValue(ledgerId, out ManagementUnitRenameLedger? ledger);
            return Task.FromResult(ledger?.OrganizationId == organizationId ? ledger : null);
        }

        public void AddCreation(
            ManagementUnit managementUnit,
            ManagementUnitCreationLedger ledger,
            IReadOnlyCollection<ManagementUnitCreationKeyAlias> aliases,
            ProductiveJournalEntry journalEntry,
            ProductiveOutboxMessage outboxMessage) =>
            throw new NotSupportedException();

        public Task AddMissingAliasesAsync(
            Guid organizationId,
            Guid ledgerId,
            IReadOnlyDictionary<string, byte[]> aliases,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public void AddRename(
            ManagementUnitRenameLedger ledger,
            IReadOnlyCollection<ManagementUnitRenameKeyAlias> aliases,
            ProductiveJournalEntry journalEntry,
            ProductiveOutboxMessage outboxMessage)
        {
            store.RenameLedgers.Add(ledger.Id, ledger);
            foreach (ManagementUnitRenameKeyAlias alias in aliases)
            {
                store.RenameAliases.Add(
                    RenameStore.AliasKey(alias.OrganizationId, alias.KeyVersion, alias.KeyDigest),
                    ledger.Id);
            }

            store.Journals.Add(journalEntry);
            store.Outbox.Add(outboxMessage);
        }

        public Task AddMissingRenameAliasesAsync(
            Guid organizationId,
            Guid ledgerId,
            IReadOnlyDictionary<string, byte[]> aliases,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken)
        {
            _ = createdAtUtc;
            cancellationToken.ThrowIfCancellationRequested();
            foreach ((string version, byte[] digest) in aliases)
            {
                store.RenameAliases.TryAdd(
                    RenameStore.AliasKey(organizationId, version, digest),
                    ledgerId);
            }

            return Task.CompletedTask;
        }

        public Task<Guid?> FindArchiveLedgerIdAsync(
            Guid organizationId,
            IReadOnlyDictionary<string, byte[]> aliases,
            CancellationToken cancellationToken)
        {
            _ = organizationId;
            _ = aliases;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Guid?>(null);
        }

        public Task<ManagementUnitArchiveLedger?> GetArchiveLedgerAsync(
            Guid organizationId,
            Guid ledgerId,
            CancellationToken cancellationToken)
        {
            _ = organizationId;
            _ = ledgerId;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ManagementUnitArchiveLedger?>(null);
        }

        public void AddArchive(
            ManagementUnitArchiveLedger ledger,
            IReadOnlyCollection<ManagementUnitArchiveKeyAlias> aliases,
            ProductiveJournalEntry journalEntry,
            ProductiveOutboxMessage outboxMessage) =>
            throw new NotSupportedException();

        public Task AddMissingArchiveAliasesAsync(
            Guid organizationId,
            Guid ledgerId,
            IReadOnlyDictionary<string, byte[]> aliases,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken)
        {
            _ = organizationId;
            _ = ledgerId;
            _ = aliases;
            _ = createdAtUtc;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (store.ThrowCommitUnknownOnce)
            {
                store.ThrowCommitUnknownOnce = false;
                throw new ProductiveCommitOutcomeUnknownException("Synthetic commit uncertainty.");
            }

            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

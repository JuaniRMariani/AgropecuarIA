using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ProductiveCoreApplicationServiceTests
{
    private const string KeyOne = "11111111111111111111111111111111";
    private const string KeyTwo = "22222222222222222222222222222222";
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse(
        "2026-08-18T18:00:00.1234567Z",
        CultureInfo.InvariantCulture);

    [TestMethod]
    public async Task CreateReplayAndFingerprintMismatchAreDeterministic()
    {
        TestFixture fixture = CreateFixture();
        CreateFieldCommand command = new(fixture.OrganizationId, " Campo Norte ", KeyOne);

        CreatedManagementUnitResult created = await fixture.Service.CreateFieldAsync(
            command,
            fixture.RequestContext,
            CancellationToken.None);
        CreatedManagementUnitResult replay = await fixture.Service.CreateFieldAsync(
            command,
            fixture.RequestContext,
            CancellationToken.None);

        Assert.IsFalse(created.IsReplay);
        Assert.IsTrue(replay.IsReplay);
        Assert.AreEqual(created.FieldId, replay.FieldId);
        Assert.AreEqual("Campo Norte", created.DisplayName);
        Assert.AreEqual(1, fixture.Store.Units.Count);
        Assert.AreEqual(1, fixture.Store.Ledgers.Count);
        Assert.AreEqual(1, fixture.Store.Journals.Count);
        Assert.AreEqual(1, fixture.Store.Outbox.Count);

        ProductiveCoreOperationException mismatch = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(
            () => fixture.Service.CreateFieldAsync(
                command with { DisplayName = "Campo Sur" },
                fixture.RequestContext,
                CancellationToken.None));
        Assert.AreEqual("idempotency.key_reused", mismatch.Code);
        Assert.AreEqual(1, fixture.Store.Units.Count);
    }

    [TestMethod]
    public async Task DuplicateDisplayNamesWithDifferentKeysAreAllowed()
    {
        TestFixture fixture = CreateFixture();

        CreatedManagementUnitResult first = await fixture.Service.CreateFieldAsync(
            new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyOne),
            fixture.RequestContext,
            CancellationToken.None);
        CreatedManagementUnitResult second = await fixture.Service.CreateFieldAsync(
            new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyTwo),
            fixture.RequestContext,
            CancellationToken.None);

        Assert.AreNotEqual(first.FieldId, second.FieldId);
        Assert.AreEqual(2, fixture.Store.Units.Count);
    }

    [TestMethod]
    public async Task AuthorizationDenialPrecedesIdempotencyLookupAndRead()
    {
        TestFixture fixture = CreateFixture(authorized: false);

        ProductiveCoreOperationException createError = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(
            () => fixture.Service.CreateFieldAsync(
                new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyOne),
                fixture.RequestContext,
                CancellationToken.None));
        ProductiveCoreOperationException listError = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(
            () => fixture.Service.ListFieldsAsync(
                fixture.OrganizationId,
                fixture.RequestContext,
                CancellationToken.None));

        Assert.AreEqual("productive_core.field_not_available", createError.Code);
        Assert.AreEqual("productive_core.field_not_available", listError.Code);
        Assert.AreEqual(0, fixture.Store.IdempotencyLookupCount);
        Assert.AreEqual(0, fixture.Store.ReadCount);
    }

    [TestMethod]
    public async Task ListIsOrderedAndDetailIsNeutralForMissingTarget()
    {
        TestFixture fixture = CreateFixture();
        fixture.Clock.Set(Now.AddMinutes(1));
        CreatedManagementUnitResult later = await fixture.Service.CreateFieldAsync(
            new CreateFieldCommand(fixture.OrganizationId, "Campo Sur", KeyTwo),
            fixture.RequestContext,
            CancellationToken.None);
        fixture.Clock.Set(Now);
        CreatedManagementUnitResult earlier = await fixture.Service.CreateFieldAsync(
            new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyOne),
            fixture.RequestContext,
            CancellationToken.None);

        IReadOnlyList<ManagementUnitResult> fields = await fixture.Service.ListFieldsAsync(
            fixture.OrganizationId,
            fixture.RequestContext,
            CancellationToken.None);
        ManagementUnitResult detail = await fixture.Service.GetFieldAsync(
            fixture.OrganizationId,
            later.FieldId,
            fixture.RequestContext,
            CancellationToken.None);

        Assert.AreEqual(earlier.FieldId, fields[0].FieldId);
        Assert.AreEqual(later.FieldId, fields[1].FieldId);
        Assert.AreEqual("Campo Sur", detail.DisplayName);

        ProductiveCoreOperationException missing = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(
            () => fixture.Service.GetFieldAsync(
                fixture.OrganizationId,
                Guid.NewGuid(),
                fixture.RequestContext,
                CancellationToken.None));
        Assert.AreEqual("productive_core.field_not_available", missing.Code);
    }

    [TestMethod]
    public async Task CommitUnknownRecoversThePersistedResultWithoutDuplicates()
    {
        TestFixture fixture = CreateFixture();
        fixture.Store.ThrowCommitUnknownOnce = true;

        CreatedManagementUnitResult result = await fixture.Service.CreateFieldAsync(
            new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyOne),
            fixture.RequestContext,
            CancellationToken.None);

        Assert.IsTrue(result.IsReplay);
        Assert.AreEqual(1, fixture.Store.Units.Count);
        Assert.AreEqual(1, fixture.Store.Ledgers.Count);
        Assert.AreEqual(1, fixture.Store.Journals.Count);
        Assert.AreEqual(1, fixture.Store.Outbox.Count);
    }

    [TestMethod]
    public async Task IdempotencyKeyMustMatchTheFrozenUrlSafeBoundary()
    {
        TestFixture fixture = CreateFixture();
        string[] invalidKeys =
        [
            new string('a', 31),
            string.Concat(new string('a', 31), "!"),
            new string('a', 129),
        ];

        foreach (string key in invalidKeys)
        {
            ProductiveCoreOperationException error =
                await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                    fixture.Service.CreateFieldAsync(
                        new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", key),
                        fixture.RequestContext,
                        CancellationToken.None));
            Assert.AreEqual("productive_core.invalid_idempotency_key", error.Code);
        }

        CreatedManagementUnitResult accepted = await fixture.Service.CreateFieldAsync(
            new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyOne),
            fixture.RequestContext,
            CancellationToken.None);
        Assert.IsFalse(accepted.IsReplay);
        Assert.AreEqual(1, fixture.Store.IdempotencyLookupCount);
    }

    [TestMethod]
    public async Task BeginFailuresAreTypedUnavailableAndRecordedForEveryOperation()
    {
        var measurements = new List<(string Operation, string Outcome)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter.Name == ProductiveCoreTelemetry.SourceName &&
                instrument.Name == "productive_core.operations")
            {
                currentListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string operation = string.Empty;
            string outcome = string.Empty;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "operation")
                {
                    operation = tag.Value?.ToString() ?? string.Empty;
                }
                else if (tag.Key == "outcome")
                {
                    outcome = tag.Value?.ToString() ?? string.Empty;
                }
            }

            measurements.Add((operation, outcome));
        });
        listener.Start();

        TestFixture fixture = CreateFixture(
            unitOfWorkFactory: new UnavailableUnitOfWorkFactory());
        ProductiveCoreOperationException create =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.CreateFieldAsync(
                    new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyOne),
                    fixture.RequestContext,
                    CancellationToken.None));
        ProductiveCoreOperationException list =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.ListFieldsAsync(
                    fixture.OrganizationId,
                    fixture.RequestContext,
                    CancellationToken.None));
        ProductiveCoreOperationException detail =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.GetFieldAsync(
                    fixture.OrganizationId,
                    Guid.NewGuid(),
                    fixture.RequestContext,
                    CancellationToken.None));

        foreach (ProductiveCoreOperationException failure in new[] { create, list, detail })
        {
            AssertUnavailable(failure);
        }

        CollectionAssert.IsSubsetOf(
            new[]
            {
                ("field_create", "unavailable"),
                ("field_list", "unavailable"),
                ("field_detail", "unavailable"),
            },
            measurements);
    }

    [TestMethod]
    public async Task ReadCommitUnknownIsTypedUnavailable()
    {
        TestFixture fixture = CreateFixture();
        ManagementUnit field = new(
            Guid.NewGuid(),
            fixture.OrganizationId,
            "Campo Norte",
            Now,
            Guid.NewGuid());
        fixture.Store.Units.Add(field.Id, field);

        fixture.Store.ThrowReadCommitUnknownOnce = true;
        ProductiveCoreOperationException list =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.ListFieldsAsync(
                    fixture.OrganizationId,
                    fixture.RequestContext,
                    CancellationToken.None));
        fixture.Store.ThrowReadCommitUnknownOnce = true;
        ProductiveCoreOperationException detail =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.GetFieldAsync(
                    fixture.OrganizationId,
                    field.Id,
                    fixture.RequestContext,
                    CancellationToken.None));

        AssertUnavailable(list);
        AssertUnavailable(detail);
    }

    [TestMethod]
    public async Task RecoveryBeginFailureIsTypedUnavailable()
    {
        var store = new InMemoryStore { Authorized = true, ThrowCommitUnknownOnce = true };
        TestFixture fixture = CreateFixture(
            store: store,
            unitOfWorkFactory: new FailAfterFirstBeginUnitOfWorkFactory(store));

        ProductiveCoreOperationException failure =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.CreateFieldAsync(
                    new CreateFieldCommand(fixture.OrganizationId, "Campo Norte", KeyOne),
                    fixture.RequestContext,
                    CancellationToken.None));

        AssertUnavailable(failure);
    }

    [TestMethod]
    public async Task CapacityRejectsANewKeyWithoutAuxiliaryEffectsButKeepsListHealthy()
    {
        TestFixture fixture = CreateFixture();
        for (int index = 0; index < 100; index++)
        {
            ManagementUnit field = new(
                Guid.NewGuid(),
                fixture.OrganizationId,
                $"Campo {index:D3}",
                Now.AddSeconds(index),
                Guid.NewGuid());
            fixture.Store.Units.Add(field.Id, field);
        }

        ProductiveCoreOperationException capacity =
            await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
                fixture.Service.CreateFieldAsync(
                    new CreateFieldCommand(fixture.OrganizationId, "Campo Límite", KeyOne),
                    fixture.RequestContext,
                    CancellationToken.None));
        IReadOnlyList<ManagementUnitResult> fields = await fixture.Service.ListFieldsAsync(
            fixture.OrganizationId,
            fixture.RequestContext,
            CancellationToken.None);

        Assert.AreEqual("productive_core.management_unit_capacity_reached", capacity.Code);
        Assert.AreEqual(409, capacity.StatusCode);
        Assert.IsFalse(capacity.Retryable);
        Assert.AreEqual(100, fields.Count);
        Assert.HasCount(0, fixture.Store.Ledgers);
        Assert.HasCount(0, fixture.Store.Aliases);
        Assert.HasCount(0, fixture.Store.Journals);
        Assert.HasCount(0, fixture.Store.Outbox);
    }

    private static TestFixture CreateFixture(
        bool authorized = true,
        IProductiveCoreUnitOfWorkFactory? unitOfWorkFactory = null,
        InMemoryStore? store = null)
    {
        Guid organizationId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        store ??= new InMemoryStore { Authorized = authorized };
        FixedTimeProvider clock = new(Now);
        ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        ProductiveCoreTelemetry telemetry = new(provider.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>());
        ProductiveCoreApplicationService service = new(
            unitOfWorkFactory ?? new InMemoryUnitOfWorkFactory(store),
            telemetry,
            clock,
            Options.Create(new ManagementUnitCreationOptions
            {
                Enabled = true,
                CurrentKeyVersion = "v1",
                HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["v1"] = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()),
                },
            }));
        return new TestFixture(
            service,
            store,
            clock,
            organizationId,
            new ProductiveRequestContext("test-correlation", actorId, sessionId, organizationId));
    }

    private sealed record TestFixture(
        ProductiveCoreApplicationService Service,
        InMemoryStore Store,
        FixedTimeProvider Clock,
        Guid OrganizationId,
        ProductiveRequestContext RequestContext);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public override DateTimeOffset GetUtcNow() => current;

        public void Set(DateTimeOffset value) => current = value;
    }

    private sealed class InMemoryStore
    {
        public bool Authorized { get; set; }

        public bool ThrowCommitUnknownOnce { get; set; }

        public bool ThrowReadCommitUnknownOnce { get; set; }

        public Guid AuthorizationVersion { get; } = Guid.NewGuid();

        public Dictionary<Guid, ManagementUnit> Units { get; } = [];

        public Dictionary<Guid, ManagementUnitCreationLedger> Ledgers { get; } = [];

        public Dictionary<string, Guid> Aliases { get; } = new(StringComparer.Ordinal);

        public List<ProductiveJournalEntry> Journals { get; } = [];

        public List<ProductiveOutboxMessage> Outbox { get; } = [];

        public int IdempotencyLookupCount { get; set; }

        public int ReadCount { get; set; }
    }

    private sealed class InMemoryUnitOfWorkFactory(InMemoryStore store) : IProductiveCoreUnitOfWorkFactory
    {
        public ValueTask<IProductiveCoreUnitOfWork> BeginAsync(
            ProductiveTransactionMode mode,
            CancellationToken cancellationToken)
        {
            _ = mode;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<IProductiveCoreUnitOfWork>(new InMemoryUnitOfWork(store));
        }
    }

    private sealed class UnavailableUnitOfWorkFactory : IProductiveCoreUnitOfWorkFactory
    {
        public ValueTask<IProductiveCoreUnitOfWork> BeginAsync(
            ProductiveTransactionMode mode,
            CancellationToken cancellationToken)
        {
            _ = mode;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromException<IProductiveCoreUnitOfWork>(
                new ProductivePersistenceUnavailableException("Synthetic begin failure."));
        }
    }

    private sealed class FailAfterFirstBeginUnitOfWorkFactory(InMemoryStore store)
        : IProductiveCoreUnitOfWorkFactory
    {
        private int beginCount;

        public ValueTask<IProductiveCoreUnitOfWork> BeginAsync(
            ProductiveTransactionMode mode,
            CancellationToken cancellationToken)
        {
            _ = mode;
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Increment(ref beginCount) == 1)
            {
                return ValueTask.FromResult<IProductiveCoreUnitOfWork>(
                    new InMemoryUnitOfWork(store));
            }

            return ValueTask.FromException<IProductiveCoreUnitOfWork>(
                new ProductivePersistenceUnavailableException("Synthetic recovery begin failure."));
        }
    }

    private sealed class InMemoryUnitOfWork(InMemoryStore store) : IProductiveCoreUnitOfWork
    {
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
            cancellationToken.ThrowIfCancellationRequested();
            store.IdempotencyLookupCount++;
            Guid[] matches = aliases
                .Select(alias => AliasKey(organizationId, alias.Key, alias.Value))
                .Where(store.Aliases.ContainsKey)
                .Select(key => store.Aliases[key])
                .Distinct()
                .ToArray();
            return Task.FromResult<Guid?>(matches.Length == 1 ? matches[0] : null);
        }

        public Task<ManagementUnitCreationLedger?> GetCreationLedgerAsync(
            Guid organizationId,
            Guid ledgerId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.Ledgers.TryGetValue(ledgerId, out ManagementUnitCreationLedger? ledger);
            return Task.FromResult(ledger?.OrganizationId == organizationId ? ledger : null);
        }

        public Task<ManagementUnit?> GetManagementUnitAsync(
            Guid organizationId,
            Guid managementUnitId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.ReadCount++;
            store.Units.TryGetValue(managementUnitId, out ManagementUnit? unit);
            return Task.FromResult(unit?.OrganizationId == organizationId ? unit : null);
        }

        public Task<IReadOnlyList<ManagementUnit>> ListManagementUnitsAsync(
            Guid organizationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.ReadCount++;
            IReadOnlyList<ManagementUnit> units = store.Units.Values
                .Where(unit => unit.OrganizationId == organizationId)
                .Reverse()
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
            CancellationToken cancellationToken) =>
            GetManagementUnitAsync(organizationId, managementUnitId, cancellationToken);

        public Task<bool> RetainedRenameKeyVersionsCoveredAsync(
            IReadOnlyCollection<string> retainedKeyVersions,
            CancellationToken cancellationToken) =>
            RetainedKeyVersionsCoveredAsync(retainedKeyVersions, cancellationToken);

        public Task<bool> RetainedArchiveKeyVersionsCoveredAsync(
            IReadOnlyCollection<string> retainedKeyVersions,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("This creation-only fake does not implement archival.");

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
            _ = organizationId;
            _ = aliases;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Guid?>(null);
        }

        public Task<ManagementUnitRenameLedger?> GetRenameLedgerAsync(
            Guid organizationId,
            Guid ledgerId,
            CancellationToken cancellationToken)
        {
            _ = organizationId;
            _ = ledgerId;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ManagementUnitRenameLedger?>(null);
        }

        public void AddCreation(
            ManagementUnit managementUnit,
            ManagementUnitCreationLedger ledger,
            IReadOnlyCollection<ManagementUnitCreationKeyAlias> aliases,
            ProductiveJournalEntry journalEntry,
            ProductiveOutboxMessage outboxMessage)
        {
            store.Units.Add(managementUnit.Id, managementUnit);
            store.Ledgers.Add(ledger.Id, ledger);
            foreach (ManagementUnitCreationKeyAlias alias in aliases)
            {
                store.Aliases.Add(AliasKey(alias.OrganizationId, alias.KeyVersion, alias.KeyDigest), ledger.Id);
            }

            store.Journals.Add(journalEntry);
            store.Outbox.Add(outboxMessage);
        }

        public Task AddMissingAliasesAsync(
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
                store.Aliases.TryAdd(AliasKey(organizationId, version, digest), ledgerId);
            }

            return Task.CompletedTask;
        }

        public void AddRename(
            ManagementUnitRenameLedger ledger,
            IReadOnlyCollection<ManagementUnitRenameKeyAlias> aliases,
            ProductiveJournalEntry journalEntry,
            ProductiveOutboxMessage outboxMessage) =>
            throw new NotSupportedException("Rename is covered by its dedicated application tests.");

        public Task AddMissingRenameAliasesAsync(
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

            if (store.ThrowReadCommitUnknownOnce)
            {
                store.ThrowReadCommitUnknownOnce = false;
                throw new ProductiveCommitOutcomeUnknownException("Synthetic read commit uncertainty.");
            }

            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static string AliasKey(Guid organizationId, string version, byte[] digest) =>
            string.Concat(organizationId.ToString("D"), "|", version, "|", Convert.ToHexString(digest));
    }

    private static void AssertUnavailable(ProductiveCoreOperationException exception)
    {
        Assert.AreEqual("productive_core.management_unit_unavailable", exception.Code);
        Assert.AreEqual(503, exception.StatusCode);
        Assert.IsTrue(exception.Retryable);
        Assert.AreEqual(1, exception.RetryAfterSeconds);
    }
}

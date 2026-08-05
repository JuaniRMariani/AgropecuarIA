using System.Text;
using AgropecuarIA.StorageRecoverySpike;

namespace AgropecuarIA.StorageRecoverySpike.Tests;

[TestClass]
public sealed class FileWorkflowTests
{
    private static readonly byte[] Pdf = "%PDF-1.7\nsynthetic fixture"u8.ToArray();
    private static readonly ActorContext TenantA = new("tenant-a-private-id", "actor-a@example.invalid");
    private static readonly ActorContext TenantB = new("tenant-b-private-id", "actor-b@example.invalid");
    private static readonly ActorContext UnprivilegedTenantA = new("tenant-a-private-id", "unprivileged@example.invalid");
    private static readonly OperatorContext BackupOperator = new("backup-operator@example.invalid");

    [TestMethod]
    public async Task CleanFileIsQuarantinedUntilExactScanThenDownloadable()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);

        var uploaded = await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);

        Assert.AreEqual(FileState.Uploaded, uploaded.State);
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(
            () => Task.FromResult(workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId)));

        var scan = await workbench.Workflow.ScanAsync(TenantA, intent.FileId, new SyntheticMalwareScanner(), "synthetic/1");
        var grant = workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId);
        var downloaded = await workbench.Workflow.DownloadAsync(TenantA, grant.DownloadToken);

        Assert.AreEqual(ScanVerdict.Clean, scan.Verdict);
        CollectionAssert.AreEqual(Pdf, downloaded);
        Assert.AreEqual(FileState.Available, workbench.Workflow.Get(TenantA, intent.FileId).State);
    }

    [TestMethod]
    public async Task ResourceOperationsAreTenantIsolatedWithNeutralDenial()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);

        var getError = Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.Get(TenantB, intent.FileId));
        var uploadError = await Assert.ThrowsExactlyAsync<ResourceDeniedException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantB, intent.UploadToken, Pdf));
        var scanError = await Assert.ThrowsExactlyAsync<ResourceDeniedException>(
            () => workbench.Workflow.ScanAsync(TenantB, intent.FileId, new SyntheticMalwareScanner(), "synthetic/1"));
        var resourceError = Assert.ThrowsExactly<ResourceDeniedException>(
            () => workbench.Workflow.Get(UnprivilegedTenantA, intent.FileId));

        Assert.AreEqual(getError.Message, uploadError.Message);
        Assert.AreEqual(getError.Message, scanError.Message);
        Assert.AreEqual(getError.Message, resourceError.Message);
        Assert.DoesNotContain(intent.FileId.ToString(), getError.Message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task DeclaredMimeMustMatchMagicBytes()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA, declaredMediaType: "image/png");

        var error = await Assert.ThrowsExactlyAsync<StorageValidationException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf));

        Assert.AreEqual("media_type_mismatch", error.Code);
        Assert.AreEqual(FileState.PendingUpload, workbench.Workflow.Get(TenantA, intent.FileId).State);
    }

    [TestMethod]
    public async Task UnknownMagicAndEmptyOrOversizedObjectsAreRejected()
    {
        using var workbench = new Workbench();
        var unknown = workbench.CreateIntent(TenantA);
        var empty = workbench.CreateIntent(TenantA);
        var oversized = workbench.CreateIntent(TenantA);

        var unknownError = await Assert.ThrowsExactlyAsync<StorageValidationException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantA, unknown.UploadToken, "not-pdf"u8.ToArray()));
        var emptyError = await Assert.ThrowsExactlyAsync<StorageValidationException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantA, empty.UploadToken, Array.Empty<byte>()));
        var oversizedBytes = new byte[FileWorkflow.MaximumBytes + 1];
        "%PDF-"u8.CopyTo(oversizedBytes);
        var oversizedError = await Assert.ThrowsExactlyAsync<StorageValidationException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantA, oversized.UploadToken, oversizedBytes));

        Assert.AreEqual("media_type_mismatch", unknownError.Code);
        Assert.AreEqual("invalid_size", emptyError.Code);
        Assert.AreEqual("invalid_size", oversizedError.Code);
    }

    [TestMethod]
    public async Task UploadTokenIsOneShotAndObjectVersionIsImmutable()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);

        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf));
        await Assert.ThrowsExactlyAsync<IOException>(
            () => workbench.Store.CreateAsync(ObjectKey(workbench.Workflow.Get(TenantA, intent.FileId)), Pdf, CancellationToken.None));
    }

    [TestMethod]
    public async Task ExpiredOrTamperedUploadTokenIsNeutrallyDenied()
    {
        using var workbench = new Workbench();
        var expired = workbench.CreateIntent(TenantA, lifetime: TimeSpan.FromMinutes(1));
        workbench.Clock.Advance(TimeSpan.FromMinutes(2));

        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantA, expired.UploadToken, Pdf));
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(
            () => workbench.Workflow.CompleteUploadAsync(TenantA, Tamper(expired.UploadToken), Pdf));
    }

    [TestMethod]
    public async Task ThreatMarkerNeverBecomesAvailable()
    {
        using var workbench = new Workbench();
        var content = Encoding.ASCII.GetBytes("%PDF-1.7\nAGROPECUARIA_TEST_THREAT\nsynthetic only");
        var intent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, content);

        var result = await workbench.Workflow.ScanAsync(TenantA, intent.FileId, new SyntheticMalwareScanner(), "synthetic/1");

        Assert.AreEqual(ScanVerdict.Threat, result.Verdict);
        Assert.AreEqual(FileState.Quarantined, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId));
    }

    [TestMethod]
    [DataRow(ScanVerdict.Unsupported, FileState.Rejected)]
    [DataRow(ScanVerdict.AccessDenied, FileState.ScanFailed)]
    [DataRow(ScanVerdict.Failed, FileState.ScanFailed)]
    public async Task NonCleanScannerVerdictsFailClosed(ScanVerdict verdict, FileState expectedState)
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);

        await workbench.Workflow.ScanAsync(TenantA, intent.FileId, new SyntheticMalwareScanner(verdict), "synthetic/1");

        Assert.AreEqual(expectedState, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId));
    }

    [TestMethod]
    public async Task ScannerOutageIsRecordedAsScanFailed()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);

        var result = await workbench.Workflow.ScanAsync(TenantA, intent.FileId, new UnavailableScanner(), "unavailable/1");

        Assert.AreEqual(ScanVerdict.Failed, result.Verdict);
        Assert.AreEqual(FileState.ScanFailed, workbench.Workflow.Get(TenantA, intent.FileId).State);
    }

    [TestMethod]
    public async Task ScanFailureCanRetryWithNextSequenceButThreatCannot()
    {
        using var workbench = new Workbench();
        var failedIntent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, failedIntent.UploadToken, Pdf);
        var failed = await workbench.Workflow.ScanAsync(TenantA, failedIntent.FileId, new UnavailableScanner(), "unavailable/1");

        var recovered = await workbench.Workflow.ScanAsync(TenantA, failedIntent.FileId, new SyntheticMalwareScanner(), "synthetic/1");

        Assert.AreEqual(1, failed.Sequence);
        Assert.AreEqual(2, recovered.Sequence);
        Assert.AreEqual(FileState.Available, workbench.Workflow.Get(TenantA, failedIntent.FileId).State);

        var threatIntent = workbench.CreateIntent(TenantA);
        var threatContent = Encoding.ASCII.GetBytes("%PDF-1.7\nAGROPECUARIA_TEST_THREAT");
        await workbench.Workflow.CompleteUploadAsync(TenantA, threatIntent.UploadToken, threatContent);
        await workbench.Workflow.ScanAsync(TenantA, threatIntent.FileId, new SyntheticMalwareScanner(), "synthetic/1");

        Assert.ThrowsExactly<StorageConflictException>(() => workbench.Workflow.BeginScan(TenantA, threatIntent.FileId));

        var invalidIntent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, invalidIntent.UploadToken, Pdf);
        var invalid = await workbench.Workflow.ScanAsync(TenantA, invalidIntent.FileId, new InvalidVerdictScanner(), "invalid/1");
        Assert.AreEqual(ScanVerdict.Failed, invalid.Verdict);
        Assert.AreEqual(FileState.ScanFailed, workbench.Workflow.Get(TenantA, invalidIntent.FileId).State);
    }

    [TestMethod]
    public async Task ScanValidatesDependenciesAndCancellationBeforeChangingState()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);
        await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => workbench.Workflow.ScanAsync(TenantA, intent.FileId, null!, "scanner/1"));
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => workbench.Workflow.ScanAsync(TenantA, intent.FileId, new SyntheticMalwareScanner(), string.Empty));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => workbench.Workflow.ScanAsync(TenantA, intent.FileId, new SyntheticMalwareScanner(), "scanner/1", cancellation.Token));

        Assert.AreEqual(FileState.Uploaded, workbench.Workflow.Get(TenantA, intent.FileId).State);
    }

    [TestMethod]
    public async Task InterruptedOrUnexpectedScanIsAuditedAsFailedAndCanRetry()
    {
        using var cancellationWorkbench = new Workbench();
        var cancelledIntent = cancellationWorkbench.CreateIntent(TenantA);
        await cancellationWorkbench.Workflow.CompleteUploadAsync(TenantA, cancelledIntent.UploadToken, Pdf);
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => cancellationWorkbench.Workflow.ScanAsync(
                TenantA,
                cancelledIntent.FileId,
                new CancellingScanner(cancellation),
                "cancel/1",
                cancellation.Token));
        Assert.AreEqual(FileState.ScanFailed, cancellationWorkbench.Workflow.Get(TenantA, cancelledIntent.FileId).State);
        var recovered = await cancellationWorkbench.Workflow.ScanAsync(
            TenantA,
            cancelledIntent.FileId,
            new SyntheticMalwareScanner(),
            "synthetic/1");
        Assert.AreEqual(2, recovered.Sequence);

        using var unexpectedWorkbench = new Workbench();
        var unexpectedIntent = unexpectedWorkbench.CreateIntent(TenantA);
        await unexpectedWorkbench.Workflow.CompleteUploadAsync(TenantA, unexpectedIntent.UploadToken, Pdf);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => unexpectedWorkbench.Workflow.ScanAsync(TenantA, unexpectedIntent.FileId, new UnexpectedScanner(), "unexpected/1"));

        Assert.AreEqual(FileState.ScanFailed, unexpectedWorkbench.Workflow.Get(TenantA, unexpectedIntent.FileId).State);
        Assert.IsTrue(unexpectedWorkbench.Workflow.GetAuditEvents(BackupOperator).Any(static entry => entry.Action == "scan_failed"));
    }

    [TestMethod]
    public async Task ExactDuplicateScanResultIsIdempotentButMutatedDuplicateIsRejected()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);
        var uploaded = await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);
        var result = new ScanResult(intent.FileId, 1, 1, uploaded.Sha256!, ScanVerdict.Clean, "synthetic/1", workbench.Clock.UtcNow);
        var scanning = workbench.Workflow.BeginScan(TenantA, intent.FileId);

        var first = workbench.Workflow.ApplyScanResult(TenantA, result);
        var duplicate = workbench.Workflow.ApplyScanResult(TenantA, result);

        Assert.AreEqual(FileState.Scanning, scanning.State);
        Assert.AreEqual(first, duplicate);
        Assert.ThrowsExactly<StorageConflictException>(
            () => workbench.Workflow.ApplyScanResult(TenantA, result with { Verdict = ScanVerdict.Threat }));
    }

    [TestMethod]
    public async Task ScanResultMustBeOrderedAndMatchVersionAndHash()
    {
        using var workbench = new Workbench();
        var intent = workbench.CreateIntent(TenantA);
        var uploaded = await workbench.Workflow.CompleteUploadAsync(TenantA, intent.UploadToken, Pdf);
        var baseResult = new ScanResult(intent.FileId, 1, 1, uploaded.Sha256!, ScanVerdict.Clean, "synthetic/1", workbench.Clock.UtcNow);
        workbench.Workflow.BeginScan(TenantA, intent.FileId);

        Assert.ThrowsExactly<StorageConflictException>(
            () => workbench.Workflow.ApplyScanResult(TenantA, baseResult with { Sequence = 2 }));
        Assert.ThrowsExactly<StorageConflictException>(
            () => workbench.Workflow.ApplyScanResult(TenantA, baseResult with { Version = 2 }));
        Assert.ThrowsExactly<StorageConflictException>(
            () => workbench.Workflow.ApplyScanResult(TenantA, baseResult with { Sha256 = new string('0', 64) }));
        Assert.ThrowsExactly<StorageValidationException>(
            () => workbench.Workflow.ApplyScanResult(TenantA, baseResult with { Sequence = 0 }));
        Assert.ThrowsExactly<ArgumentException>(
            () => workbench.Workflow.ApplyScanResult(TenantA, baseResult with { ScannerRef = string.Empty }));

        var normalized = workbench.Workflow.ApplyScanResult(TenantA, baseResult with { Verdict = (ScanVerdict)999 });
        Assert.AreEqual(FileState.ScanFailed, normalized.State);
        var retry = await workbench.Workflow.ScanAsync(TenantA, intent.FileId, new SyntheticMalwareScanner(), "synthetic/2");
        Assert.AreEqual(2, retry.Sequence);
        Assert.AreEqual(ScanVerdict.Clean, retry.Verdict);
    }

    [TestMethod]
    public async Task DownloadGrantReauthorizesTenantAndRejectsExpiryOrTamper()
    {
        using var workbench = new Workbench();
        var intent = await workbench.CreateAvailableFileAsync(TenantA);
        var grant = workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId, TimeSpan.FromMinutes(1));

        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.DownloadAsync(TenantB, grant.DownloadToken));
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.DownloadAsync(TenantA, Tamper(grant.DownloadToken)));
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.DownloadAsync(TenantA, MakeNonCanonical(grant.DownloadToken)));
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.DownloadAsync(TenantA, grant.DownloadToken + "="));
        workbench.Clock.Advance(TimeSpan.FromMinutes(2));
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.DownloadAsync(TenantA, grant.DownloadToken));
    }

    [TestMethod]
    public async Task ActiveDownloadLeaseBlocksPurgeUntilReadCompletes()
    {
        BlockingReadObjectStore? blockingStore = null;
        using var workbench = new Workbench(root => blockingStore = new BlockingReadObjectStore(new LocalObjectStore(root)));
        var intent = await workbench.CreateAvailableFileAsync(TenantA);
        var grant = workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId);
        blockingStore!.BlockReads();

        var download = workbench.Workflow.DownloadAsync(TenantA, grant.DownloadToken);
        await blockingStore.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.ThrowsExactlyAsync<StorageConflictException>(() => workbench.Workflow.PurgeAsync(TenantA, intent.FileId));
        Assert.AreEqual(FileState.Available, workbench.Workflow.Get(TenantA, intent.FileId).State);

        blockingStore.AllowRead();
        CollectionAssert.AreEqual(Pdf, await download);
        await workbench.Workflow.PurgeAsync(TenantA, intent.FileId);
        Assert.AreEqual(FileState.Deleted, workbench.Workflow.Get(TenantA, intent.FileId).State);
    }

    [TestMethod]
    public async Task DownloadIoFailureIsNeutralAndDoesNotLeakStoragePath()
    {
        FailingReadObjectStore? failingStore = null;
        using var workbench = new Workbench(root => failingStore = new FailingReadObjectStore(new LocalObjectStore(root)));
        var intent = await workbench.CreateAvailableFileAsync(TenantA);
        var grant = workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId);
        failingStore!.FailReads();

        var error = await Assert.ThrowsExactlyAsync<ResourceDeniedException>(
            () => workbench.Workflow.DownloadAsync(TenantA, grant.DownloadToken));

        Assert.DoesNotContain("private-storage", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-storage", string.Join('\n', workbench.Telemetry.Entries), StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual(FileState.Available, workbench.Workflow.Get(TenantA, intent.FileId).State);

        failingStore.AllowReads();
        await workbench.Workflow.PurgeAsync(TenantA, intent.FileId);
        Assert.AreEqual(FileState.Deleted, workbench.Workflow.Get(TenantA, intent.FileId).State);
    }

    [TestMethod]
    public async Task SameHashInTwoTenantsUsesDistinctObjectKeys()
    {
        using var workbench = new Workbench();
        var a = workbench.CreateIntent(TenantA);
        var b = workbench.CreateIntent(TenantB);
        var fileA = await workbench.Workflow.CompleteUploadAsync(TenantA, a.UploadToken, Pdf);
        var fileB = await workbench.Workflow.CompleteUploadAsync(TenantB, b.UploadToken, Pdf);

        Assert.AreEqual(fileA.Sha256, fileB.Sha256);
        Assert.AreNotEqual(fileA.TenantRef, fileB.TenantRef);
        Assert.AreNotEqual(ObjectKey(fileA), ObjectKey(fileB));
        Assert.IsTrue(await workbench.Store.ExistsAsync(ObjectKey(fileA), CancellationToken.None));
        Assert.IsTrue(await workbench.Store.ExistsAsync(ObjectKey(fileB), CancellationToken.None));
    }

    [TestMethod]
    public async Task LegalHoldBlocksPurgeUntilExplicitlyReleased()
    {
        using var workbench = new Workbench();
        var intent = await workbench.CreateAvailableFileAsync(TenantA);
        var file = workbench.Workflow.SetLegalHold(TenantA, intent.FileId, true);

        await Assert.ThrowsExactlyAsync<StorageConflictException>(() => workbench.Workflow.PurgeAsync(TenantA, intent.FileId));
        Assert.IsTrue(await workbench.Store.ExistsAsync(ObjectKey(file), CancellationToken.None));

        workbench.Workflow.SetLegalHold(TenantA, intent.FileId, false);
        await workbench.Workflow.PurgeAsync(TenantA, intent.FileId);
        Assert.AreEqual(FileState.Deleted, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.IsFalse(await workbench.Store.ExistsAsync(ObjectKey(file), CancellationToken.None));
    }

    [TestMethod]
    public async Task PurgeTransitionPreventsConcurrentLegalHoldChange()
    {
        BlockingDeleteObjectStore? blockingStore = null;
        using var workbench = new Workbench(root => blockingStore = new BlockingDeleteObjectStore(new LocalObjectStore(root)));
        var intent = await workbench.CreateAvailableFileAsync(TenantA);

        var purge = workbench.Workflow.PurgeAsync(TenantA, intent.FileId);
        await blockingStore!.DeleteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(FileState.Purging, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.ThrowsExactly<StorageConflictException>(() => workbench.Workflow.SetLegalHold(TenantA, intent.FileId, true));

        blockingStore.AllowDelete();
        await purge;
        Assert.AreEqual(FileState.Deleted, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.ThrowsExactly<StorageConflictException>(() => workbench.Workflow.SetLegalHold(TenantA, intent.FileId, true));
    }

    [TestMethod]
    public async Task FailedDeleteRemainsUncertainUntilOperatorConfirmsObjectExists()
    {
        using var workbench = new Workbench(root => new FailingDeleteObjectStore(new LocalObjectStore(root)));
        var intent = await workbench.CreateAvailableFileAsync(TenantA);

        var error = await Assert.ThrowsExactlyAsync<StorageConflictException>(() => workbench.Workflow.PurgeAsync(TenantA, intent.FileId));

        Assert.AreEqual(FileState.PurgeUncertain, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.DoesNotContain("Synthetic path", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.ThrowsExactly<StorageConflictException>(() => workbench.Workflow.SetLegalHold(TenantA, intent.FileId, true));
        await Assert.ThrowsExactlyAsync<StorageConflictException>(() => workbench.Workflow.PurgeAsync(TenantA, intent.FileId));
        var report = await workbench.Workflow.ReconcileAsync(BackupOperator);
        CollectionAssert.Contains(report.PurgeUncertainFileIds.ToArray(), intent.FileId);

        var resolved = await workbench.Workflow.ResolvePurgeUncertainAsync(BackupOperator, intent.FileId);
        Assert.AreEqual(FileState.Available, resolved.State);
        Assert.IsTrue(workbench.Workflow.SetLegalHold(TenantA, intent.FileId, true).LegalHold);
        Assert.IsTrue(workbench.Workflow.GetAuditEvents(BackupOperator).Any(static entry => entry.Action == "purge_uncertain"));
        Assert.IsTrue(workbench.Workflow.GetAuditEvents(BackupOperator).Any(static entry => entry.Action == "purge_reconciled_present"));
    }

    [TestMethod]
    public async Task DeleteThenThrowNeverRestoresAvailabilityAndOperatorConfirmsDeletion()
    {
        using var workbench = new Workbench(root => new DeleteThenThrowObjectStore(new LocalObjectStore(root)));
        var intent = await workbench.CreateAvailableFileAsync(TenantA);

        var error = await Assert.ThrowsExactlyAsync<StorageConflictException>(() => workbench.Workflow.PurgeAsync(TenantA, intent.FileId));

        Assert.AreEqual(FileState.PurgeUncertain, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.DoesNotContain("private-delete-path", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId));
        await Assert.ThrowsExactlyAsync<StorageConflictException>(() => workbench.Workflow.PurgeAsync(TenantA, intent.FileId));
        var report = await workbench.Workflow.ReconcileAsync(BackupOperator);
        CollectionAssert.Contains(report.MissingFileIds.ToArray(), intent.FileId);
        CollectionAssert.Contains(report.PurgeUncertainFileIds.ToArray(), intent.FileId);

        var resolved = await workbench.Workflow.ResolvePurgeUncertainAsync(BackupOperator, intent.FileId);
        Assert.AreEqual(FileState.Deleted, resolved.State);
    }

    [TestMethod]
    public async Task DeleteThenCancelNeverRestoresAvailability()
    {
        using var cancellation = new CancellationTokenSource();
        using var workbench = new Workbench(root => new DeleteThenCancelObjectStore(new LocalObjectStore(root), cancellation));
        var intent = await workbench.CreateAvailableFileAsync(TenantA);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => workbench.Workflow.PurgeAsync(TenantA, intent.FileId, cancellation.Token));

        Assert.AreEqual(FileState.PurgeUncertain, workbench.Workflow.Get(TenantA, intent.FileId).State);
        Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId));
        var resolved = await workbench.Workflow.ResolvePurgeUncertainAsync(BackupOperator, intent.FileId);
        Assert.AreEqual(FileState.Deleted, resolved.State);
    }

    [TestMethod]
    public async Task LegalHoldApplyAndReleaseRequireSeparatePermissions()
    {
        var authorizer = new DelegateResourceAuthorizer(
            static (actor, resource, action) =>
                (actor == TenantA || actor == TenantB)
                && resource.Type == "field_observation"
                && action != "legal_hold_release");
        using var workbench = new Workbench(resourceAuthorizer: authorizer);
        var intent = await workbench.CreateAvailableFileAsync(TenantA);

        Assert.IsTrue(workbench.Workflow.SetLegalHold(TenantA, intent.FileId, true).LegalHold);
        Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.SetLegalHold(TenantA, intent.FileId, false));
    }

    [TestMethod]
    public async Task ReconciliationReportsOrphanAndMissingObjectsWithoutDeletingThem()
    {
        using var workbench = new Workbench();
        var available = await workbench.CreateAvailableFileAsync(TenantA);
        var known = workbench.Workflow.Get(TenantA, available.FileId);
        await workbench.Store.DeleteAsync(ObjectKey(known), CancellationToken.None);
        const string orphanKey = "tenants/0123456789abcdef/quarantine/0123456789abcdef0123456789abcdef/v1";
        await workbench.Store.CreateAsync(orphanKey, Pdf, CancellationToken.None);

        var report = await workbench.Workflow.ReconcileAsync(BackupOperator);

        CollectionAssert.Contains(report.MissingFileIds.ToArray(), available.FileId);
        CollectionAssert.Contains(report.OrphanObjectKeys.ToArray(), orphanKey);
        Assert.IsTrue(await workbench.Store.ExistsAsync(orphanKey, CancellationToken.None));
    }

    [TestMethod]
    public async Task AuditAndReconciliationRequireExplicitPrivilegedOperator()
    {
        using var workbench = new Workbench();
        var tenantAsOperator = new OperatorContext(TenantA.ActorId);
        var unprivileged = new OperatorContext("observer@example.invalid");

        Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.GetAuditEvents(tenantAsOperator));
        Assert.ThrowsExactly<ResourceDeniedException>(() => workbench.Workflow.GetAuditEvents(unprivileged));
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.ReconcileAsync(tenantAsOperator));
        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.ReconcileAsync(unprivileged));
    }

    [TestMethod]
    public async Task AuditIsOrderedAndTelemetryExcludesRawTenantNameActorAndTokens()
    {
        using var workbench = new Workbench();
        var intent = await workbench.CreateAvailableFileAsync(TenantA, "sensitive-name.pdf");
        var grant = workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId);
        await workbench.Workflow.DownloadAsync(TenantA, grant.DownloadToken);

        var audit = workbench.Workflow.GetAuditEvents(BackupOperator);
        Assert.IsTrue(audit.Count >= 5);
        CollectionAssert.AreEqual(Enumerable.Range(1, audit.Count).Select(static value => (long)value).ToArray(), audit.Select(static entry => entry.Sequence).ToArray());

        var telemetry = string.Join('\n', workbench.Telemetry.Entries);
        Assert.DoesNotContain(TenantA.TenantId, telemetry, StringComparison.Ordinal);
        Assert.DoesNotContain(TenantA.ActorId, telemetry, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-name.pdf", telemetry, StringComparison.Ordinal);
        Assert.DoesNotContain(intent.UploadToken, telemetry, StringComparison.Ordinal);
        Assert.DoesNotContain(grant.DownloadToken, telemetry, StringComparison.Ordinal);
        Assert.DoesNotContain(intent.FileId.ToString(), telemetry, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(intent.FileId.ToString("N"), telemetry, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task CorruptedObjectIsDeniedAtDownload()
    {
        using var workbench = new Workbench();
        var intent = await workbench.CreateAvailableFileAsync(TenantA);
        var file = workbench.Workflow.Get(TenantA, intent.FileId);
        await workbench.Store.DeleteAsync(ObjectKey(file), CancellationToken.None);
        await workbench.Store.CreateAsync(ObjectKey(file), "%PDF-1.7\ncorrupted"u8.ToArray(), CancellationToken.None);
        var grant = workbench.Workflow.CreateDownloadGrant(TenantA, intent.FileId);

        await Assert.ThrowsExactlyAsync<ResourceDeniedException>(() => workbench.Workflow.DownloadAsync(TenantA, grant.DownloadToken));
        Assert.IsTrue(workbench.Telemetry.Entries.Any(static entry => entry.Contains("integrity_failed", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void MediaDetectionAcceptsOnlyExplicitSignatures()
    {
        Assert.AreEqual("application/pdf", FileWorkflow.DetectMediaType(Pdf));
        Assert.AreEqual("image/jpeg", FileWorkflow.DetectMediaType([0xFF, 0xD8, 0xFF, 0x01]));
        Assert.AreEqual("image/png", FileWorkflow.DetectMediaType([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
        Assert.AreEqual(string.Empty, FileWorkflow.DetectMediaType("GIF89a"u8));
    }

    [TestMethod]
    public void PublicFilesAreModeledButSecretsAreRejected()
    {
        using var workbench = new Workbench();
        var publicIntent = workbench.Workflow.CreateUploadIntent(
            TenantA,
            "public.pdf",
            "application/pdf",
            FileClassification.Public,
            new LinkedResource("field_observation", Guid.Parse("11111111-1111-1111-1111-111111111111")));

        Assert.AreEqual(FileClassification.Public, workbench.Workflow.Get(TenantA, publicIntent.FileId).Classification);
        var error = Assert.ThrowsExactly<StorageValidationException>(() => workbench.Workflow.CreateUploadIntent(
            TenantA,
            "secret.pdf",
            "application/pdf",
            FileClassification.Secret,
            new LinkedResource("field_observation", Guid.Parse("11111111-1111-1111-1111-111111111111"))));
        Assert.AreEqual("secret_not_allowed", error.Code);
    }

    private static string Tamper(string token)
    {
        var replacement = token[^1] == 'A' ? 'B' : 'A';
        return token[..^1] + replacement;
    }

    private static string MakeNonCanonical(string token)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var separator = token.IndexOf('.', StringComparison.Ordinal);
        var signature = token[(separator + 1)..];
        var canonicalIndex = alphabet.IndexOf(signature[^1], StringComparison.Ordinal);
        Assert.AreEqual(0, canonicalIndex & 0b11, "The SHA-256 signature must end with two unused Base64 bits.");
        var alternate = alphabet[canonicalIndex + 1];
        return token[..^1] + alternate;
    }

    private static string ObjectKey(StoredFileSnapshot file) =>
        $"tenants/{file.TenantRef}/quarantine/{file.FileId:N}/v{file.Version}";

    private sealed class UnavailableScanner : IMalwareScanner
    {
        public Task<ScanVerdict> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            throw new ScannerUnavailableException("Synthetic outage.");
    }

    private sealed class UnexpectedScanner : IMalwareScanner
    {
        public Task<ScanVerdict> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Synthetic unexpected scanner failure.");
    }

    private sealed class InvalidVerdictScanner : IMalwareScanner
    {
        public Task<ScanVerdict> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            Task.FromResult((ScanVerdict)999);
    }

    private sealed class CancellingScanner(CancellationTokenSource cancellation) : IMalwareScanner
    {
        public Task<ScanVerdict> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class BlockingDeleteObjectStore(IObjectStore inner) : IObjectStore
    {
        private readonly TaskCompletionSource deleteAllowed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource DeleteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            inner.CreateAsync(key, content, cancellationToken);

        public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken) =>
            inner.ReadAsync(key, cancellationToken);

        public async Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeleteStarted.TrySetResult();
            await deleteAllowed.Task.WaitAsync(cancellationToken);
            await inner.DeleteAsync(key, cancellationToken);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            inner.ExistsAsync(key, cancellationToken);

        public IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken) =>
            inner.ListKeysAsync(cancellationToken);

        public void AllowDelete() => deleteAllowed.TrySetResult();
    }

    private sealed class BlockingReadObjectStore(IObjectStore inner) : IObjectStore
    {
        private readonly TaskCompletionSource readAllowed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool shouldBlock;

        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            inner.CreateAsync(key, content, cancellationToken);

        public async Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken)
        {
            if (shouldBlock)
            {
                ReadStarted.TrySetResult();
                await readAllowed.Task.WaitAsync(cancellationToken);
            }

            return await inner.ReadAsync(key, cancellationToken);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
            inner.DeleteAsync(key, cancellationToken);

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            inner.ExistsAsync(key, cancellationToken);

        public IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken) =>
            inner.ListKeysAsync(cancellationToken);

        public void BlockReads() => shouldBlock = true;

        public void AllowRead() => readAllowed.TrySetResult();
    }

    private sealed class FailingReadObjectStore(IObjectStore inner) : IObjectStore
    {
        private bool shouldFail;

        public Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            inner.CreateAsync(key, content, cancellationToken);

        public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken) =>
            shouldFail
                ? throw new FileNotFoundException("Object missing at C:\\private-storage\\tenant\\object.")
                : inner.ReadAsync(key, cancellationToken);

        public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
            inner.DeleteAsync(key, cancellationToken);

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            inner.ExistsAsync(key, cancellationToken);

        public IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken) =>
            inner.ListKeysAsync(cancellationToken);

        public void FailReads() => shouldFail = true;

        public void AllowReads() => shouldFail = false;
    }

    private sealed class FailingDeleteObjectStore(IObjectStore inner) : IObjectStore
    {
        public Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            inner.CreateAsync(key, content, cancellationToken);

        public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken) =>
            inner.ReadAsync(key, cancellationToken);

        public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
            throw new IOException("Synthetic path that must never escape.");

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            inner.ExistsAsync(key, cancellationToken);

        public IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken) =>
            inner.ListKeysAsync(cancellationToken);
    }

    private sealed class DeleteThenThrowObjectStore(IObjectStore inner) : IObjectStore
    {
        public Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            inner.CreateAsync(key, content, cancellationToken);

        public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken) =>
            inner.ReadAsync(key, cancellationToken);

        public async Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            await inner.DeleteAsync(key, cancellationToken);
            throw new IOException("Delete may have completed at C:\\private-delete-path\\object.");
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            inner.ExistsAsync(key, cancellationToken);

        public IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken) =>
            inner.ListKeysAsync(cancellationToken);
    }

    private sealed class DeleteThenCancelObjectStore(IObjectStore inner, CancellationTokenSource cancellation) : IObjectStore
    {
        public Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
            inner.CreateAsync(key, content, cancellationToken);

        public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken) =>
            inner.ReadAsync(key, cancellationToken);

        public async Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            await inner.DeleteAsync(key, cancellationToken);
            cancellation.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            inner.ExistsAsync(key, cancellationToken);

        public IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken) =>
            inner.ListKeysAsync(cancellationToken);
    }

    private sealed class ManualClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }

    private sealed class Workbench : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"agro-dis-005-{Guid.NewGuid():N}");

        public Workbench(
            Func<string, IObjectStore>? storeFactory = null,
            IResourceAuthorizer? resourceAuthorizer = null,
            IOperationsAuthorizer? operationsAuthorizer = null)
        {
            Clock = new ManualClock(new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero));
            Store = storeFactory?.Invoke(root) ?? new LocalObjectStore(root);
            Telemetry = new InMemorySafeTelemetry();
            Workflow = new FileWorkflow(
                Store,
                Clock,
                Telemetry,
                resourceAuthorizer ?? new FixtureResourceAuthorizer(),
                operationsAuthorizer ?? new FixtureOperationsAuthorizer(),
                Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray(),
                Enumerable.Range(33, 16).Select(static value => (byte)value).ToArray());
        }

        public ManualClock Clock { get; }
        public IObjectStore Store { get; }
        public InMemorySafeTelemetry Telemetry { get; }
        public FileWorkflow Workflow { get; }

        public UploadIntent CreateIntent(
            ActorContext actor,
            string displayName = "fixture.pdf",
            string declaredMediaType = "application/pdf",
            TimeSpan? lifetime = null) =>
            Workflow.CreateUploadIntent(
                actor,
                displayName,
                declaredMediaType,
                FileClassification.Confidential,
                new LinkedResource("field_observation", Guid.Parse("11111111-1111-1111-1111-111111111111")),
                lifetime);

        public async Task<UploadIntent> CreateAvailableFileAsync(ActorContext actor, string displayName = "fixture.pdf")
        {
            var intent = CreateIntent(actor, displayName);
            await Workflow.CompleteUploadAsync(actor, intent.UploadToken, Pdf);
            await Workflow.ScanAsync(actor, intent.FileId, new SyntheticMalwareScanner(), "synthetic/1");
            return intent;
        }

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class FixtureResourceAuthorizer : IResourceAuthorizer
    {
        public bool IsAllowed(ActorContext actor, LinkedResource resource, string action) =>
            (actor == TenantA || actor == TenantB)
            && resource.Type == "field_observation"
            && resource.Id == Guid.Parse("11111111-1111-1111-1111-111111111111")
            && action is "upload" or "scan" or "download" or "legal_hold_apply" or "legal_hold_release" or "purge" or "read";
    }

    private sealed class DelegateResourceAuthorizer(
        Func<ActorContext, LinkedResource, string, bool> authorize) : IResourceAuthorizer
    {
        public bool IsAllowed(ActorContext actor, LinkedResource resource, string action) =>
            authorize(actor, resource, action);
    }

    private sealed class FixtureOperationsAuthorizer : IOperationsAuthorizer
    {
        public bool IsAllowed(OperatorContext operatorContext, string scope) =>
            operatorContext == BackupOperator && scope is "audit_read" or "reconcile";
    }
}

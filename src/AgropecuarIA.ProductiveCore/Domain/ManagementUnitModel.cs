using System.Globalization;
using System.Text;

namespace AgropecuarIA.ProductiveCore.Domain;

public static class ManagementUnitTypes
{
    public const string Field = "field";
}

public static class ManagementUnitStatuses
{
    public const string Draft = "draft";
    public const string Archived = "archived";
}

public static class ManagementUnitSpatialStatuses
{
    public const string NotConfigured = "not_configured";
}

public static class ManagementUnitLimits
{
    public const int MaximumPerOrganization = 100;
}

public static class ManagementUnitCreationProtocol
{
    public const string ScopeKind = "tenant";
    public const string Namespace = "management_unit";
    public const string Operation = "create_field";

    public static class States
    {
        public const string InProgress = "in_progress";
        public const string Succeeded = "succeeded";
        public const string FailedTerminal = "failed_terminal";
        public const string ResponseExpired = "response_expired";
    }
}

public static class ManagementUnitRenameProtocol
{
    public const string ScopeKind = "tenant";
    public const string Namespace = "management_unit";
    public const string Operation = "rename_field";

    public static class States
    {
        public const string InProgress = "in_progress";
        public const string Succeeded = "succeeded";
        public const string FailedTerminal = "failed_terminal";
        public const string ResponseExpired = "response_expired";
    }
}

public static class ManagementUnitArchiveProtocol
{
    public const string ScopeKind = "tenant";
    public const string Namespace = "management_unit";
    public const string Operation = "archive_field";

    public static class States
    {
        public const string InProgress = "in_progress";
        public const string Succeeded = "succeeded";
        public const string FailedTerminal = "failed_terminal";
        public const string ResponseExpired = "response_expired";
    }
}

public sealed class ManagementUnit
{
    private ManagementUnit()
    {
    }

    public ManagementUnit(
        Guid id,
        Guid organizationId,
        string displayName,
        DateTimeOffset createdAtUtc,
        Guid version)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || version == Guid.Empty)
        {
            throw new ArgumentException("Management unit, organization, and version IDs are required.");
        }

        Id = id;
        OrganizationId = organizationId;
        DisplayName = NormalizeDisplayName(displayName);
        UnitType = ManagementUnitTypes.Field;
        Status = ManagementUnitStatuses.Draft;
        SpatialStatus = ManagementUnitSpatialStatuses.NotConfigured;
        CreatedAtUtc = createdAtUtc;
        Revision = 1;
        Version = version;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string UnitType { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string SpatialStatus { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public long Revision { get; private set; }

    public Guid Version { get; private set; }

    public void Rename(string displayName, Guid expectedVersion, Guid newVersion)
    {
        if (expectedVersion == Guid.Empty || newVersion == Guid.Empty)
        {
            throw new ArgumentException("Expected and replacement versions are required.");
        }

        if (Version != expectedVersion)
        {
            throw new ManagementUnitVersionConflictException();
        }

        string normalized = NormalizeDisplayName(displayName);
        if (string.Equals(DisplayName, normalized, StringComparison.Ordinal))
        {
            throw new ManagementUnitNoChangeException();
        }

        DisplayName = normalized;
        Revision = checked(Revision + 1);
        Version = newVersion;
    }

    public void Archive(Guid expectedVersion, Guid newVersion)
    {
        if (expectedVersion == Guid.Empty || newVersion == Guid.Empty)
        {
            throw new ArgumentException("Expected and replacement versions are required.");
        }

        if (Version != expectedVersion)
        {
            throw new ManagementUnitVersionConflictException();
        }

        if (Status != ManagementUnitStatuses.Draft)
        {
            throw new InvalidOperationException("Only drafts can be archived.");
        }

        Status = ManagementUnitStatuses.Archived;
        Revision = checked(Revision + 1);
        Version = newVersion;
    }

    public static string NormalizeDisplayName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);

        EnsureWellFormedUtf16(displayName);
        string normalized = TrimDisplayNameBoundary(displayName)
            .Normalize(NormalizationForm.FormC);
        int characterCount = 0;
        foreach (Rune rune in normalized.EnumerateRunes())
        {
            characterCount++;
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.Control)
            {
                throw new ArgumentException(
                    "The management unit display name cannot contain control characters.",
                    nameof(displayName));
            }
        }

        if (characterCount is < 2 or > 120)
        {
            throw new ArgumentException(
                "The management unit display name must contain between 2 and 120 characters.",
                nameof(displayName));
        }

        return normalized;
    }

    private static string TrimDisplayNameBoundary(string value)
    {
        int start = 0;
        while (start < value.Length)
        {
            Rune rune = Rune.GetRuneAt(value, start);
            if (!IsDisplayNameBoundary(rune))
            {
                break;
            }

            start += rune.Utf16SequenceLength;
        }

        int end = value.Length;
        while (end > start)
        {
            int runeStart = end - 1;
            if (char.IsLowSurrogate(value[runeStart]))
            {
                runeStart--;
            }

            Rune rune = Rune.GetRuneAt(value, runeStart);
            if (!IsDisplayNameBoundary(rune))
            {
                break;
            }

            end = runeStart;
        }

        return value[start..end];
    }

    private static bool IsDisplayNameBoundary(Rune rune) => rune.Value is
        >= 0x0009 and <= 0x000D or
        0x0020 or
        0x0085 or
        0x00A0 or
        0x1680 or
        >= 0x2000 and <= 0x200A or
        0x2028 or
        0x2029 or
        0x202F or
        0x205F or
        0x3000 or
        0xFEFF;

    private static void EnsureWellFormedUtf16(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (!char.IsSurrogate(value[index]))
            {
                continue;
            }

            if (!char.IsHighSurrogate(value[index]) ||
                index + 1 >= value.Length ||
                !char.IsLowSurrogate(value[index + 1]))
            {
                throw new ArgumentException(
                    "The management unit display name must contain well-formed Unicode.",
                    nameof(value));
            }

            index++;
        }
    }
}

public sealed class ManagementUnitVersionConflictException : Exception;

public sealed class ManagementUnitNoChangeException : Exception;

public sealed class ManagementUnitCreationLedger
{
    private ManagementUnitCreationLedger()
    {
    }

    public ManagementUnitCreationLedger(
        Guid id,
        Guid organizationId,
        Guid actorUserId,
        Guid sessionId,
        Guid authorizationVersion,
        byte[] requestFingerprint,
        Guid leaseOwner,
        DateTimeOffset startedAtUtc,
        DateTimeOffset leaseUntilUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || actorUserId == Guid.Empty ||
            sessionId == Guid.Empty || authorizationVersion == Guid.Empty || leaseOwner == Guid.Empty)
        {
            throw new ArgumentException("Ledger, tenant, actor, session, authorization, and lease IDs are required.");
        }

        if (requestFingerprint is not { Length: 32 })
        {
            throw new ArgumentException("The request fingerprint must contain 32 bytes.", nameof(requestFingerprint));
        }

        if (leaseUntilUtc <= startedAtUtc)
        {
            throw new ArgumentException("The ledger lease must expire after it starts.", nameof(leaseUntilUtc));
        }

        Id = id;
        OrganizationId = organizationId;
        ScopeKind = ManagementUnitCreationProtocol.ScopeKind;
        Namespace = ManagementUnitCreationProtocol.Namespace;
        Operation = ManagementUnitCreationProtocol.Operation;
        ContractVersion = 1;
        CanonicalizationVersion = 1;
        ActorUserId = actorUserId;
        SessionId = sessionId;
        AuthorizationVersion = authorizationVersion;
        RequestFingerprint = requestFingerprint.ToArray();
        State = ManagementUnitCreationProtocol.States.InProgress;
        LeaseOwner = leaseOwner;
        FenceToken = 1;
        LeaseUntilUtc = leaseUntilUtc;
        StartedAtUtc = startedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string ScopeKind { get; private set; } = string.Empty;

    public string Namespace { get; private set; } = string.Empty;

    public string Operation { get; private set; } = string.Empty;

    public int ContractVersion { get; private set; }

    public int CanonicalizationVersion { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid SessionId { get; private set; }

    public Guid AuthorizationVersion { get; private set; }

    public byte[] RequestFingerprint { get; private set; } = [];

    public string State { get; private set; } = string.Empty;

    public Guid? ManagementUnitId { get; private set; }

    public Guid? ResultVersion { get; private set; }

    public Guid LeaseOwner { get; private set; }

    public long FenceToken { get; private set; }

    public DateTimeOffset LeaseUntilUtc { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public void Complete(
        Guid leaseOwner,
        long fenceToken,
        Guid managementUnitId,
        Guid resultVersion,
        DateTimeOffset completedAtUtc)
    {
        if (State != ManagementUnitCreationProtocol.States.InProgress ||
            LeaseOwner != leaseOwner ||
            FenceToken != fenceToken)
        {
            throw new InvalidOperationException("Only the current fenced owner can complete this ledger entry.");
        }

        if (managementUnitId == Guid.Empty || resultVersion == Guid.Empty)
        {
            throw new ArgumentException("Management unit result IDs are required.");
        }

        if (completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException("Completion cannot precede the ledger start.", nameof(completedAtUtc));
        }

        ManagementUnitId = managementUnitId;
        ResultVersion = resultVersion;
        CompletedAtUtc = completedAtUtc;
        State = ManagementUnitCreationProtocol.States.Succeeded;
        Version = Guid.NewGuid();
    }
}

public sealed class ManagementUnitCreationKeyAlias
{
    private ManagementUnitCreationKeyAlias()
    {
    }

    public ManagementUnitCreationKeyAlias(
        Guid id,
        Guid ledgerId,
        Guid organizationId,
        string keyVersion,
        byte[] keyDigest,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || ledgerId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("Alias, ledger, and organization IDs are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);
        if (keyDigest is not { Length: 32 })
        {
            throw new ArgumentException("The idempotency key digest must contain 32 bytes.", nameof(keyDigest));
        }

        Id = id;
        LedgerId = ledgerId;
        OrganizationId = organizationId;
        ScopeKind = ManagementUnitCreationProtocol.ScopeKind;
        Namespace = ManagementUnitCreationProtocol.Namespace;
        Operation = ManagementUnitCreationProtocol.Operation;
        KeyVersion = keyVersion;
        KeyDigest = keyDigest.ToArray();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid LedgerId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string ScopeKind { get; private set; } = string.Empty;

    public string Namespace { get; private set; } = string.Empty;

    public string Operation { get; private set; } = string.Empty;

    public string KeyVersion { get; private set; } = string.Empty;

    public byte[] KeyDigest { get; private set; } = [];

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class ManagementUnitRenameLedger
{
    private ManagementUnitRenameLedger()
    {
    }

    public ManagementUnitRenameLedger(
        Guid id,
        Guid organizationId,
        Guid actorUserId,
        Guid sessionId,
        Guid authorizationVersion,
        Guid managementUnitId,
        Guid expectedVersion,
        byte[] requestFingerprint,
        Guid leaseOwner,
        DateTimeOffset startedAtUtc,
        DateTimeOffset leaseUntilUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || actorUserId == Guid.Empty ||
            sessionId == Guid.Empty || authorizationVersion == Guid.Empty ||
            managementUnitId == Guid.Empty || expectedVersion == Guid.Empty || leaseOwner == Guid.Empty)
        {
            throw new ArgumentException(
                "Ledger, tenant, actor, session, authorization, unit, version, and lease IDs are required.");
        }

        if (requestFingerprint is not { Length: 32 })
        {
            throw new ArgumentException("The request fingerprint must contain 32 bytes.", nameof(requestFingerprint));
        }

        if (leaseUntilUtc <= startedAtUtc)
        {
            throw new ArgumentException("The ledger lease must expire after it starts.", nameof(leaseUntilUtc));
        }

        Id = id;
        OrganizationId = organizationId;
        ScopeKind = ManagementUnitRenameProtocol.ScopeKind;
        Namespace = ManagementUnitRenameProtocol.Namespace;
        Operation = ManagementUnitRenameProtocol.Operation;
        ContractVersion = 1;
        CanonicalizationVersion = 1;
        ActorUserId = actorUserId;
        SessionId = sessionId;
        AuthorizationVersion = authorizationVersion;
        ManagementUnitId = managementUnitId;
        ExpectedVersion = expectedVersion;
        RequestFingerprint = requestFingerprint.ToArray();
        State = ManagementUnitRenameProtocol.States.InProgress;
        LeaseOwner = leaseOwner;
        FenceToken = 1;
        LeaseUntilUtc = leaseUntilUtc;
        StartedAtUtc = startedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string ScopeKind { get; private set; } = string.Empty;

    public string Namespace { get; private set; } = string.Empty;

    public string Operation { get; private set; } = string.Empty;

    public int ContractVersion { get; private set; }

    public int CanonicalizationVersion { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid SessionId { get; private set; }

    public Guid AuthorizationVersion { get; private set; }

    public Guid ManagementUnitId { get; private set; }

    public Guid ExpectedVersion { get; private set; }

    public byte[] RequestFingerprint { get; private set; } = [];

    public string State { get; private set; } = string.Empty;

    public string? ResultDisplayName { get; private set; }

    public Guid? ResultVersion { get; private set; }

    public long? ResultRevision { get; private set; }

    public Guid LeaseOwner { get; private set; }

    public long FenceToken { get; private set; }

    public DateTimeOffset LeaseUntilUtc { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public void Complete(
        Guid leaseOwner,
        long fenceToken,
        string resultDisplayName,
        Guid resultVersion,
        long resultRevision,
        DateTimeOffset completedAtUtc)
    {
        if (State != ManagementUnitRenameProtocol.States.InProgress ||
            LeaseOwner != leaseOwner ||
            FenceToken != fenceToken)
        {
            throw new InvalidOperationException("Only the current fenced owner can complete this ledger entry.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(resultDisplayName);
        if (resultVersion == Guid.Empty || resultRevision < 2)
        {
            throw new ArgumentException("A valid rename result is required.");
        }

        if (completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException("Completion cannot precede the ledger start.", nameof(completedAtUtc));
        }

        ResultDisplayName = ManagementUnit.NormalizeDisplayName(resultDisplayName);
        ResultVersion = resultVersion;
        ResultRevision = resultRevision;
        CompletedAtUtc = completedAtUtc;
        State = ManagementUnitRenameProtocol.States.Succeeded;
        Version = Guid.NewGuid();
    }
}

public sealed class ManagementUnitRenameKeyAlias
{
    private ManagementUnitRenameKeyAlias()
    {
    }

    public ManagementUnitRenameKeyAlias(
        Guid id,
        Guid ledgerId,
        Guid organizationId,
        string keyVersion,
        byte[] keyDigest,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || ledgerId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("Alias, ledger, and organization IDs are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);
        if (keyDigest is not { Length: 32 })
        {
            throw new ArgumentException("The idempotency key digest must contain 32 bytes.", nameof(keyDigest));
        }

        Id = id;
        LedgerId = ledgerId;
        OrganizationId = organizationId;
        ScopeKind = ManagementUnitRenameProtocol.ScopeKind;
        Namespace = ManagementUnitRenameProtocol.Namespace;
        Operation = ManagementUnitRenameProtocol.Operation;
        KeyVersion = keyVersion;
        KeyDigest = keyDigest.ToArray();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid LedgerId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string ScopeKind { get; private set; } = string.Empty;

    public string Namespace { get; private set; } = string.Empty;

    public string Operation { get; private set; } = string.Empty;

    public string KeyVersion { get; private set; } = string.Empty;

    public byte[] KeyDigest { get; private set; } = [];

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

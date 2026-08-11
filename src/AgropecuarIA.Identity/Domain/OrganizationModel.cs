namespace AgropecuarIA.Identity.Domain;

public static class OrganizationStatuses
{
    public const string Active = "active";
}

public static class OrganizationMembershipRoles
{
    public const string Owner = "owner";
}

public static class OrganizationCreationProtocol
{
    public const string ScopeKind = "platform";
    public const string Namespace = "organization-bootstrap";
    public const string Operation = "create_organization";

    public static class States
    {
        public const string InProgress = "in_progress";
        public const string Succeeded = "succeeded";
        public const string FailedTerminal = "failed_terminal";
        public const string ResponseExpired = "response_expired";
    }
}

public sealed class OrganizationDirectoryEntry
{
    private OrganizationDirectoryEntry()
    {
    }

    public OrganizationDirectoryEntry(
        Guid id,
        string displayName,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Organization and creator IDs are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        Id = id;
        DisplayName = displayName;
        Status = OrganizationStatuses.Active;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();
}

public sealed class OrganizationMembershipAssignment
{
    private OrganizationMembershipAssignment()
    {
    }

    public OrganizationMembershipAssignment(
        Guid id,
        Guid organizationId,
        Guid userId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentException("Membership, organization, and user IDs are required.");
        }

        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        Role = OrganizationMembershipRoles.Owner;
        Status = OrganizationStatuses.Active;
        SecurityVersion = 1;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public long SecurityVersion { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class OrganizationCreationLedger
{
    private OrganizationCreationLedger()
    {
    }

    public OrganizationCreationLedger(
        Guid id,
        Guid actorUserId,
        Guid sessionId,
        Guid authorizationVersion,
        byte[] requestFingerprint,
        Guid leaseOwner,
        DateTimeOffset startedAtUtc,
        DateTimeOffset leaseUntilUtc)
    {
        if (id == Guid.Empty || actorUserId == Guid.Empty || sessionId == Guid.Empty ||
            authorizationVersion == Guid.Empty || leaseOwner == Guid.Empty)
        {
            throw new ArgumentException("Ledger, actor, and lease owner IDs are required.");
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
        ScopeKind = OrganizationCreationProtocol.ScopeKind;
        Namespace = OrganizationCreationProtocol.Namespace;
        Operation = OrganizationCreationProtocol.Operation;
        ContractVersion = 1;
        CanonicalizationVersion = 1;
        ActorUserId = actorUserId;
        SessionId = sessionId;
        AuthorizationVersion = authorizationVersion;
        RequestFingerprint = requestFingerprint;
        State = OrganizationCreationProtocol.States.InProgress;
        LeaseOwner = leaseOwner;
        FenceToken = 1;
        LeaseUntilUtc = leaseUntilUtc;
        StartedAtUtc = startedAtUtc;
    }

    public Guid Id { get; private set; }

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

    public Guid? OrganizationId { get; private set; }

    public Guid? MembershipId { get; private set; }

    public Guid LeaseOwner { get; private set; }

    public long FenceToken { get; private set; }

    public DateTimeOffset LeaseUntilUtc { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public void Complete(
        Guid leaseOwner,
        long fenceToken,
        Guid organizationId,
        Guid membershipId,
        DateTimeOffset completedAtUtc)
    {
        if (State != OrganizationCreationProtocol.States.InProgress ||
            LeaseOwner != leaseOwner ||
            FenceToken != fenceToken)
        {
            throw new InvalidOperationException("Only the current fenced owner can complete this ledger entry.");
        }

        if (organizationId == Guid.Empty || membershipId == Guid.Empty)
        {
            throw new ArgumentException("Organization and membership results are required.");
        }

        if (completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException("Completion cannot precede the ledger start.", nameof(completedAtUtc));
        }

        OrganizationId = organizationId;
        MembershipId = membershipId;
        CompletedAtUtc = completedAtUtc;
        State = OrganizationCreationProtocol.States.Succeeded;
        Version = Guid.NewGuid();
    }
}

public sealed class OrganizationCreationKeyAlias
{
    private OrganizationCreationKeyAlias()
    {
    }

    public OrganizationCreationKeyAlias(
        Guid id,
        Guid ledgerId,
        string keyVersion,
        byte[] keyDigest,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || ledgerId == Guid.Empty)
        {
            throw new ArgumentException("Alias and ledger IDs are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(keyVersion);
        if (keyDigest is not { Length: 32 })
        {
            throw new ArgumentException("The idempotency key digest must contain 32 bytes.", nameof(keyDigest));
        }

        Id = id;
        LedgerId = ledgerId;
        ScopeKind = OrganizationCreationProtocol.ScopeKind;
        Namespace = OrganizationCreationProtocol.Namespace;
        Operation = OrganizationCreationProtocol.Operation;
        KeyVersion = keyVersion;
        KeyDigest = keyDigest;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid LedgerId { get; private set; }

    public string ScopeKind { get; private set; } = string.Empty;

    public string Namespace { get; private set; } = string.Empty;

    public string Operation { get; private set; } = string.Empty;

    public string KeyVersion { get; private set; } = string.Empty;

    public byte[] KeyDigest { get; private set; } = [];

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

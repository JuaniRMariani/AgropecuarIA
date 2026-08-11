namespace AgropecuarIA.Identity.Domain;

public static class OrganizationOwnerInvitationStatuses
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Revoked = "revoked";
    public const string Expired = "expired";
}

public sealed class OrganizationOwnerInvitation
{
    private OrganizationOwnerInvitation()
    {
    }

    public OrganizationOwnerInvitation(
        Guid id,
        Guid organizationId,
        Guid createdByUserId,
        Guid creationSessionId,
        string creationKeyVersion,
        byte[] creationKeyDigest,
        string tokenKeyVersion,
        byte[] tokenDigest,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || createdByUserId == Guid.Empty ||
            creationSessionId == Guid.Empty)
        {
            throw new ArgumentException("Invitation, organization, creator, and session IDs are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(creationKeyVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenKeyVersion);
        ValidateDigest(creationKeyDigest, nameof(creationKeyDigest));
        ValidateDigest(tokenDigest, nameof(tokenDigest));
        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("The invitation must expire after it is created.", nameof(expiresAtUtc));
        }

        Id = id;
        OrganizationId = organizationId;
        CreatedByUserId = createdByUserId;
        CreationSessionId = creationSessionId;
        CreationKeyVersion = creationKeyVersion;
        CreationKeyDigest = creationKeyDigest;
        TokenKeyVersion = tokenKeyVersion;
        TokenDigest = tokenDigest;
        Status = OrganizationOwnerInvitationStatuses.Pending;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid CreationSessionId { get; private set; }

    public string CreationKeyVersion { get; private set; } = string.Empty;

    public byte[] CreationKeyDigest { get; private set; } = [];

    public string TokenKeyVersion { get; private set; } = string.Empty;

    public byte[] TokenDigest { get; private set; } = [];

    public string Status { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }

    public Guid? AcceptedByUserId { get; private set; }

    public Guid? AcceptedMembershipId { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public string GetEffectiveStatus(DateTimeOffset now) =>
        Status == OrganizationOwnerInvitationStatuses.Pending && ExpiresAtUtc <= now
            ? OrganizationOwnerInvitationStatuses.Expired
            : Status;

    public void Accept(Guid userId, Guid membershipId, DateTimeOffset acceptedAtUtc)
    {
        if (userId == Guid.Empty || membershipId == Guid.Empty)
        {
            throw new ArgumentException("The accepting user and membership IDs are required.");
        }

        if (Status != OrganizationOwnerInvitationStatuses.Pending || acceptedAtUtc >= ExpiresAtUtc)
        {
            throw new InvalidOperationException("Only a pending, unexpired invitation can be accepted.");
        }

        if (acceptedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Acceptance cannot precede creation.", nameof(acceptedAtUtc));
        }

        Status = OrganizationOwnerInvitationStatuses.Accepted;
        AcceptedAtUtc = acceptedAtUtc;
        AcceptedByUserId = userId;
        AcceptedMembershipId = membershipId;
        Version = Guid.NewGuid();
    }

    public void Revoke(Guid userId, DateTimeOffset revokedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("The revoking user ID is required.", nameof(userId));
        }

        if (Status != OrganizationOwnerInvitationStatuses.Pending || revokedAtUtc >= ExpiresAtUtc)
        {
            throw new InvalidOperationException("Only a pending, unexpired invitation can be revoked.");
        }

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException("Revocation cannot precede creation.", nameof(revokedAtUtc));
        }

        Status = OrganizationOwnerInvitationStatuses.Revoked;
        RevokedAtUtc = revokedAtUtc;
        RevokedByUserId = userId;
        Version = Guid.NewGuid();
    }

    private static void ValidateDigest(byte[] digest, string parameterName)
    {
        if (digest is not { Length: 32 })
        {
            throw new ArgumentException("The HMAC digest must contain 32 bytes.", parameterName);
        }
    }
}

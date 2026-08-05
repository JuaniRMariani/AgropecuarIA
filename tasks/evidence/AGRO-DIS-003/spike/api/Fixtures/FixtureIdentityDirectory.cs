using AgropecuarIA.IdentitySpike.Api.Linking;
using AgropecuarIA.IdentitySpike.Api.Sessions;

namespace AgropecuarIA.IdentitySpike.Api.Fixtures;

internal sealed class FixtureIdentityDirectory
{
    internal static readonly Guid OrganizationAId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    internal static readonly Guid OrganizationBId = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    internal static readonly Guid NoOrganizationUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa0");
    internal static readonly Guid OneOrganizationUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid NoReadPermissionUserId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    internal static readonly Guid ManyOrganizationsUserId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static readonly Uri FixtureEmailIssuer = new("https://fake-idp.invalid/email");
    private static readonly Uri FixtureGoogleIssuer = new("https://fake-idp.invalid/google");
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ExternalIdentity> _primaryIdentities = new()
    {
        [NoOrganizationUserId] = new(FixtureEmailIssuer, "fixture-user-zero"),
        [OneOrganizationUserId] = new(FixtureEmailIssuer, "user-a"),
        [NoReadPermissionUserId] = new(FixtureEmailIssuer, "user-b"),
        [ManyOrganizationsUserId] = new(FixtureGoogleIssuer, "user-shared")
    };
    private readonly Dictionary<ExternalIdentityKey, Guid> _linkedIdentities;

    internal FixtureIdentityDirectory()
    {
        _linkedIdentities = _primaryIdentities.ToDictionary(
            pair => ExternalIdentityKey.From(pair.Value),
            pair => pair.Key);
        _linkedIdentities[new ExternalIdentityKey(FixtureEmailIssuer.AbsoluteUri, "already-linked")] =
            OneOrganizationUserId;
    }

    internal bool UserExists(Guid userId) => _primaryIdentities.ContainsKey(userId);

    internal ExternalIdentity GetPrimaryIdentity(Guid userId) => _primaryIdentities[userId];

    internal static IReadOnlyList<OrganizationMembership> GetActiveMemberships(Guid userId)
    {
        if (userId == OneOrganizationUserId)
        {
            return
            [
                new(
                    Guid.Parse("a1111111-1111-4111-8111-111111111111"),
                    OrganizationAId,
                    "Organización sintética A",
                    "active",
                    ["tenant-record.read"])
            ];
        }

        if (userId == ManyOrganizationsUserId)
        {
            return
            [
                new(
                    Guid.Parse("a2222222-2222-4222-8222-222222222221"),
                    OrganizationAId,
                    "Organización sintética A",
                    "active",
                    ["tenant-record.read"]),
                new(
                    Guid.Parse("a2222222-2222-4222-8222-222222222222"),
                    OrganizationBId,
                    "Organización sintética B",
                    "active",
                    ["tenant-record.read"])
            ];
        }

        if (userId == NoReadPermissionUserId)
        {
            return
            [
                new(
                    Guid.Parse("a3333333-3333-4333-8333-333333333333"),
                    OrganizationBId,
                    "Organización sintética B",
                    "active",
                    [])
            ];
        }

        return [];
    }

    internal LinkIdentityResult TryLink(Guid userId, ExternalIdentity candidate)
    {
        var key = ExternalIdentityKey.From(candidate);

        lock (_sync)
        {
            if (_linkedIdentities.TryGetValue(key, out var linkedUserId))
            {
                return linkedUserId == userId
                    ? LinkIdentityResult.AlreadyLinkedToCurrentUser
                    : LinkIdentityResult.LinkedToAnotherUser;
            }

            _linkedIdentities[key] = userId;
            return LinkIdentityResult.Linked;
        }
    }

    private readonly record struct ExternalIdentityKey(string Issuer, string Subject)
    {
        internal static ExternalIdentityKey From(ExternalIdentity identity) =>
            new(identity.Issuer.AbsoluteUri, identity.Subject);
    }
}

internal enum LinkIdentityResult
{
    Linked,
    AlreadyLinkedToCurrentUser,
    LinkedToAnotherUser
}

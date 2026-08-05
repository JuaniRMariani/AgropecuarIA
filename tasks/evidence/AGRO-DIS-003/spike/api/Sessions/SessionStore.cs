namespace AgropecuarIA.IdentitySpike.Api.Sessions;

internal sealed class SessionStore(TimeProvider timeProvider)
{
    internal const string CookieName = "__Host-agro-dis003-session";
    private readonly object _sync = new();
    private readonly Dictionary<Guid, SessionRecord> _sessions = [];

    internal SessionRecord Create(
        Guid userId,
        Guid? selectedOrganizationId,
        bool stepUpExpired = false)
    {
        var now = timeProvider.GetUtcNow();
        var session = new SessionRecord(
            Guid.NewGuid(),
            userId,
            "aal2",
            ["oidc", "totp"],
            now,
            now.AddHours(8),
            stepUpExpired ? now.AddMinutes(-1) : now.AddMinutes(10),
            selectedOrganizationId,
            null,
            null);

        lock (_sync)
        {
            _sessions[session.SessionId] = session;
        }

        return session;
    }

    internal SessionRecord? Find(Guid sessionId)
    {
        lock (_sync)
        {
            return _sessions.GetValueOrDefault(sessionId);
        }
    }

    internal SessionRecord? SwitchOrganization(Guid sessionId, Guid organizationId)
    {
        lock (_sync)
        {
            if (!_sessions.TryGetValue(sessionId, out var current) || current.RevokedAt is not null)
            {
                return null;
            }

            var now = timeProvider.GetUtcNow();
            _sessions[sessionId] = current with
            {
                RevokedAt = now,
                RevocationReason = "security_version_changed"
            };

            var replacement = current with
            {
                SessionId = Guid.NewGuid(),
                SelectedOrganizationId = organizationId,
                RevokedAt = null,
                RevocationReason = null
            };
            _sessions[replacement.SessionId] = replacement;
            return replacement;
        }
    }

    internal SessionRecord? Revoke(Guid sessionId, string reason)
    {
        lock (_sync)
        {
            if (!_sessions.TryGetValue(sessionId, out var current))
            {
                return null;
            }

            if (current.RevokedAt is not null)
            {
                return current;
            }

            var revoked = current with
            {
                RevokedAt = timeProvider.GetUtcNow(),
                RevocationReason = reason
            };
            _sessions[sessionId] = revoked;
            return revoked;
        }
    }

    internal int RevokeAll(Guid userId, string reason)
    {
        lock (_sync)
        {
            var now = timeProvider.GetUtcNow();
            var sessionIds = _sessions.Values
                .Where(session => session.UserId == userId && session.RevokedAt is null)
                .Select(session => session.SessionId)
                .ToArray();

            foreach (var sessionId in sessionIds)
            {
                var current = _sessions[sessionId];
                _sessions[sessionId] = current with { RevokedAt = now, RevocationReason = reason };
            }

            return sessionIds.Length;
        }
    }
}

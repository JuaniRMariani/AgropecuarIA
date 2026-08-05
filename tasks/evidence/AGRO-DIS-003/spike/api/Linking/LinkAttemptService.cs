using AgropecuarIA.IdentitySpike.Api.Fixtures;

namespace AgropecuarIA.IdentitySpike.Api.Linking;

internal sealed class LinkAttemptService(
    FixtureIdentityDirectory directory,
    ReauthenticationProofStore proofStore,
    TimeProvider timeProvider)
{
    private const int MaximumAttempts = 256;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, LinkAttempt> _attempts = [];

    internal LinkAttempt Create(Guid sessionId, Guid userId, ExternalIdentity candidateIdentity)
    {
        var now = timeProvider.GetUtcNow();
        var attempt = new LinkAttempt(
            Guid.NewGuid(),
            sessionId,
            userId,
            "requested",
            directory.GetPrimaryIdentity(userId),
            candidateIdentity,
            now.AddMinutes(5));

        lock (_sync)
        {
            foreach (var staleAttemptId in _attempts
                .Where(pair => pair.Value.ExpiresAt <= now || pair.Value.State is "linked" or "rejected")
                .Select(pair => pair.Key)
                .ToArray())
            {
                _attempts.Remove(staleAttemptId);
            }

            if (_attempts.Count == MaximumAttempts)
            {
                var oldestAttemptId = _attempts.MinBy(pair => pair.Value.ExpiresAt).Key;
                _attempts.Remove(oldestAttemptId);
            }

            _attempts[attempt.AttemptId] = attempt;
        }

        return attempt;
    }

    internal LinkOperation ReauthenticateCurrent(Guid attemptId, Guid sessionId, Guid proofId) =>
        TransitionWithProof(
            attemptId,
            sessionId,
            proofId,
            "requested",
            "current_reauthenticated",
            attempt => attempt.CurrentIdentity);

    internal LinkOperation ReauthenticateCandidate(Guid attemptId, Guid sessionId, Guid proofId) =>
        TransitionWithProof(
            attemptId,
            sessionId,
            proofId,
            "current_reauthenticated",
            "candidate_reauthenticated",
            attempt => attempt.CandidateIdentity);

    internal LinkOperation Complete(Guid attemptId, Guid sessionId)
    {
        lock (_sync)
        {
            var lookup = GetUsableAttempt(attemptId, sessionId);
            if (lookup.Result != LinkOperationResult.Succeeded)
            {
                return lookup;
            }

            var attempt = lookup.Attempt!;
            if (attempt.State != "candidate_reauthenticated")
            {
                return new(LinkOperationResult.InvalidState, attempt);
            }

            var linkResult = directory.TryLink(attempt.UserId, attempt.CandidateIdentity);
            if (linkResult != LinkIdentityResult.Linked)
            {
                var rejected = attempt with { State = "rejected" };
                _attempts[attemptId] = rejected;
                return new(LinkOperationResult.Conflict, rejected);
            }

            var linked = attempt with { State = "linked" };
            _attempts[attemptId] = linked;
            return new(LinkOperationResult.Succeeded, linked);
        }
    }

    private LinkOperation TransitionWithProof(
        Guid attemptId,
        Guid sessionId,
        Guid proofId,
        string expectedState,
        string nextState,
        Func<LinkAttempt, ExternalIdentity> expectedIdentity)
    {
        lock (_sync)
        {
            var lookup = GetUsableAttempt(attemptId, sessionId);
            if (lookup.Result != LinkOperationResult.Succeeded)
            {
                return lookup;
            }

            var attempt = lookup.Attempt!;
            if (attempt.State != expectedState)
            {
                return new(LinkOperationResult.InvalidState, attempt);
            }

            var proofResult = proofStore.Consume(
                proofId,
                sessionId,
                expectedIdentity(attempt));
            if (proofResult != LinkOperationResult.Succeeded)
            {
                return new(proofResult, attempt);
            }

            var transitioned = attempt with { State = nextState };
            _attempts[attemptId] = transitioned;
            return new(LinkOperationResult.Succeeded, transitioned);
        }
    }

    private LinkOperation GetUsableAttempt(Guid attemptId, Guid sessionId)
    {
        if (!_attempts.TryGetValue(attemptId, out var attempt) || attempt.SessionId != sessionId)
        {
            return new(LinkOperationResult.NotFound, null);
        }

        if (attempt.ExpiresAt <= timeProvider.GetUtcNow())
        {
            var expired = attempt with { State = "expired" };
            _attempts[attemptId] = expired;
            return new(LinkOperationResult.Expired, expired);
        }

        return new(LinkOperationResult.Succeeded, attempt);
    }
}

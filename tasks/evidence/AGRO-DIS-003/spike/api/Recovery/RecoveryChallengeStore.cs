namespace AgropecuarIA.IdentitySpike.Api.Recovery;

internal sealed class RecoveryChallengeStore(TimeProvider timeProvider)
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(2);
    private const int MaximumChallenges = 256;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, RecoveryChallenge> _challenges = [];

    internal RecoveryChallenge Issue(Guid userId, TimeSpan? lifetime = null)
    {
        var now = timeProvider.GetUtcNow();
        var challenge = new RecoveryChallenge(
            Guid.NewGuid(),
            userId,
            now.Add(lifetime ?? DefaultLifetime),
            false);

        lock (_sync)
        {
            foreach (var staleChallengeId in _challenges
                .Where(pair => pair.Value.Consumed || pair.Value.ExpiresAt <= now)
                .Select(pair => pair.Key)
                .ToArray())
            {
                _challenges.Remove(staleChallengeId);
            }

            if (_challenges.Count == MaximumChallenges)
            {
                var oldestChallengeId = _challenges.MinBy(pair => pair.Value.ExpiresAt).Key;
                _challenges.Remove(oldestChallengeId);
            }

            _challenges[challenge.ChallengeId] = challenge;
        }

        return challenge;
    }

    internal RecoveryChallengeResult Consume(Guid challengeId, Guid userId)
    {
        lock (_sync)
        {
            if (!_challenges.TryGetValue(challengeId, out var challenge) || challenge.UserId != userId)
            {
                return RecoveryChallengeResult.Invalid;
            }

            if (challenge.Consumed)
            {
                return RecoveryChallengeResult.Replayed;
            }

            if (challenge.ExpiresAt <= timeProvider.GetUtcNow())
            {
                return RecoveryChallengeResult.Expired;
            }

            _challenges[challengeId] = challenge with { Consumed = true };
            return RecoveryChallengeResult.Succeeded;
        }
    }
}

internal sealed record RecoveryChallenge(
    Guid ChallengeId,
    Guid UserId,
    DateTimeOffset ExpiresAt,
    bool Consumed);

internal enum RecoveryChallengeResult
{
    Succeeded,
    Invalid,
    Expired,
    Replayed
}

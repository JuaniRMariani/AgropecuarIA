namespace AgropecuarIA.IdentitySpike.Api.Linking;

internal sealed class ReauthenticationProofStore(TimeProvider timeProvider)
{
    private const int MaximumProofs = 256;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ReauthenticationProof> _proofs = [];

    internal ReauthenticationProof Issue(Guid sessionId, ExternalIdentity identity)
    {
        var now = timeProvider.GetUtcNow();
        var proof = new ReauthenticationProof(
            Guid.NewGuid(),
            sessionId,
            identity,
            now.AddMinutes(2),
            false);

        lock (_sync)
        {
            foreach (var staleProofId in _proofs
                .Where(pair => pair.Value.Consumed || pair.Value.ExpiresAt <= now)
                .Select(pair => pair.Key)
                .ToArray())
            {
                _proofs.Remove(staleProofId);
            }

            if (_proofs.Count == MaximumProofs)
            {
                var oldestProofId = _proofs.MinBy(pair => pair.Value.ExpiresAt).Key;
                _proofs.Remove(oldestProofId);
            }

            _proofs[proof.ProofId] = proof;
        }

        return proof;
    }

    internal LinkOperationResult Consume(
        Guid proofId,
        Guid sessionId,
        ExternalIdentity expectedIdentity)
    {
        lock (_sync)
        {
            if (!_proofs.TryGetValue(proofId, out var proof) ||
                proof.SessionId != sessionId ||
                proof.Identity != expectedIdentity)
            {
                return LinkOperationResult.InvalidProof;
            }

            if (proof.Consumed)
            {
                return LinkOperationResult.ProofReplayed;
            }

            if (proof.ExpiresAt <= timeProvider.GetUtcNow())
            {
                return LinkOperationResult.InvalidProof;
            }

            _proofs[proofId] = proof with { Consumed = true };
            return LinkOperationResult.Succeeded;
        }
    }
}

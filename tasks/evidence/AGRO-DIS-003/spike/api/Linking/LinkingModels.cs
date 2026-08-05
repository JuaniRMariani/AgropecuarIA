namespace AgropecuarIA.IdentitySpike.Api.Linking;

internal sealed record ExternalIdentity(Uri Issuer, string Subject);

internal sealed record ExternalIdentityResponse(string Issuer, string Subject);

internal sealed record LinkAttemptResponse(
    Guid AttemptId,
    string State,
    ExternalIdentityResponse CurrentIdentity,
    ExternalIdentityResponse CandidateIdentity,
    DateTimeOffset ExpiresAt);

internal sealed record LinkAttempt(
    Guid AttemptId,
    Guid SessionId,
    Guid UserId,
    string State,
    ExternalIdentity CurrentIdentity,
    ExternalIdentity CandidateIdentity,
    DateTimeOffset ExpiresAt);

internal sealed record ReauthenticationProof(
    Guid ProofId,
    Guid SessionId,
    ExternalIdentity Identity,
    DateTimeOffset ExpiresAt,
    bool Consumed);

internal enum LinkOperationResult
{
    Succeeded,
    NotFound,
    Expired,
    InvalidState,
    InvalidProof,
    ProofReplayed,
    Conflict
}

internal sealed record LinkOperation(LinkOperationResult Result, LinkAttempt? Attempt);

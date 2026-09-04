namespace AgropecuarIA.Catalog.Application;

public sealed record CatalogEditorialContext(Guid ActorUserId, Guid SessionId, string CorrelationId)
{
    public void Validate()
    {
        if (ActorUserId == Guid.Empty || SessionId == Guid.Empty || string.IsNullOrWhiteSpace(CorrelationId) || CorrelationId.Length > 128)
            throw CatalogErrors.InvalidRequest();
    }
}

public sealed class CatalogOperationException(string code, int statusCode, string message) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public static class CatalogErrors
{
    public static CatalogOperationException InvalidRequest() => new("catalog.invalid_request", 400, "The catalog request is invalid.");
    public static CatalogOperationException InvalidSource() => new("catalog.invalid_source", 400, "The entire source must match the bounded catalog JSON schema; no rows were ingested.");
    public static CatalogOperationException TooLarge() => new("catalog.source_too_large", 413, "The source or candidate exceeds the documented operational limits.");
    public static CatalogOperationException LegacySnapshot() => new("catalog.legacy_snapshot_unverified", 409, "This legacy snapshot lacks complete-ingestion evidence. A separately reviewed replay/backfill is required.");
    public static CatalogOperationException Conflict() => new("catalog.candidate_conflict", 409, "The candidate contains ambiguous normalized codes or aliases. Review the editorial diff.");
    public static CatalogOperationException EmptyCandidate() => new("catalog.empty_candidate", 409, "An empty catalog cannot be published.");
    public static CatalogOperationException Stale() => new("catalog.candidate_stale", 409, "The candidate or active version changed. Review a fresh diff before publishing.");
    public static CatalogOperationException VersionExists() => new("catalog.version_exists", 409, "The catalog version tag already exists.");
    public static CatalogOperationException VersionNotFound() => new("catalog.version_not_found", 404, "The requested catalog version was not found.");
    public static CatalogOperationException ItemNotFound() => new("catalog.item_not_found", 404, "The requested catalog item was not found.");
    public static CatalogOperationException Unavailable() => new("catalog.unavailable", 503, "Catalog is unavailable or the write outcome is unknown. Refresh the active version and editorial diff before a new attempt; do not retry automatically.");
}

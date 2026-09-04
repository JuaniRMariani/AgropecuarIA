using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Application;

public enum ProductionCatalogResolutionStatus
{
    Resolved,
    NotPublished,
    VersionStale,
    ItemNotFound,
    Unavailable,
}

public sealed record ProductionCatalogResolution(
    ProductionCatalogResolutionStatus Status,
    ProductionCatalogSnapshot? Snapshot = null);

public interface IProductionCatalogResolver
{
    Task<ProductionCatalogResolution> ResolveActiveAsync(
        string catalogCode, Guid? expectedCatalogVersionId, CancellationToken cancellationToken);
}

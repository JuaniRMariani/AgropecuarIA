using AgropecuarIA.Catalog.Application;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.Api;

/// <summary>Composition-only translation between the modules' public application contracts.</summary>
internal sealed class ProductionCatalogResolver(CatalogSearchApplicationService catalog) : IProductionCatalogResolver
{
    public async Task<ProductionCatalogResolution> ResolveActiveAsync(
        string catalogCode, Guid? expectedCatalogVersionId, CancellationToken cancellationToken)
    {
        try
        {
            CatalogActiveItemResolution result = await catalog.ResolveActiveItemAsync(catalogCode, expectedCatalogVersionId, cancellationToken);
            if (result.Status == CatalogActiveItemResolutionStatus.Resolved && result.Item is { } item)
            {
                var snapshot = new ProductionCatalogSnapshot(item.VersionId, item.Id, item.VersionTag, item.Code,
                    item.DisplayName, item.SupportLevel, item.SourceSnapshotId, item.SourceId, item.SourceHash,
                    item.SourceIngestedAtUtc, item.ProvenanceStatus, result.ResolvedAtUtc);
                snapshot.Validate();
                return new(ProductionCatalogResolutionStatus.Resolved, snapshot);
            }

            return new(result.Status switch
            {
                CatalogActiveItemResolutionStatus.NotPublished => ProductionCatalogResolutionStatus.NotPublished,
                CatalogActiveItemResolutionStatus.VersionStale => ProductionCatalogResolutionStatus.VersionStale,
                CatalogActiveItemResolutionStatus.ItemNotFound => ProductionCatalogResolutionStatus.ItemNotFound,
                _ => ProductionCatalogResolutionStatus.Unavailable,
            });
        }
        catch (Exception exception) when (exception is CatalogOperationException or ArgumentException)
        {
            // Never propagate Catalog's private exception details or substitute client metadata.
            return new(ProductionCatalogResolutionStatus.Unavailable);
        }
    }
}

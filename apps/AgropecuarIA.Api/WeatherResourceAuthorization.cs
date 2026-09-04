using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.Weather.Application;

namespace AgropecuarIA.Api;

internal sealed class WeatherResourceAuthorization(IProductiveCoreUnitOfWorkFactory factory)
    : IWeatherResourceAuthorization
{
    public async ValueTask<IAsyncDisposable> OpenAuthorizedScopeAsync(
        Guid organizationId,
        Guid? fieldId,
        Guid actorUserId,
        Guid sessionId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || fieldId == Guid.Empty)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }

        IProductiveCoreUnitOfWork scope = await factory.BeginAsync(
            ProductiveTransactionMode.Read, cancellationToken);
        try
        {
            Guid? version = await scope.AuthorizeOwnerAsync(
                new ProductiveRequestContext(correlationId, actorUserId, sessionId, organizationId),
                cancellationToken);
            if (version is null || (fieldId.HasValue &&
                await scope.GetManagementUnitAsync(organizationId, fieldId.Value, cancellationToken) is null))
            {
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            // Keep the authorization transaction alive until the operation finishes.
            return scope;
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }
}

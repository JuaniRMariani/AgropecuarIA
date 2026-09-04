namespace AgropecuarIA.Weather.Application;

/// <summary>Revalidates the session and owner scope before any weather data access.</summary>
public interface IWeatherResourceAuthorization
{
    ValueTask<IAsyncDisposable> OpenAuthorizedScopeAsync(
        Guid organizationId,
        Guid? fieldId,
        Guid actorUserId,
        Guid sessionId,
        string correlationId,
        CancellationToken cancellationToken);
}

using AgropecuarIA.Identity.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AgropecuarIA.Weather.Infrastructure;

public static class WeatherRequestScope
{
    public static async Task<IDbContextTransaction> BeginTenantAsync(
        WeatherDbContext database, Guid organizationId, AuthenticatedSession session, CancellationToken cancellationToken)
    {
        IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await database.Database.ExecuteSqlRawAsync("SET LOCAL ROLE agro_weather_app", cancellationToken);
            await database.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.current_scope_kind', {"tenant"}, true), set_config('app.current_organization_id', {organizationId.ToString("D")}, true), set_config('app.current_actor_id', {session.UserId.ToString("D")}, true), set_config('app.current_session_id', {session.SessionId.ToString("D")}, true)", cancellationToken);
            await database.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.current_authorization_version', coalesce(identity.authorize_productive_owner()::text, ''), true)", cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public static async Task<IDbContextTransaction> BeginEditorialAsync(
        WeatherDbContext database, CancellationToken cancellationToken)
    {
        IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await database.Database.ExecuteSqlRawAsync("SET LOCAL ROLE agro_weather_editor", cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}

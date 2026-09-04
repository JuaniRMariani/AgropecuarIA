using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Weather.Application;
using AgropecuarIA.Weather.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace AgropecuarIA.Weather.Delivery;

internal sealed class WeatherResourceFilter(
    IWeatherResourceAuthorization authorization,
    WeatherDbContext database,
    IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext invocation,
        EndpointFilterDelegate next)
    {
        HttpContext context = invocation.HttpContext;
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await antiforgery.ValidateRequestAsync(context);
        }

        AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
        if (!Guid.TryParse(context.Request.RouteValues["organizationId"]?.ToString(), out Guid organizationId))
        {
            return Results.NotFound();
        }

        Guid? fieldId = null;
        if (context.Request.RouteValues.TryGetValue("fieldId", out object? routeField))
        {
            if (!Guid.TryParse(routeField?.ToString(), out Guid parsedField))
            {
                return Results.NotFound();
            }
            fieldId = parsedField;
        }
        else if (invocation.Arguments.OfType<CreateActivityRuleRequest>().FirstOrDefault() is { } request)
        {
            fieldId = request.FieldId;
        }
        else if (context.Request.Query.TryGetValue("fieldId", out var queryField))
        {
            if (queryField.Count != 1 || !Guid.TryParse(queryField[0], out Guid parsedField))
            {
                return Results.BadRequest();
            }
            fieldId = parsedField;
        }

        await using IAsyncDisposable scope = await authorization.OpenAuthorizedScopeAsync(
            organizationId, fieldId, session.UserId, session.SessionId,
            context.TraceIdentifier, context.RequestAborted);
        await using IDbContextTransaction transaction = await WeatherRequestScope.BeginTenantAsync(
            database, organizationId, session, context.RequestAborted);
        try
        {
            object? result = await next(invocation);
            await transaction.CommitAsync(context.RequestAborted);
            return result;
        }
        catch (WeatherRainReferenceException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "The observation to rectify is not available in this field.",
                extensions: new Dictionary<string, object?> { ["code"] = "weather.rain.reference_unavailable" });
        }
    }
}

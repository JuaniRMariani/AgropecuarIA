using Microsoft.AspNetCore.Antiforgery;

namespace AgropecuarIA.IdentitySpike.Api.Common;

internal sealed class AntiforgeryEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return ProblemResults.Create(
                context.HttpContext,
                StatusCodes.Status400BadRequest,
                "invalid-antiforgery-token",
                "Solicitud no válida",
                "La operación requiere una prueba antiforgery vigente.");
        }

        return await next(context);
    }
}

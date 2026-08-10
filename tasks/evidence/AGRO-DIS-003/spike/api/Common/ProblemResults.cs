using Microsoft.AspNetCore.Http.HttpResults;

namespace AgropecuarIA.IdentitySpike.Api.Common;

internal static class ProblemResults
{
    private const string BaseType = "https://agropecuaria.example/problems/identity-spike/";

    internal static ProblemHttpResult Create(
        HttpContext context,
        int statusCode,
        string code,
        string title,
        string detail)
    {
        return TypedResults.Problem(
            detail: detail,
            statusCode: statusCode,
            title: title,
            type: BaseType + code,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["correlationId"] = CorrelationIdAccessor.Get(context)
            });
    }

    internal static ProblemHttpResult NotAuthenticated(HttpContext context) => Create(
        context,
        StatusCodes.Status401Unauthorized,
        "not-authenticated",
        "Autenticación requerida",
        "La sesión no es válida o ya no está disponible.");

    internal static ProblemHttpResult OrganizationSelectionRequired(HttpContext context) => Create(
        context,
        StatusCodes.Status409Conflict,
        "organization-selection-required",
        "Selección de organización requerida",
        "La sesión tiene más de una membresía activa y necesita seleccionar una organización.");

    internal static ProblemHttpResult NoActiveMembership(HttpContext context) => Create(
        context,
        StatusCodes.Status403Forbidden,
        "active-membership-required",
        "Membresía activa requerida",
        "La identidad autenticada no tiene una membresía activa.");

    internal static ProblemHttpResult MembershipLimitExceeded(HttpContext context) => Create(
        context,
        StatusCodes.Status409Conflict,
        "membership-discovery-limit-exceeded",
        "Contexto de organizaciones no resoluble",
        "La cuenta supera el límite seguro de membresías activas para este flujo.");

    internal static ProblemHttpResult NeutralNotFound(HttpContext context) => Create(
        context,
        StatusCodes.Status404NotFound,
        "resource-not-found",
        "Recurso no encontrado",
        "El recurso solicitado no existe o no está disponible para el contexto efectivo.");

    internal static ProblemHttpResult StepUpRequired(HttpContext context) => Create(
        context,
        StatusCodes.Status403Forbidden,
        "step-up-required",
        "Reautenticación reciente requerida",
        "La operación sensible requiere una autenticación reciente y verificable.");
}

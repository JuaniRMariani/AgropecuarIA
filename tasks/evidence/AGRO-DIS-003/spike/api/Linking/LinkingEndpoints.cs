using AgropecuarIA.IdentitySpike.Api.Auditing;
using AgropecuarIA.IdentitySpike.Api.Common;
using AgropecuarIA.IdentitySpike.Api.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgropecuarIA.IdentitySpike.Api.Linking;

internal static class LinkingEndpoints
{
    internal static void Map(RouteGroupBuilder api)
    {
        api.MapPost("/link-attempts", CreateAttempt)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();
        api.MapPost("/link-attempts/{attemptId:guid}/reauthenticate-current", ReauthenticateCurrent)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();
        api.MapPost("/link-attempts/{attemptId:guid}/reauthenticate-candidate", ReauthenticateCandidate)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();
        api.MapPost("/link-attempts/{attemptId:guid}/complete", Complete)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();
    }

    private static IResult CreateAttempt(
        CreateLinkAttemptRequest request,
        HttpContext context,
        SessionContextService contextService,
        LinkAttemptService service)
    {
        var resolution = SessionEndpointSupport.RequireStepUp(context, contextService, out var failure);
        if (failure is not null)
        {
            return failure;
        }

        if (!TryCreateIdentity(request.Issuer, request.Subject, out var candidate))
        {
            return InvalidIdentity(context);
        }

        var attempt = service.Create(
            resolution.Session!.SessionId,
            resolution.Session.UserId,
            candidate!);
        return TypedResults.Created(
            $"/api/spike/link-attempts/{attempt.AttemptId:D}",
            ToResponse(attempt));
    }

    private static IResult ReauthenticateCurrent(
        Guid attemptId,
        ReauthenticateRequest request,
        HttpContext context,
        SessionContextService contextService,
        LinkAttemptService service)
    {
        var resolution = SessionEndpointSupport.RequireStepUp(context, contextService, out var failure);
        return failure ?? ToResult(
            context,
            service.ReauthenticateCurrent(attemptId, resolution.Session!.SessionId, request.ProofId));
    }

    private static IResult ReauthenticateCandidate(
        Guid attemptId,
        ReauthenticateRequest request,
        HttpContext context,
        SessionContextService contextService,
        LinkAttemptService service)
    {
        var resolution = SessionEndpointSupport.RequireStepUp(context, contextService, out var failure);
        return failure ?? ToResult(
            context,
            service.ReauthenticateCandidate(attemptId, resolution.Session!.SessionId, request.ProofId));
    }

    private static async Task<IResult> Complete(
        Guid attemptId,
        HttpContext context,
        SessionContextService contextService,
        LinkAttemptService service,
        AuditEventRepository auditRepository,
        CancellationToken cancellationToken)
    {
        var resolution = SessionEndpointSupport.RequireStepUp(context, contextService, out var failure);
        if (failure is not null)
        {
            return failure;
        }

        var operation = service.Complete(attemptId, resolution.Session!.SessionId);
        if (operation.Result != LinkOperationResult.Succeeded)
        {
            return ToResult(context, operation);
        }

        await auditRepository.RecordAsync(
            IdentityAuditEvent.Succeeded(
                "IdentityLinked",
                resolution.Session.UserId,
                resolution.Session.SelectedOrganizationId,
                CorrelationIdAccessor.Get(context)),
            cancellationToken);
        return TypedResults.Ok(ToResponse(operation.Attempt!));
    }

    internal static bool TryCreateIdentity(
        string? issuer,
        string? subject,
        out ExternalIdentity? identity)
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(subject) ||
            issuer.Length > 500 ||
            subject.Length > 255 ||
            !Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
            issuerUri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        identity = new ExternalIdentity(issuerUri, subject);
        return true;
    }

    internal static LinkAttemptResponse ToResponse(LinkAttempt attempt) => new(
        attempt.AttemptId,
        attempt.State,
        new(attempt.CurrentIdentity.Issuer.AbsoluteUri, attempt.CurrentIdentity.Subject),
        new(attempt.CandidateIdentity.Issuer.AbsoluteUri, attempt.CandidateIdentity.Subject),
        attempt.ExpiresAt);

    private static IResult ToResult(HttpContext context, LinkOperation operation) => operation.Result switch
    {
        LinkOperationResult.Succeeded => TypedResults.Ok(ToResponse(operation.Attempt!)),
        LinkOperationResult.NotFound => ProblemResults.NeutralNotFound(context),
        LinkOperationResult.Expired => ProblemResults.Create(
            context,
            StatusCodes.Status410Gone,
            "link-attempt-expired",
            "Intento vencido",
            "El intento de vinculación venció y debe comenzar nuevamente."),
        LinkOperationResult.InvalidState => ProblemResults.Create(
            context,
            StatusCodes.Status409Conflict,
            "link-state-conflict",
            "Estado de vinculación inválido",
            "La transición solicitada no es válida para el estado actual."),
        LinkOperationResult.InvalidProof => ProblemResults.Create(
            context,
            StatusCodes.Status403Forbidden,
            "reauthentication-required",
            "Reautenticación requerida",
            "La prueba de reautenticación no es válida para esta identidad y sesión."),
        LinkOperationResult.ProofReplayed => ProblemResults.Create(
            context,
            StatusCodes.Status409Conflict,
            "reauthentication-replayed",
            "Prueba ya utilizada",
            "La prueba de reautenticación es de un solo uso."),
        LinkOperationResult.Conflict => ProblemResults.Create(
            context,
            StatusCodes.Status409Conflict,
            "identity-link-conflict",
            "Identidad no vinculable",
            "La identidad externa ya está vinculada y no puede asociarse a esta cuenta."),
        _ => throw new InvalidOperationException("Unknown link operation result.")
    };

    private static ProblemHttpResult InvalidIdentity(HttpContext context) => ProblemResults.Create(
        context,
        StatusCodes.Status400BadRequest,
        "invalid-external-identity",
        "Identidad externa inválida",
        "Issuer y subject deben formar una identidad OIDC válida; email no es autoridad de vinculación.");

    private sealed record CreateLinkAttemptRequest(string? Issuer, string? Subject);
    private sealed record ReauthenticateRequest(Guid ProofId);
}

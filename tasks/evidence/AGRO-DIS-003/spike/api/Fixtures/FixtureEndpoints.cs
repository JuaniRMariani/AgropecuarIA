using AgropecuarIA.IdentitySpike.Api.Auditing;
using AgropecuarIA.IdentitySpike.Api.Common;
using AgropecuarIA.IdentitySpike.Api.Linking;
using AgropecuarIA.IdentitySpike.Api.Recovery;
using AgropecuarIA.IdentitySpike.Api.Sessions;

namespace AgropecuarIA.IdentitySpike.Api.Fixtures;

internal static class FixtureEndpoints
{
    private static readonly string[] Scenarios = ["zero", "one", "many", "no-read"];

    internal static void Map(RouteGroupBuilder fixtures)
    {
        fixtures.MapPost("/sessions", CreateSession);
        fixtures.MapPost("/reauthentication-proofs", IssueReauthenticationProof)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();
        fixtures.MapPost("/recovery/complete", CompleteRecovery);
        fixtures.MapPost("/recovery/challenges", IssueRecoveryChallenge);
        fixtures.MapGet(
            "/audit-events/global",
            (AuditEventRepository repository) => TypedResults.Ok(repository.GetGlobalFixtureEvents()));
        fixtures.MapGet("/scenarios", () => TypedResults.Ok(Scenarios));
    }

    private static IResult CreateSession(
        CreateFixtureSessionRequest request,
        HttpContext context,
        SessionStore sessionStore)
    {
        var userId = request.Scenario switch
        {
            "zero" => FixtureIdentityDirectory.NoOrganizationUserId,
            "one" => FixtureIdentityDirectory.OneOrganizationUserId,
            "many" => FixtureIdentityDirectory.ManyOrganizationsUserId,
            "no-read" => FixtureIdentityDirectory.NoReadPermissionUserId,
            _ => Guid.Empty
        };

        if (userId == Guid.Empty)
        {
            return ProblemResults.Create(
                context,
                StatusCodes.Status400BadRequest,
                "invalid-fixture-scenario",
                "Escenario fixture inválido",
                "Los únicos escenarios disponibles son zero, one, many y no-read.");
        }

        Guid? selectedOrganizationId = request.Scenario switch
        {
            "one" => FixtureIdentityDirectory.OrganizationAId,
            "no-read" => FixtureIdentityDirectory.OrganizationBId,
            _ => request.SelectedOrganizationId
        };
        var session = sessionStore.Create(userId, selectedOrganizationId, request.StepUpExpired);
        SessionEndpointSupport.SetSessionCookie(context, session);

        var initialResolution = request.Scenario switch
        {
            "zero" => "no_active_membership",
            "one" => "active",
            "no-read" => "active",
            "many" when selectedOrganizationId is null => "selection_required",
            _ => "active"
        };
        return TypedResults.Created(
            "/api/spike/session",
            new FixtureSessionCreatedResponse(
                session.SessionId,
                userId,
                selectedOrganizationId,
                initialResolution));
    }

    private static async Task<IResult> IssueReauthenticationProof(
        IssueProofRequest request,
        HttpContext context,
        SessionContextService contextService,
        ReauthenticationProofStore proofStore,
        CancellationToken cancellationToken)
    {
        SessionRequirementResult requirement = await SessionEndpointSupport.RequireAuthenticatedAsync(
            context,
            contextService,
            cancellationToken);
        if (requirement.Failure is not null)
        {
            return requirement.Failure;
        }

        if (!LinkingEndpoints.TryCreateIdentity(request.Issuer, request.Subject, out var identity))
        {
            return ProblemResults.Create(
                context,
                StatusCodes.Status400BadRequest,
                "invalid-external-identity",
                "Identidad fixture inválida",
                "Issuer debe ser HTTPS y subject no puede estar vacío.");
        }

        var proof = proofStore.Issue(requirement.Resolution.Session!.SessionId, identity!);
        return TypedResults.Created(
            $"/__fixtures/reauthentication-proofs/{proof.ProofId:D}",
            new ProofResponse(proof.ProofId, proof.ExpiresAt));
    }

    private static IResult IssueRecoveryChallenge(
        IssueRecoveryChallengeRequest request,
        HttpContext context,
        FixtureIdentityDirectory directory,
        RecoveryChallengeStore challengeStore)
    {
        if (!directory.UserExists(request.UserId) || request.ExpiresInSeconds is < 0 or > 120)
        {
            return ProblemResults.NeutralNotFound(context);
        }

        var challenge = challengeStore.Issue(
            request.UserId,
            TimeSpan.FromSeconds(request.ExpiresInSeconds));
        return TypedResults.Created(
            $"/__fixtures/recovery/challenges/{challenge.ChallengeId:D}",
            new RecoveryChallengeResponse(challenge.ChallengeId, challenge.ExpiresAt));
    }

    private static async Task<IResult> CompleteRecovery(
        CompleteRecoveryRequest request,
        HttpContext context,
        FixtureIdentityDirectory directory,
        SessionStore sessionStore,
        AuditEventRepository auditRepository,
        RecoveryChallengeStore challengeStore,
        CancellationToken cancellationToken)
    {
        if (!directory.UserExists(request.UserId))
        {
            return ProblemResults.NeutralNotFound(context);
        }

        var challengeResult = challengeStore.Consume(request.ChallengeId, request.UserId);
        if (challengeResult != RecoveryChallengeResult.Succeeded)
        {
            return challengeResult switch
            {
                RecoveryChallengeResult.Expired => ProblemResults.Create(
                    context,
                    StatusCodes.Status410Gone,
                    "recovery-challenge-expired",
                    "Prueba de recuperación vencida",
                    "La prueba venció y debe comenzar nuevamente."),
                RecoveryChallengeResult.Replayed => ProblemResults.Create(
                    context,
                    StatusCodes.Status409Conflict,
                    "recovery-challenge-replayed",
                    "Prueba de recuperación ya utilizada",
                    "La prueba es de un solo uso."),
                _ => ProblemResults.NeutralNotFound(context)
            };
        }

        var revokedCount = sessionStore.RevokeAll(request.UserId, "security_version_changed");
        await auditRepository.RecordAsync(
            IdentityAuditEvent.Succeeded(
                "RecoveryCompleted",
                request.UserId,
                null,
                CorrelationIdAccessor.Get(context)),
            cancellationToken);
        return TypedResults.Ok(new RecoveryCompletedResponse(revokedCount));
    }

    private sealed record CreateFixtureSessionRequest(
        string Scenario,
        Guid? SelectedOrganizationId,
        bool StepUpExpired = false);
    private sealed record FixtureSessionCreatedResponse(
        Guid SessionId,
        Guid UserId,
        Guid? SelectedOrganizationId,
        string InitialResolution);
    private sealed record IssueProofRequest(string? Issuer, string? Subject);
    private sealed record ProofResponse(Guid ProofId, DateTimeOffset ExpiresAt);
    private sealed record IssueRecoveryChallengeRequest(Guid UserId, int ExpiresInSeconds = 120);
    private sealed record RecoveryChallengeResponse(Guid ChallengeId, DateTimeOffset ExpiresAt);
    private sealed record CompleteRecoveryRequest(Guid UserId, Guid ChallengeId);
    private sealed record RecoveryCompletedResponse(int RevokedSessions);
}

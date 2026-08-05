using AgropecuarIA.IdentitySpike.Api.Auditing;
using AgropecuarIA.IdentitySpike.Api.Common;

namespace AgropecuarIA.IdentitySpike.Api.Recovery;

internal static class RecoveryEndpoints
{
    internal static void Map(RouteGroupBuilder api) =>
        api.MapPost("/recovery/start", StartRecovery);

    private static async Task<IResult> StartRecovery(
        RecoveryRequest request,
        HttpContext context,
        RecoveryRequestService service,
        AuditEventRepository auditRepository,
        CancellationToken cancellationToken)
    {
        var decision = service.Accept(request.Email);
        await auditRepository.RecordAsync(
            IdentityAuditEvent.Accepted(
                "RecoveryRequested",
                null,
                null,
                CorrelationIdAccessor.Get(context),
                decision.InternalReasonCode),
            cancellationToken);

        return TypedResults.Accepted(
            uri: (string?)null,
            value: new RecoveryAcceptedResponse(
                "Si la identidad existe, el proveedor continuará la recuperación por el canal configurado."));
    }

    private sealed record RecoveryRequest(string? Email);
    private sealed record RecoveryAcceptedResponse(string Message);
}

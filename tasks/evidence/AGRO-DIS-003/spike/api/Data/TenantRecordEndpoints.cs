using AgropecuarIA.IdentitySpike.Api.Auditing;
using AgropecuarIA.IdentitySpike.Api.Common;
using AgropecuarIA.IdentitySpike.Api.Sessions;

namespace AgropecuarIA.IdentitySpike.Api.Data;

internal static class TenantRecordEndpoints
{
    internal static void Map(RouteGroupBuilder api) =>
        api.MapGet("/tenant-records/{recordId:guid}", GetTenantRecord);

    private static async Task<IResult> GetTenantRecord(
        Guid recordId,
        HttpContext context,
        SessionContextService contextService,
        TenantRecordRepository repository,
        AuditEventRepository auditRepository,
        CancellationToken cancellationToken)
    {
        var resolution = SessionEndpointSupport.RequireActive(context, contextService, out var failure);
        if (failure is not null)
        {
            return failure;
        }

        if (!resolution.ActiveMembership!.Permissions.Contains(
            "tenant-record.read",
            StringComparer.Ordinal))
        {
            await auditRepository.RecordAsync(
                IdentityAuditEvent.Denied(
                    "AccessDenied",
                    resolution.Session!.UserId,
                    resolution.ActiveMembership.OrganizationId,
                    CorrelationIdAccessor.Get(context),
                    "permission_missing"),
                cancellationToken);
            return ProblemResults.NeutralNotFound(context);
        }

        var record = await repository.FindAsync(
            resolution.ActiveMembership!.OrganizationId,
            recordId,
            cancellationToken);
        if (record is not null)
        {
            return TypedResults.Ok(record);
        }

        await auditRepository.RecordAsync(
            IdentityAuditEvent.Denied(
                "AccessDenied",
                resolution.Session!.UserId,
                resolution.ActiveMembership.OrganizationId,
                CorrelationIdAccessor.Get(context),
                "record_not_visible"),
            cancellationToken);
        return ProblemResults.NeutralNotFound(context);
    }
}

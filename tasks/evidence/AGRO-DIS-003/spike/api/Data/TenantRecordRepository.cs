using Npgsql;

namespace AgropecuarIA.IdentitySpike.Api.Data;

internal sealed record TenantRecord(Guid Id, Guid OrganizationId, string Label);

internal sealed class TenantRecordRepository(NpgsqlDataSource dataSource)
{
    internal async Task<TenantRecord?> FindAsync(
        Guid organizationId,
        Guid actorUserId,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var contextCommand = connection.CreateCommand())
        {
            contextCommand.Transaction = transaction;
            contextCommand.CommandText =
                "select set_config('app.current_organization_id', @organization_id, true)";
            contextCommand.Parameters.AddWithValue("organization_id", organizationId.ToString("D"));
            await contextCommand.ExecuteScalarAsync(cancellationToken);
        }

        TenantRecord? record = null;
        await using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText =
                """
                select record.id, record.organization_id, record.record_name
                from identity_spike.tenant_record record
                where record.id = @record_id
                  and exists (
                      select 1
                      from identity_spike.membership membership
                      where membership.organization_id = record.organization_id
                        and membership.platform_user_id = @actor_user_id
                        and membership.is_active
                        and membership.permission_set = 'tenant-record.read'
                  )
                """;
            query.Parameters.AddWithValue("record_id", recordId);
            query.Parameters.AddWithValue("actor_user_id", actorUserId);

            // Authorization and data share this READ COMMITTED statement snapshot.
            // A revocation committed before it starts denies the read; a later racing
            // revocation becomes authoritative for the next request.
            await using var reader = await query.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                record = new TenantRecord(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return record;
    }
}

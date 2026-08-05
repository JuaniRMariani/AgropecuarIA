using Npgsql;

namespace AgropecuarIA.IdentitySpike.Api.Data;

internal sealed record TenantRecord(Guid Id, Guid OrganizationId, string Label);

internal sealed class TenantRecordRepository(NpgsqlDataSource dataSource)
{
    internal async Task<TenantRecord?> FindAsync(
        Guid organizationId,
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
                "select id, organization_id, record_name from identity_spike.tenant_record where id = @record_id";
            query.Parameters.AddWithValue("record_id", recordId);

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

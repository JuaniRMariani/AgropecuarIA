using Npgsql;
using NpgsqlTypes;

namespace AgropecuarIA.IdentitySpike.Api.Auditing;

internal sealed class AuditEventRepository(NpgsqlDataSource dataSource)
{
    private const int MaximumGlobalFixtureEvents = 256;
    private readonly object _globalSync = new();
    private readonly Queue<IdentityAuditEvent> _globalEvents = new();

    internal async Task RecordAsync(IdentityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        if (auditEvent.OrganizationId is not Guid organizationId)
        {
            // The R0 schema deliberately keeps tenant audit rows under FORCE RLS.
            // Provider-level recovery events have no tenant and remain ephemeral.
            lock (_globalSync)
            {
                if (_globalEvents.Count == MaximumGlobalFixtureEvents)
                {
                    _globalEvents.Dequeue();
                }

                _globalEvents.Enqueue(auditEvent);
            }

            return;
        }

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

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into identity_spike.audit_event
                (organization_id, id, event_type, actor_kind, actor_user_id, subject_ref, correlation_id, occurred_at)
            values
                (@organization_id, @id, @event_type, @actor_kind, @actor_user_id, @subject_ref, @correlation_id, @occurred_at)
            """;
        command.Parameters.AddWithValue("organization_id", organizationId);
        command.Parameters.AddWithValue("id", auditEvent.EventId);
        command.Parameters.AddWithValue("event_type", auditEvent.EventType);
        command.Parameters.AddWithValue("occurred_at", auditEvent.OccurredAt);
        command.Parameters.AddWithValue("actor_kind", auditEvent.ActorId is null ? "system" : "user");
        command.Parameters.Add("actor_user_id", NpgsqlDbType.Uuid).Value =
            auditEvent.ActorId is Guid actorId ? actorId : DBNull.Value;
        command.Parameters.AddWithValue("subject_ref", CreateSafeSubjectReference(auditEvent));
        command.Parameters.AddWithValue("correlation_id", auditEvent.CorrelationId);

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    internal IReadOnlyList<IdentityAuditEvent> GetGlobalFixtureEvents()
    {
        lock (_globalSync)
        {
            return _globalEvents.ToArray();
        }
    }

    private static string CreateSafeSubjectReference(IdentityAuditEvent auditEvent)
    {
        var reason = auditEvent.ReasonCode is null ? "none" : auditEvent.ReasonCode;
        return $"outcome:{auditEvent.Outcome};reason:{reason}";
    }
}

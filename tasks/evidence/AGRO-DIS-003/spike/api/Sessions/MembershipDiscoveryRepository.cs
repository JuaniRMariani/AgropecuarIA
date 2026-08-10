using Npgsql;

namespace AgropecuarIA.IdentitySpike.Api.Sessions;

internal interface IMembershipDiscoveryRepository
{
    Task ValidateConfigurationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganizationMembership>> ListActiveForActorAsync(
        Guid actorUserId,
        CancellationToken cancellationToken);
}

internal sealed class PostgresMembershipDiscoveryRepository :
    IMembershipDiscoveryRepository,
    IAsyncDisposable
{
    private const int MaximumMemberships = 100;
    private const string ExpectedPrincipal = "agro_membership_discovery";
    private const string ReadPermission = "tenant-record.read";
    private const string NoPermissions = "identity.none";
    private readonly NpgsqlDataSource _dataSource;

    internal PostgresMembershipDiscoveryRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _dataSource = NpgsqlDataSource.Create(connectionString);
    }

    public async Task ValidateConfigurationAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = _dataSource.CreateCommand(
            """
            select
                current_user::text,
                session_user::text,
                role.rolcanlogin,
                role.rolsuper,
                role.rolcreatedb,
                role.rolcreaterole,
                role.rolreplication,
                role.rolbypassrls,
                role.rolinherit,
                exists (
                    select 1
                    from pg_auth_members membership
                    where membership.member = role.oid
                ),
                exists (
                    select 1 from pg_database owned_database where owned_database.datdba = role.oid
                    union all
                    select 1 from pg_namespace namespace where namespace.nspowner = role.oid
                    union all
                    select 1 from pg_class relation where relation.relowner = role.oid
                    union all
                    select 1 from pg_proc procedure where procedure.proowner = role.oid
                    union all
                    select 1 from pg_type owned_type where owned_type.typowner = role.oid
                )
            from pg_roles role
            where role.rolname = current_user
            """);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) ||
            !string.Equals(reader.GetString(0), ExpectedPrincipal, StringComparison.Ordinal) ||
            !string.Equals(reader.GetString(1), ExpectedPrincipal, StringComparison.Ordinal) ||
            !reader.GetBoolean(2) ||
            reader.GetBoolean(3) ||
            reader.GetBoolean(4) ||
            reader.GetBoolean(5) ||
            reader.GetBoolean(6) ||
            reader.GetBoolean(7) ||
            reader.GetBoolean(8) ||
            reader.GetBoolean(9) ||
            reader.GetBoolean(10))
        {
            throw new InvalidOperationException(
                "Membership discovery database principal failed the required least-privilege checks.");
        }
    }

    public async Task<IReadOnlyList<OrganizationMembership>> ListActiveForActorAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user ID is required.", nameof(actorUserId));
        }

        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (NpgsqlCommand contextCommand = connection.CreateCommand())
        {
            contextCommand.Transaction = transaction;
            contextCommand.CommandText =
                "select set_config('app.current_actor_id', @actor_user_id, true)";
            contextCommand.Parameters.AddWithValue("actor_user_id", actorUserId.ToString("D"));
            await contextCommand.ExecuteScalarAsync(cancellationToken);
        }

        var memberships = new List<OrganizationMembership>(MaximumMemberships + 1);
        await using (NpgsqlCommand query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText =
                """
                select
                    membership.id,
                    membership.organization_id,
                    organization.display_name,
                    membership.permission_set,
                    membership.security_version
                from identity_spike.membership as membership
                join identity_spike.organization as organization
                  on organization.id = membership.organization_id
                where membership.is_active
                  and organization.is_active
                order by organization.display_name, membership.organization_id
                limit 101
                """;
            await using NpgsqlDataReader reader = await query.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                memberships.Add(new OrganizationMembership(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    "active",
                    ParsePermissions(reader.GetString(3)),
                    reader.GetInt32(4)));
            }
        }

        if (memberships.Count > MaximumMemberships)
        {
            throw new MembershipDiscoveryLimitExceededException(MaximumMemberships);
        }

        await transaction.CommitAsync(cancellationToken);
        return memberships;
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();

    private static IReadOnlyList<string> ParsePermissions(string permissionSet) => permissionSet switch
    {
        ReadPermission => [ReadPermission],
        NoPermissions => [],
        _ => throw new InvalidOperationException(
            $"Unknown synthetic permission set '{permissionSet}'. The spike fails closed on permission drift.")
    };
}

internal sealed class MembershipDiscoveryLimitExceededException(int maximumMemberships)
    : Exception($"Membership discovery exceeded the safe limit of {maximumMemberships} active memberships.");

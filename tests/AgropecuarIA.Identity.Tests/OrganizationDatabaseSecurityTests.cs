using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NpgsqlTypes;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrganizationDatabaseSecurityTests
{
    private const string PreviousMigration =
        "20260810195645_AddPurposeBoundStrongAuthentication";

    [TestMethod]
    public async Task MigrationExpandsWithoutBreakingTheNMinusOneWriter()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);

        try
        {
            await using var dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            Guid firstUserId = Guid.NewGuid();
            Guid secondUserId = Guid.NewGuid();
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var legacySeed = new NpgsqlCommand(
                    """
                    INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
                    VALUES
                        (@firstUserId, 'Legacy owner one', now(), 1),
                        (@secondUserId, 'Legacy owner two', now(), 1);

                    INSERT INTO identity.organization_memberships
                        ("UserId", "OrganizationId", "OrganizationName", "Role")
                    VALUES
                        (@firstUserId, @firstOrganizationId, 'Legacy organization one', 'owner');
                    """,
                    connection);
                legacySeed.Parameters.AddWithValue("firstUserId", firstUserId);
                legacySeed.Parameters.AddWithValue("secondUserId", secondUserId);
                legacySeed.Parameters.AddWithValue("firstOrganizationId", Guid.NewGuid());
                Assert.AreEqual(3, await legacySeed.ExecuteNonQueryAsync());
            }

            await migrator.MigrateAsync();

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var nMinusOneWriter = new NpgsqlCommand(
                    """
                    INSERT INTO identity.organization_memberships
                        ("UserId", "OrganizationId", "OrganizationName", "Role")
                    VALUES
                        (@userId, @organizationId, 'Legacy organization two', 'owner')
                    """,
                    connection);
                nMinusOneWriter.Parameters.AddWithValue("userId", secondUserId);
                nMinusOneWriter.Parameters.AddWithValue("organizationId", Guid.NewGuid());
                Assert.AreEqual(1, await nMinusOneWriter.ExecuteNonQueryAsync());

                Assert.IsTrue(await ScalarBooleanAsync(
                    connection,
                    """
                    SELECT
                        (SELECT count(*) FROM identity.organization_memberships) = 2
                        AND to_regclass('identity.organizations') IS NOT NULL
                        AND to_regclass('identity.memberships') IS NOT NULL
                        AND to_regclass('identity.organization_creation_ledgers') IS NOT NULL
                        AND to_regclass('identity.organization_creation_key_aliases') IS NOT NULL
                        AND (SELECT relrowsecurity AND relforcerowsecurity
                             FROM pg_class
                             WHERE oid = 'identity.organizations'::regclass)
                        AND (SELECT relrowsecurity AND relforcerowsecurity
                             FROM pg_class
                             WHERE oid = 'identity.memberships'::regclass)
                    """));
            }

            Assert.IsEmpty(await dbContext.Database.GetPendingMigrationsAsync());

            await migrator.MigrateAsync(PreviousMigration);
            await using (var rolledBackConnection = new NpgsqlConnection(connectionString))
            {
                await rolledBackConnection.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    rolledBackConnection,
                    """
                    SELECT
                        (SELECT count(*) FROM identity.organization_memberships) = 2
                        AND to_regclass('identity.organizations') IS NULL
                        AND to_regclass('identity.memberships') IS NULL
                        AND to_regclass('identity.organization_creation_ledgers') IS NULL
                        AND to_regclass('identity.organization_creation_key_aliases') IS NULL
                        AND to_regclass('identity."UX_sessions_Id_UserId"') IS NULL
                    """));

                await using var rolledBackWriter = new NpgsqlCommand(
                    """
                    INSERT INTO identity.organization_memberships
                        ("UserId", "OrganizationId", "OrganizationName", "Role")
                    VALUES
                        (@userId, @organizationId, 'Legacy organization three', 'owner')
                    """,
                    rolledBackConnection);
                rolledBackWriter.Parameters.AddWithValue("userId", secondUserId);
                rolledBackWriter.Parameters.AddWithValue("organizationId", Guid.NewGuid());
                Assert.AreEqual(1, await rolledBackWriter.ExecuteNonQueryAsync());
            }

            await migrator.MigrateAsync();

            await using var rolledForward = CreateDbContext(connectionString);
            Assert.IsEmpty(await rolledForward.Database.GetPendingMigrationsAsync());
            Assert.AreEqual(3, await rolledForward.Memberships.CountAsync());
            Assert.AreEqual(0, await rolledForward.AuthoritativeMemberships.CountAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task ConstraintsBindProtocolResultsAndOpaqueDigests()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        DatabaseFixture fixture = await SeedCurrentModelAsync(scenario.ConnectionString);

        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();

        PostgresException shortDigest = await Assert.ThrowsExactlyAsync<PostgresException>(
            async () =>
            {
                await using var command = new NpgsqlCommand(
                    """
                    INSERT INTO identity.organization_creation_key_aliases
                        ("Id", "LedgerId", "ScopeKind", "Namespace", "Operation",
                         "KeyVersion", "KeyDigest", "CreatedAtUtc")
                    VALUES
                        (@id, @ledgerId, 'platform', 'organization-bootstrap',
                         'create_organization', 'v2', @digest, now())
                    """,
                    connection);
                command.Parameters.AddWithValue("id", Guid.NewGuid());
                command.Parameters.AddWithValue("ledgerId", fixture.LedgerId);
                command.Parameters.AddWithValue("digest", Digest(0x2a, 31));
                await command.ExecuteNonQueryAsync();
            });
        Assert.AreEqual(PostgresErrorCodes.CheckViolation, shortDigest.SqlState);

        PostgresException wrongOwnerSession = await Assert.ThrowsExactlyAsync<PostgresException>(
            async () =>
            {
                await using var command = LedgerInsertCommand(
                    connection,
                    Guid.NewGuid(),
                    fixture.SecondUserId,
                    fixture.FirstSessionId,
                    fixture.FirstAuthorizationVersion,
                    state: "in_progress");
                await command.ExecuteNonQueryAsync();
            });
        Assert.AreEqual(PostgresErrorCodes.ForeignKeyViolation, wrongOwnerSession.SqlState);

        Guid crossActorMembershipId = Guid.NewGuid();
        await using (var crossActorMembership = new NpgsqlCommand(
            """
            INSERT INTO identity.memberships
                ("Id", "OrganizationId", "UserId", "Role", "Status",
                 "SecurityVersion", "CreatedAtUtc")
            VALUES
                (@id, @organizationId, @userId, 'owner', 'active', 1, now())
            """,
            connection))
        {
            crossActorMembership.Parameters.AddWithValue("id", crossActorMembershipId);
            crossActorMembership.Parameters.AddWithValue(
                "organizationId",
                fixture.FirstOrganizationId);
            crossActorMembership.Parameters.AddWithValue("userId", fixture.SecondUserId);
            Assert.AreEqual(1, await crossActorMembership.ExecuteNonQueryAsync());
        }

        PostgresException mismatchedResult = await Assert.ThrowsExactlyAsync<PostgresException>(
            async () =>
            {
                await using var command = LedgerInsertCommand(
                    connection,
                    Guid.NewGuid(),
                    fixture.FirstUserId,
                    fixture.FirstSessionId,
                    fixture.FirstAuthorizationVersion,
                    state: "succeeded",
                    organizationId: fixture.FirstOrganizationId,
                    membershipId: crossActorMembershipId);
                await command.ExecuteNonQueryAsync();
            });
        Assert.AreEqual(PostgresErrorCodes.ForeignKeyViolation, mismatchedResult.SqlState);

        PostgresException invalidSecurityVersion = await Assert.ThrowsExactlyAsync<PostgresException>(
            async () =>
            {
                await using var command = new NpgsqlCommand(
                    """
                    INSERT INTO identity.memberships
                        ("Id", "OrganizationId", "UserId", "Role", "Status",
                         "SecurityVersion", "CreatedAtUtc")
                    VALUES
                        (@id, @organizationId, @userId, 'owner', 'active', 0, now())
                    """,
                    connection);
                command.Parameters.AddWithValue("id", Guid.NewGuid());
                command.Parameters.AddWithValue("organizationId", fixture.FirstOrganizationId);
                command.Parameters.AddWithValue("userId", fixture.SecondUserId);
                await command.ExecuteNonQueryAsync();
            });
        Assert.AreEqual(PostgresErrorCodes.CheckViolation, invalidSecurityVersion.SqlState);
    }

    [TestMethod]
    public async Task RuntimeRolesEnforceActorAndTenantIsolation()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        DatabaseFixture fixture = await SeedCurrentModelAsync(scenario.ConnectionString);

        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT
                bool_and(NOT rolcanlogin AND NOT rolinherit AND NOT rolsuper
                         AND NOT rolcreatedb AND NOT rolcreaterole
                         AND NOT rolreplication AND NOT rolbypassrls)
                AND NOT EXISTS (
                    SELECT 1
                    FROM pg_auth_members AS membership
                    JOIN pg_roles AS granted_role ON granted_role.oid = membership.roleid
                    JOIN pg_roles AS member_role ON member_role.oid = membership.member
                    WHERE granted_role.rolname = 'agro_identity_owner'
                      AND member_role.rolname IN (
                          'agro_identity_app', 'agro_identity_job', 'agro_identity_discovery'))
            FROM pg_roles
            WHERE rolname IN (
                'agro_identity_owner', 'agro_identity_migrator', 'agro_identity_app',
                'agro_identity_job', 'agro_identity_discovery')
            HAVING count(*) = 5
            """));

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT NOT has_table_privilege('agro_identity_app', 'identity.users', 'SELECT')
               AND NOT has_table_privilege('agro_identity_app', 'identity.sessions', 'SELECT')
               AND has_column_privilege(
                    'agro_identity_app', 'identity.sessions', 'Id', 'SELECT')
               AND has_column_privilege(
                    'agro_identity_app', 'identity.sessions', 'UserId', 'SELECT')
               AND has_column_privilege(
                    'agro_identity_app', 'identity.sessions', 'Version', 'SELECT')
               AND NOT has_column_privilege(
                    'agro_identity_app', 'identity.sessions', 'TokenHash', 'SELECT')
            """));

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT has_function_privilege(
                       'agro_identity_app',
                       'identity.organization_creation_current_key_covered(text)',
                       'EXECUTE')
               AND NOT has_function_privilege(
                       'agro_identity_job',
                       'identity.organization_creation_current_key_covered(text)',
                       'EXECUTE')
               AND NOT has_function_privilege(
                       'agro_identity_discovery',
                       'identity.organization_creation_current_key_covered(text)',
                       'EXECUTE')
               AND NOT EXISTS (
                    SELECT 1
                    FROM pg_proc AS procedure
                    JOIN pg_namespace AS namespace ON namespace.oid = procedure.pronamespace
                    WHERE namespace.nspname = 'identity'
                      AND procedure.proname = 'organization_creation_current_key_covered'
                      AND (
                          procedure.prosecdef IS NOT TRUE
                          OR procedure.provolatile <> 's'
                          OR procedure.proconfig <> ARRAY['search_path=pg_catalog']
                          OR procedure.prosrc LIKE '%TokenHash%'
                          OR procedure.prosrc LIKE '%RequestFingerprint%'
                          OR procedure.prosrc LIKE '%KeyDigest%'
                      ))
            """));

        await AssertOrganizationAuthorizationReadIsLeastPrivilegeAsync(
            connection,
            fixture);
        await AssertKeyCoverageFunctionAsync(connection, fixture);

        await AssertAppScopeAsync(
            connection,
            fixture.FirstUserId,
            "platform",
            fixture.FirstOrganizationId,
            expectedOrganizations: 1,
            expectedMemberships: 1,
            expectedAliases: 1);
        await AssertAppScopeAsync(
            connection,
            fixture.FirstUserId,
            "tenant",
            fixture.FirstOrganizationId,
            expectedOrganizations: 1,
            expectedMemberships: 1,
            expectedAliases: 0);
        await AssertAppScopeAsync(
            connection,
            fixture.FirstUserId,
            "tenant",
            fixture.SecondOrganizationId,
            expectedOrganizations: 0,
            expectedMemberships: 0,
            expectedAliases: 0);
        await AssertAppScopeAsync(
            connection,
            fixture.SecondUserId,
            "platform",
            fixture.SecondOrganizationId,
            expectedOrganizations: 1,
            expectedMemberships: 1,
            expectedAliases: 0);

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT current_role = session_user
               AND coalesce(current_setting('app.current_actor_id', true), '') = ''
               AND coalesce(current_setting('app.current_scope_kind', true), '') = ''
               AND coalesce(current_setting('app.current_organization_id', true), '') = ''
            """));

        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
        {
            await SetRoleAndContextAsync(
                connection,
                transaction,
                "agro_identity_discovery",
                fixture.FirstUserId,
                scopeKind: null,
                organizationId: null);
            Assert.AreEqual(1L, await ScalarInt64Async(
                connection,
                transaction,
                "SELECT count(*) FROM identity.organizations"));
            Assert.AreEqual(1L, await ScalarInt64Async(
                connection,
                transaction,
                "SELECT count(*) FROM identity.memberships"));
            await transaction.RollbackAsync();
        }

        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
        {
            await SetRoleAndContextAsync(
                connection,
                transaction,
                "agro_identity_app",
                actorId: null,
                "platform",
                fixture.FirstOrganizationId);
            Assert.AreEqual(0L, await ScalarInt64Async(
                connection,
                transaction,
                "SELECT count(*) FROM identity.organizations"));
            Assert.AreEqual(0L, await ScalarInt64Async(
                connection,
                transaction,
                "SELECT count(*) FROM identity.organization_creation_key_aliases"));
            await transaction.RollbackAsync();
        }

        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
        {
            await SetRoleAndContextAsync(
                connection,
                transaction,
                "agro_identity_job",
                fixture.FirstUserId,
                "tenant",
                fixture.FirstOrganizationId);
            PostgresException denied = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await ScalarInt64Async(
                    connection,
                    transaction,
                    "SELECT count(*) FROM identity.organizations"));
            Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            await transaction.RollbackAsync();
        }
    }

    [TestMethod]
    public async Task ExternalPrincipalMustExplicitlyAssumeTheApplicationRole()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        string suffix = Guid.NewGuid().ToString("N");
        string permittedRole = $"agro_identity_test_permitted_{suffix}";
        string deniedRole = $"agro_identity_test_denied_{suffix}";
        string permittedPassword = $"test-{Guid.NewGuid():N}";
        string deniedPassword = $"test-{Guid.NewGuid():N}";

        await using var adminConnection = new NpgsqlConnection(scenario.ConnectionString);
        await adminConnection.OpenAsync();
        await using (var createRoles = new NpgsqlCommand(
            $"""
            CREATE ROLE {permittedRole} LOGIN NOINHERIT PASSWORD '{permittedPassword}';
            CREATE ROLE {deniedRole} LOGIN NOINHERIT PASSWORD '{deniedPassword}';
            GRANT agro_identity_app TO {permittedRole} WITH INHERIT FALSE, SET TRUE;
            """,
            adminConnection))
        {
            await createRoles.ExecuteNonQueryAsync();
        }

        try
        {
            var permittedBuilder = new NpgsqlConnectionStringBuilder(scenario.ConnectionString)
            {
                Username = permittedRole,
                Password = permittedPassword,
                Pooling = false,
            };
            await using (var permittedConnection = new NpgsqlConnection(permittedBuilder.ConnectionString))
            {
                await permittedConnection.OpenAsync();
                await using NpgsqlTransaction transaction =
                    await permittedConnection.BeginTransactionAsync();

                PostgresException beforeSetRole =
                    await Assert.ThrowsExactlyAsync<PostgresException>(
                        async () => await ScalarInt64Async(
                            permittedConnection,
                            transaction,
                            "SELECT count(*) FROM identity.organizations"));
                Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, beforeSetRole.SqlState);
                await transaction.RollbackAsync();

                await using NpgsqlTransaction assumedTransaction =
                    await permittedConnection.BeginTransactionAsync();
                await SetRoleAndContextAsync(
                    permittedConnection,
                    assumedTransaction,
                    "agro_identity_app",
                    actorId: null,
                    "platform",
                    organizationId: null);
                Assert.AreEqual(0L, await ScalarInt64Async(
                    permittedConnection,
                    assumedTransaction,
                    "SELECT count(*) FROM identity.organizations"));
                await assumedTransaction.RollbackAsync();
            }

            var deniedBuilder = new NpgsqlConnectionStringBuilder(scenario.ConnectionString)
            {
                Username = deniedRole,
                Password = deniedPassword,
                Pooling = false,
            };
            await using var deniedConnection = new NpgsqlConnection(deniedBuilder.ConnectionString);
            await deniedConnection.OpenAsync();
            await using NpgsqlTransaction deniedTransaction =
                await deniedConnection.BeginTransactionAsync();
            PostgresException denied = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await SetRoleAndContextAsync(
                    deniedConnection,
                    deniedTransaction,
                    "agro_identity_app",
                    actorId: null,
                    "platform",
                    organizationId: null));
            Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            await deniedTransaction.RollbackAsync();
        }
        finally
        {
            await using var dropRoles = new NpgsqlCommand(
                $"""
                REVOKE agro_identity_app FROM {permittedRole};
                DROP ROLE IF EXISTS {permittedRole};
                DROP ROLE IF EXISTS {deniedRole};
                """,
                adminConnection);
            await dropRoles.ExecuteNonQueryAsync();
        }
    }

    private static IdentityDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static PostgreSqlTestServer RequirePostgreSql() =>
        IdentityTestAssembly.PostgreSql
        ?? throw new AssertFailedException(
            "PostgreSQL integration fixture could not start: "
            + IdentityTestAssembly.StartupError?.Message);

    private static async Task<DatabaseFixture> SeedCurrentModelAsync(string connectionString)
    {
        var fixture = new DatabaseFixture(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
            VALUES
                (@firstUserId, 'Security owner one', now(), 1),
                (@secondUserId, 'Security owner two', now(), 1);

            INSERT INTO identity.sessions
                ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                 "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                 "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
            VALUES
                (@firstSessionId, @firstUserId, @firstToken, now(), now() + interval '1 hour',
                 true, NULL, NULL, NULL, @firstAuthorizationVersion),
                (@secondSessionId, @secondUserId, @secondToken, now(), now() + interval '1 hour',
                 true, NULL, NULL, NULL, @secondAuthorizationVersion);

            INSERT INTO identity.organizations
                ("Id", "DisplayName", "Status", "CreatedByUserId", "CreatedAtUtc", "Version")
            VALUES
                (@firstOrganizationId, 'Private organization one', 'active', @firstUserId, now(), @firstOrganizationVersion),
                (@secondOrganizationId, 'Private organization two', 'active', @secondUserId, now(), @secondOrganizationVersion);

            INSERT INTO identity.memberships
                ("Id", "OrganizationId", "UserId", "Role", "Status", "SecurityVersion", "CreatedAtUtc")
            VALUES
                (@firstMembershipId, @firstOrganizationId, @firstUserId, 'owner', 'active', 1, now()),
                (@secondMembershipId, @secondOrganizationId, @secondUserId, 'owner', 'active', 1, now());

            INSERT INTO identity.organization_creation_ledgers
                ("Id", "ScopeKind", "Namespace", "Operation", "ContractVersion",
                 "CanonicalizationVersion", "ActorUserId", "SessionId", "AuthorizationVersion",
                 "RequestFingerprint", "State", "OrganizationId", "MembershipId", "LeaseOwner",
                 "FenceToken", "LeaseUntilUtc", "StartedAtUtc", "CompletedAtUtc", "Version")
            VALUES
                (@ledgerId, 'platform', 'organization-bootstrap', 'create_organization', 1, 1,
                 @firstUserId, @firstSessionId, @firstAuthorizationVersion, @fingerprint,
                 'succeeded', @firstOrganizationId, @firstMembershipId, @leaseOwner, 1,
                 now() + interval '1 minute', now(), now(), @ledgerVersion);

            INSERT INTO identity.organization_creation_key_aliases
                ("Id", "LedgerId", "ScopeKind", "Namespace", "Operation",
                 "KeyVersion", "KeyDigest", "CreatedAtUtc")
            VALUES
                (@aliasId, @ledgerId, 'platform', 'organization-bootstrap',
                 'create_organization', 'v1', @keyDigest, now());
            """,
            connection);
        command.Parameters.AddWithValue("firstUserId", fixture.FirstUserId);
        command.Parameters.AddWithValue("secondUserId", fixture.SecondUserId);
        command.Parameters.AddWithValue("firstSessionId", fixture.FirstSessionId);
        command.Parameters.AddWithValue("secondSessionId", fixture.SecondSessionId);
        command.Parameters.AddWithValue("firstAuthorizationVersion", fixture.FirstAuthorizationVersion);
        command.Parameters.AddWithValue("secondAuthorizationVersion", Guid.NewGuid());
        command.Parameters.AddWithValue("firstToken", Digest(0x11));
        command.Parameters.AddWithValue("secondToken", Digest(0x22));
        command.Parameters.AddWithValue("firstOrganizationId", fixture.FirstOrganizationId);
        command.Parameters.AddWithValue("secondOrganizationId", fixture.SecondOrganizationId);
        command.Parameters.AddWithValue("firstOrganizationVersion", Guid.NewGuid());
        command.Parameters.AddWithValue("secondOrganizationVersion", Guid.NewGuid());
        command.Parameters.AddWithValue("firstMembershipId", fixture.FirstMembershipId);
        command.Parameters.AddWithValue("secondMembershipId", fixture.SecondMembershipId);
        command.Parameters.AddWithValue("ledgerId", fixture.LedgerId);
        command.Parameters.AddWithValue("fingerprint", Digest(0x33));
        command.Parameters.AddWithValue("leaseOwner", Guid.NewGuid());
        command.Parameters.AddWithValue("ledgerVersion", Guid.NewGuid());
        command.Parameters.AddWithValue("aliasId", Guid.NewGuid());
        command.Parameters.AddWithValue("keyDigest", Digest(0x44));
        Assert.AreEqual(10, await command.ExecuteNonQueryAsync());
        return fixture;
    }

    private static NpgsqlCommand LedgerInsertCommand(
        NpgsqlConnection connection,
        Guid ledgerId,
        Guid actorId,
        Guid sessionId,
        Guid authorizationVersion,
        string state,
        Guid? organizationId = null,
        Guid? membershipId = null)
    {
        var command = new NpgsqlCommand(
            """
            INSERT INTO identity.organization_creation_ledgers
                ("Id", "ScopeKind", "Namespace", "Operation", "ContractVersion",
                 "CanonicalizationVersion", "ActorUserId", "SessionId", "AuthorizationVersion",
                 "RequestFingerprint", "State", "OrganizationId", "MembershipId", "LeaseOwner",
                 "FenceToken", "LeaseUntilUtc", "StartedAtUtc", "CompletedAtUtc", "Version")
            VALUES
                (@id, 'platform', 'organization-bootstrap', 'create_organization', 1, 1,
                 @actorId, @sessionId, @authorizationVersion, @fingerprint, @state,
                 @organizationId, @membershipId, @leaseOwner, 1,
                 now() + interval '1 minute', now(),
                 CASE WHEN @state = 'in_progress' THEN NULL ELSE now() END,
                 @version)
            """,
            connection);
        command.Parameters.AddWithValue("id", ledgerId);
        command.Parameters.AddWithValue("actorId", actorId);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("authorizationVersion", authorizationVersion);
        command.Parameters.AddWithValue("fingerprint", Digest(0x55));
        command.Parameters.AddWithValue("state", state);
        command.Parameters.Add("organizationId", NpgsqlDbType.Uuid).Value =
            organizationId ?? (object)DBNull.Value;
        command.Parameters.Add("membershipId", NpgsqlDbType.Uuid).Value =
            membershipId ?? (object)DBNull.Value;
        command.Parameters.AddWithValue("leaseOwner", Guid.NewGuid());
        command.Parameters.AddWithValue("version", Guid.NewGuid());
        return command;
    }

    private static async Task AssertAppScopeAsync(
        NpgsqlConnection connection,
        Guid actorId,
        string scopeKind,
        Guid organizationId,
        long expectedOrganizations,
        long expectedMemberships,
        long expectedAliases)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetRoleAndContextAsync(
            connection,
            transaction,
            "agro_identity_app",
            actorId,
            scopeKind,
            organizationId);
        Assert.AreEqual(expectedOrganizations, await ScalarInt64Async(
            connection,
            transaction,
            "SELECT count(*) FROM identity.organizations"));
        Assert.AreEqual(expectedMemberships, await ScalarInt64Async(
            connection,
            transaction,
            "SELECT count(*) FROM identity.memberships"));
        Assert.AreEqual(expectedAliases, await ScalarInt64Async(
            connection,
            transaction,
            "SELECT count(*) FROM identity.organization_creation_key_aliases"));
        await transaction.RollbackAsync();
    }

    private static async Task AssertOrganizationAuthorizationReadIsLeastPrivilegeAsync(
        NpgsqlConnection connection,
        DatabaseFixture fixture)
    {
        await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync())
        {
            await SetRoleAndContextAsync(
                connection,
                transaction,
                "agro_identity_app",
                fixture.FirstUserId,
                "platform",
                organizationId: null);

            await using var authorizationQuery = new NpgsqlCommand(
                """
                SELECT count(*)
                FROM identity.sessions
                WHERE "Id" = @sessionId
                  AND "UserId" = @userId
                  AND "RevokedAtUtc" IS NULL
                  AND "ExpiresAtUtc" > now()
                  AND "IsAuthenticationAssuranceVerified"
                  AND "Version" = @authorizationVersion
                """,
                connection,
                transaction);
            authorizationQuery.Parameters.AddWithValue("sessionId", fixture.FirstSessionId);
            authorizationQuery.Parameters.AddWithValue("userId", fixture.FirstUserId);
            authorizationQuery.Parameters.AddWithValue(
                "authorizationVersion",
                fixture.FirstAuthorizationVersion);
            Assert.AreEqual(1L, await authorizationQuery.ExecuteScalarAsync());

            Assert.AreEqual(1L, await ScalarInt64Async(
                connection,
                transaction,
                "SELECT count(*) FROM identity.sessions"));
            await using var otherActorQuery = new NpgsqlCommand(
                "SELECT count(*) FROM identity.sessions WHERE \"Id\" = @sessionId",
                connection,
                transaction);
            otherActorQuery.Parameters.AddWithValue("sessionId", fixture.SecondSessionId);
            Assert.AreEqual(0L, await otherActorQuery.ExecuteScalarAsync());
            await transaction.RollbackAsync();
        }

        await AssertColumnReadDeniedAsync(
            connection,
            fixture.FirstUserId,
            "SELECT \"TokenHash\" FROM identity.sessions LIMIT 1");
        await AssertColumnReadDeniedAsync(
            connection,
            fixture.FirstUserId,
            "SELECT \"DisplayName\" FROM identity.users LIMIT 1");
    }

    private static async Task AssertColumnReadDeniedAsync(
        NpgsqlConnection connection,
        Guid actorId,
        string sql)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetRoleAndContextAsync(
            connection,
            transaction,
            "agro_identity_app",
            actorId,
            "platform",
            organizationId: null);
        PostgresException denied = await Assert.ThrowsExactlyAsync<PostgresException>(
            async () =>
            {
                await using var command = new NpgsqlCommand(sql, connection, transaction);
                await command.ExecuteScalarAsync();
            });
        Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
        await transaction.RollbackAsync();
    }

    private static async Task AssertKeyCoverageFunctionAsync(
        NpgsqlConnection connection,
        DatabaseFixture fixture)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetRoleAndContextAsync(
            connection,
            transaction,
            "agro_identity_app",
            fixture.FirstUserId,
            "platform",
            organizationId: null);

        Assert.IsTrue(await KeyVersionIsCoveredAsync(connection, transaction, "v1"));
        Assert.IsFalse(await KeyVersionIsCoveredAsync(connection, transaction, "v2"));
        Assert.IsFalse(await KeyVersionIsCoveredAsync(connection, transaction, string.Empty));
        Assert.IsFalse(await KeyVersionIsCoveredAsync(connection, transaction, version: null));

        await using (var addCurrentAlias = new NpgsqlCommand(
            """
            INSERT INTO identity.organization_creation_key_aliases
                ("Id", "LedgerId", "ScopeKind", "Namespace", "Operation",
                 "KeyVersion", "KeyDigest", "CreatedAtUtc")
            VALUES
                (@id, @ledgerId, 'platform', 'organization-bootstrap',
                 'create_organization', 'v2', @keyDigest, now())
            """,
            connection,
            transaction))
        {
            addCurrentAlias.Parameters.AddWithValue("id", Guid.NewGuid());
            addCurrentAlias.Parameters.AddWithValue("ledgerId", fixture.LedgerId);
            addCurrentAlias.Parameters.AddWithValue("keyDigest", Digest(0x66));
            Assert.AreEqual(1, await addCurrentAlias.ExecuteNonQueryAsync());
        }

        Assert.IsTrue(await KeyVersionIsCoveredAsync(connection, transaction, "v2"));
        await transaction.RollbackAsync();
    }

    private static async Task<bool> KeyVersionIsCoveredAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string? version)
    {
        await using var command = new NpgsqlCommand(
            "SELECT identity.organization_creation_current_key_covered(@version)",
            connection,
            transaction);
        command.Parameters.Add("version", NpgsqlDbType.Text).Value = version ?? (object)DBNull.Value;
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The key coverage probe returned null."));
    }

    private static async Task SetRoleAndContextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        Guid? actorId,
        string? scopeKind,
        Guid? organizationId)
    {
        if (role is not ("agro_identity_app" or "agro_identity_job" or "agro_identity_discovery"))
        {
            throw new ArgumentException("Unexpected test role.", nameof(role));
        }

        await using var roleCommand = new NpgsqlCommand($"SET LOCAL ROLE {role}", connection, transaction);
        await roleCommand.ExecuteNonQueryAsync();

        await using var contextCommand = new NpgsqlCommand(
            """
            SELECT set_config('app.current_actor_id', @actorId, true),
                   set_config('app.current_scope_kind', @scopeKind, true),
                   set_config('app.current_organization_id', @organizationId, true)
            """,
            connection,
            transaction);
        contextCommand.Parameters.AddWithValue("actorId", actorId?.ToString("D") ?? string.Empty);
        contextCommand.Parameters.AddWithValue("scopeKind", scopeKind ?? string.Empty);
        contextCommand.Parameters.AddWithValue(
            "organizationId",
            organizationId?.ToString("D") ?? string.Empty);
        await contextCommand.ExecuteNonQueryAsync();
    }

    private static async Task<bool> ScalarBooleanAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database security probe returned null."));
    }

    private static async Task<long> ScalarInt64Async(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database count probe returned null."));
    }

    private static byte[] Digest(byte value, int length = 32) =>
        Enumerable.Repeat(value, length).ToArray();

    private sealed record DatabaseFixture(
        Guid FirstUserId,
        Guid SecondUserId,
        Guid FirstSessionId,
        Guid SecondSessionId,
        Guid FirstAuthorizationVersion,
        Guid FirstOrganizationId,
        Guid SecondOrganizationId,
        Guid FirstMembershipId,
        Guid SecondMembershipId,
        Guid LedgerId);
}

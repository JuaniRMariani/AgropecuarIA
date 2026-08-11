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
public sealed class OrganizationOwnerInvitationDatabaseSecurityTests
{
    private const string PreviousMigration =
        "20260811012228_ImplementOrganizationBootstrap";

    [TestMethod]
    public async Task MigrationPreservesNMinusOneAndSupportsEphemeralRollbackAndRollForward()
    {
        PostgreSqlTestServer postgresql = RequirePostgreSql();
        string connectionString = await postgresql.CreateDatabaseAsync(CancellationToken.None);

        try
        {
            await using var dbContext = CreateDbContext(connectionString);
            IMigrator migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            Guid userId = Guid.NewGuid();
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var legacySeed = new NpgsqlCommand(
                    """
                    INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
                    VALUES (@userId, 'Legacy invitation owner', now(), 1);
                    INSERT INTO identity.organization_memberships
                        ("UserId", "OrganizationId", "OrganizationName", "Role")
                    VALUES (@userId, @organizationId, 'Legacy invitation organization', 'owner');
                    """,
                    connection);
                legacySeed.Parameters.AddWithValue("userId", userId);
                legacySeed.Parameters.AddWithValue("organizationId", Guid.NewGuid());
                Assert.AreEqual(2, await legacySeed.ExecuteNonQueryAsync());
            }

            await migrator.MigrateAsync();
            await using (var expanded = new NpgsqlConnection(connectionString))
            {
                await expanded.OpenAsync();
                await using var nMinusOneWriter = new NpgsqlCommand(
                    """
                    INSERT INTO identity.organization_memberships
                        ("UserId", "OrganizationId", "OrganizationName", "Role")
                    VALUES (@userId, @organizationId, 'Legacy writer after expand', 'owner')
                    """,
                    expanded);
                nMinusOneWriter.Parameters.AddWithValue("userId", userId);
                nMinusOneWriter.Parameters.AddWithValue("organizationId", Guid.NewGuid());
                Assert.AreEqual(1, await nMinusOneWriter.ExecuteNonQueryAsync());
                Assert.IsTrue(await ScalarBooleanAsync(
                    expanded,
                    """
                    SELECT to_regclass('identity.organization_owner_invitations') IS NOT NULL
                       AND (SELECT relrowsecurity AND relforcerowsecurity
                            FROM pg_class
                            WHERE oid = 'identity.organization_owner_invitations'::regclass)
                       AND to_regprocedure(
                            'identity.resolve_owner_invitation_by_token(text,bytea)') IS NOT NULL
                    """));
            }

            Assert.IsEmpty(await dbContext.Database.GetPendingMigrationsAsync());
            await migrator.MigrateAsync(PreviousMigration);
            await using (var rolledBack = new NpgsqlConnection(connectionString))
            {
                await rolledBack.OpenAsync();
                Assert.IsTrue(await ScalarBooleanAsync(
                    rolledBack,
                    """
                    SELECT to_regclass('identity.organization_owner_invitations') IS NULL
                       AND to_regprocedure(
                            'identity.resolve_owner_invitation_by_token(text,bytea)') IS NULL
                       AND (SELECT count(*) FROM identity.organization_memberships) = 2
                    """));
            }

            await migrator.MigrateAsync();
            await using var rolledForward = CreateDbContext(connectionString);
            Assert.IsEmpty(await rolledForward.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await postgresql.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task ConstraintsBindCreatorAcceptanceStateAndOpaqueDigests()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        InvitationFixture fixture = await SeedAsync(scenario.ConnectionString);
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();

        await AssertSqlStateAsync(
            connection,
            InvitationInsertCommand(
                connection,
                Guid.NewGuid(),
                fixture.OrganizationA,
                fixture.OwnerA,
                fixture.OwnerBSession,
                Digest(0x31),
                Digest(0x41)),
            PostgresErrorCodes.ForeignKeyViolation);

        await AssertSqlStateAsync(
            connection,
            InvitationInsertCommand(
                connection,
                Guid.NewGuid(),
                fixture.OrganizationA,
                fixture.OwnerA,
                fixture.OwnerASession,
                Digest(0x32, 31),
                Digest(0x42)),
            PostgresErrorCodes.CheckViolation);

        await AssertSqlStateAsync(
            connection,
            InvitationInsertCommand(
                connection,
                Guid.NewGuid(),
                fixture.OrganizationA,
                fixture.OwnerA,
                fixture.OwnerASession,
                fixture.CreationDigestA,
                Digest(0x43)),
            PostgresErrorCodes.UniqueViolation);

        await AssertSqlStateAsync(
            connection,
            InvitationInsertCommand(
                connection,
                Guid.NewGuid(),
                fixture.OrganizationB,
                fixture.OwnerB,
                fixture.OwnerBSession,
                Digest(0x34),
                fixture.TokenDigestA),
            PostgresErrorCodes.UniqueViolation);

        await using var invalidAcceptance = new NpgsqlCommand(
            """
            UPDATE identity.organization_owner_invitations
            SET "Status" = 'accepted',
                "AcceptedAtUtc" = now(),
                "AcceptedByUserId" = @ownerB,
                "AcceptedMembershipId" = @ownerAMembership
            WHERE "Id" = @invitationId
            """,
            connection);
        invalidAcceptance.Parameters.AddWithValue("ownerB", fixture.OwnerB);
        invalidAcceptance.Parameters.AddWithValue("ownerAMembership", fixture.OwnerAMembership);
        invalidAcceptance.Parameters.AddWithValue("invitationId", fixture.InvitationA);
        await AssertSqlStateAsync(
            connection,
            invalidAcceptance,
            PostgresErrorCodes.ForeignKeyViolation);

        await using var persistedExpiry = new NpgsqlCommand(
            """
            UPDATE identity.organization_owner_invitations
            SET "Status" = 'expired'
            WHERE "Id" = @invitationId
            """,
            connection);
        persistedExpiry.Parameters.AddWithValue("invitationId", fixture.InvitationA);
        await AssertSqlStateAsync(
            connection,
            persistedExpiry,
            PostgresErrorCodes.CheckViolation);
    }

    [TestMethod]
    public async Task RlsAndGrantsIsolateTenantsNoContextPoolsAndNonApplicationRoles()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        InvitationFixture fixture = await SeedAsync(scenario.ConnectionString);
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();

        Assert.AreEqual(1L, await CountAsAppAsync(
            connection,
            fixture.OwnerA,
            "tenant",
            fixture.OrganizationA));
        Assert.AreEqual(1L, await CountAsAppAsync(
            connection,
            fixture.OwnerB,
            "tenant",
            fixture.OrganizationB));
        Assert.AreEqual(0L, await CountAsAppAsync(
            connection,
            fixture.OwnerA,
            "tenant",
            fixture.OrganizationB));
        Assert.AreEqual(0L, await CountAsAppAsync(
            connection,
            actorId: null,
            scopeKind: null,
            organizationId: null));

        await using (NpgsqlTransaction ownerTransaction = await connection.BeginTransactionAsync())
        {
            await SetRoleAndContextAsync(
                connection,
                ownerTransaction,
                "agro_identity_app",
                fixture.OwnerA,
                "tenant",
                fixture.OrganizationA);
            Guid invitationId = Guid.NewGuid();
            await using NpgsqlCommand create = InvitationInsertCommand(
                connection,
                invitationId,
                fixture.OrganizationA,
                fixture.OwnerA,
                fixture.OwnerASession,
                Digest(0x61),
                Digest(0x62));
            create.Transaction = ownerTransaction;
            Assert.AreEqual(1, await create.ExecuteNonQueryAsync());
            await using var revoke = new NpgsqlCommand(
                """
                UPDATE identity.organization_owner_invitations
                SET "Status" = 'revoked',
                    "RevokedAtUtc" = now(),
                    "RevokedByUserId" = @actorId,
                    "Version" = @version
                WHERE "Id" = @invitationId
                """,
                connection,
                ownerTransaction);
            revoke.Parameters.AddWithValue("actorId", fixture.OwnerA);
            revoke.Parameters.AddWithValue("version", Guid.NewGuid());
            revoke.Parameters.AddWithValue("invitationId", invitationId);
            Assert.AreEqual(1, await revoke.ExecuteNonQueryAsync());
            await ownerTransaction.RollbackAsync();
        }

        await using (NpgsqlTransaction crossTenant = await connection.BeginTransactionAsync())
        {
            await SetRoleAndContextAsync(
                connection,
                crossTenant,
                "agro_identity_app",
                fixture.OwnerA,
                "tenant",
                fixture.OrganizationB);
            await using NpgsqlCommand deniedCreate = InvitationInsertCommand(
                connection,
                Guid.NewGuid(),
                fixture.OrganizationB,
                fixture.OwnerA,
                fixture.OwnerASession,
                Digest(0x63),
                Digest(0x64));
            deniedCreate.Transaction = crossTenant;
            PostgresException denied = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await deniedCreate.ExecuteNonQueryAsync());
            Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            await crossTenant.RollbackAsync();
        }

        foreach (string role in new[] { "agro_identity_job", "agro_identity_discovery" })
        {
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
            await SetRoleAndContextAsync(
                connection,
                transaction,
                role,
                fixture.OwnerA,
                "tenant",
                fixture.OrganizationA);
            await using var deniedCommand = new NpgsqlCommand(
                "SELECT count(*) FROM identity.organization_owner_invitations",
                connection,
                transaction);
            PostgresException denied = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await deniedCommand.ExecuteScalarAsync());
            Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            await transaction.RollbackAsync();
        }

        Assert.IsTrue(await ScalarBooleanAsync(
            connection,
            """
            SELECT NOT has_table_privilege(
                       'agro_identity_job',
                       'identity.organization_owner_invitations',
                       'SELECT')
               AND NOT has_table_privilege(
                       'agro_identity_discovery',
                       'identity.organization_owner_invitations',
                       'SELECT')
               AND has_table_privilege(
                       'agro_identity_app',
                       'identity.organization_owner_invitations',
                       'SELECT,INSERT,UPDATE')
               AND NOT has_function_privilege(
                       'public',
                       'identity.resolve_owner_invitation_by_token(text,bytea)',
                       'EXECUTE')
               AND has_function_privilege(
                       'agro_identity_app',
                       'identity.resolve_owner_invitation_by_token(text,bytea)',
                       'EXECUTE')
               AND NOT has_function_privilege(
                       'agro_identity_job',
                       'identity.resolve_owner_invitation_by_token(text,bytea)',
                       'EXECUTE')
               AND NOT (SELECT rolbypassrls FROM pg_roles WHERE rolname = 'agro_identity_app')
               AND (SELECT rolname <> 'agro_identity_app'
                    FROM pg_class
                    JOIN pg_roles ON pg_roles.oid = pg_class.relowner
                    WHERE pg_class.oid =
                          'identity.organization_owner_invitations'::regclass)
            """));

        var pooledBuilder = new NpgsqlConnectionStringBuilder(scenario.ConnectionString)
        {
            Pooling = true,
            ApplicationName = $"invitation-pool-{Guid.NewGuid():N}",
        };
        await using (var firstLease = new NpgsqlConnection(pooledBuilder.ConnectionString))
        {
            await firstLease.OpenAsync();
            Assert.AreEqual(1L, await CountAsAppAsync(
                firstLease,
                fixture.OwnerA,
                "tenant",
                fixture.OrganizationA));
        }

        await using (var secondLease = new NpgsqlConnection(pooledBuilder.ConnectionString))
        {
            await secondLease.OpenAsync();
            Assert.AreEqual(0L, await CountAsAppAsync(
                secondLease,
                actorId: null,
                scopeKind: null,
                organizationId: null));
        }

        NpgsqlConnection.ClearPool(new NpgsqlConnection(pooledBuilder.ConnectionString));
    }

    [TestMethod]
    public async Task TokenLookupExposesOnlyOneRowAndEnablesBoundAcceptanceAndRotationGate()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        InvitationFixture fixture = await SeedAsync(scenario.ConnectionString);
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();

        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetRoleAndContextAsync(
            connection,
            transaction,
            "agro_identity_app",
            fixture.Invitee,
            "platform",
            organizationId: null);

        await SetTenantContextAsync(connection, transaction, fixture.OrganizationA);
        await using (var unresolvedAccept = new NpgsqlCommand(
            """
            UPDATE identity.organization_owner_invitations
            SET "Status" = 'accepted',
                "AcceptedAtUtc" = now(),
                "AcceptedByUserId" = @userId,
                "AcceptedMembershipId" = @membershipId,
                "Version" = @version
            WHERE "Id" = @invitationId
            """,
            connection,
            transaction))
        {
            unresolvedAccept.Parameters.AddWithValue("userId", fixture.Invitee);
            unresolvedAccept.Parameters.AddWithValue("membershipId", Guid.NewGuid());
            unresolvedAccept.Parameters.AddWithValue("version", Guid.NewGuid());
            unresolvedAccept.Parameters.AddWithValue("invitationId", fixture.InvitationA);
            Assert.AreEqual(0, await unresolvedAccept.ExecuteNonQueryAsync());
        }

        await SetRoleAndContextAsync(
            connection,
            transaction,
            "agro_identity_app",
            fixture.Invitee,
            "platform",
            organizationId: null);

        Assert.IsTrue(await RetainedKeysCoveredAsync(connection, transaction, "v1", "v2"));
        Assert.IsFalse(await RetainedKeysCoveredAsync(connection, transaction, "v1"));
        Assert.IsFalse(await RetainedKeysCoveredAsync(connection, transaction, versions: null));

        Guid? resolved = await ResolveAsync(
            connection,
            transaction,
            "v1",
            fixture.TokenDigestA);
        Assert.AreEqual(fixture.InvitationA, resolved);
        Assert.AreEqual(1L, await ScalarInt64Async(
            connection,
            transaction,
            "SELECT count(*) FROM identity.organization_owner_invitations"));

        await SetTenantContextAsync(connection, transaction, fixture.OrganizationA);
        Assert.AreEqual(1L, await ScalarInt64Async(
            connection,
            transaction,
            "SELECT count(*) FROM identity.organizations"));

        Guid acceptedMembershipId = Guid.NewGuid();
        await using (var insertMembership = new NpgsqlCommand(
            """
            INSERT INTO identity.memberships
                ("Id", "OrganizationId", "UserId", "Role", "Status",
                 "SecurityVersion", "CreatedAtUtc")
            VALUES
                (@id, @organizationId, @userId, 'owner', 'active', 1, now())
            """,
            connection,
            transaction))
        {
            insertMembership.Parameters.AddWithValue("id", acceptedMembershipId);
            insertMembership.Parameters.AddWithValue("organizationId", fixture.OrganizationA);
            insertMembership.Parameters.AddWithValue("userId", fixture.Invitee);
            Assert.AreEqual(1, await insertMembership.ExecuteNonQueryAsync());
        }

        await using (var accept = new NpgsqlCommand(
            """
            UPDATE identity.organization_owner_invitations
            SET "Status" = 'accepted',
                "AcceptedAtUtc" = now(),
                "AcceptedByUserId" = @userId,
                "AcceptedMembershipId" = @membershipId,
                "Version" = @version
            WHERE "Id" = @invitationId
            """,
            connection,
            transaction))
        {
            accept.Parameters.AddWithValue("userId", fixture.Invitee);
            accept.Parameters.AddWithValue("membershipId", acceptedMembershipId);
            accept.Parameters.AddWithValue("version", Guid.NewGuid());
            accept.Parameters.AddWithValue("invitationId", fixture.InvitationA);
            Assert.AreEqual(1, await accept.ExecuteNonQueryAsync());
        }

        await transaction.CommitAsync();

        await using NpgsqlTransaction otherActor = await connection.BeginTransactionAsync();
        await SetRoleAndContextAsync(
            connection,
            otherActor,
            "agro_identity_app",
            fixture.OwnerB,
            "platform",
            organizationId: null);
        Assert.AreEqual(
            fixture.InvitationA,
            await ResolveAsync(connection, otherActor, "v1", fixture.TokenDigestA));
        Assert.IsFalse(await RetainedKeysCoveredAsync(connection, otherActor, "v2"));
        Assert.AreEqual(0L, await ScalarInt64Async(
            connection,
            otherActor,
            "SELECT count(*) FROM identity.organization_owner_invitations"));
        Assert.IsNull(await ResolveAsync(connection, otherActor, "v1", Digest(0xff)));
        Assert.AreEqual(0L, await ScalarInt64Async(
            connection,
            otherActor,
            "SELECT count(*) FROM identity.organization_owner_invitations"));
        await otherActor.RollbackAsync();
    }

    private static async Task<InvitationFixture> SeedAsync(string connectionString)
    {
        var fixture = new InvitationFixture(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Digest(0x11), Digest(0x12), Digest(0x21), Digest(0x22));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO identity.users ("Id", "DisplayName", "CreatedAtUtc", "Version")
            VALUES
                (@ownerA, 'Invitation owner A', now(), 1),
                (@ownerB, 'Invitation owner B', now(), 1),
                (@invitee, 'Invitation recipient', now(), 1);
            INSERT INTO identity.sessions
                ("Id", "UserId", "TokenHash", "AuthenticatedAtUtc", "ExpiresAtUtc",
                 "IsAuthenticationAssuranceVerified", "StrongAuthenticatedAtUtc",
                 "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
            VALUES
                (@ownerASession, @ownerA, @sessionDigestA, now(), now() + interval '1 hour',
                 true, NULL, NULL, NULL, @sessionVersionA),
                (@ownerBSession, @ownerB, @sessionDigestB, now(), now() + interval '1 hour',
                 true, NULL, NULL, NULL, @sessionVersionB);
            INSERT INTO identity.organizations
                ("Id", "DisplayName", "Status", "CreatedByUserId", "CreatedAtUtc", "Version")
            VALUES
                (@organizationA, 'Invitation organization A', 'active', @ownerA, now(), @organizationVersionA),
                (@organizationB, 'Invitation organization B', 'active', @ownerB, now(), @organizationVersionB);
            INSERT INTO identity.memberships
                ("Id", "OrganizationId", "UserId", "Role", "Status",
                 "SecurityVersion", "CreatedAtUtc")
            VALUES
                (@ownerAMembership, @organizationA, @ownerA, 'owner', 'active', 1, now()),
                (@ownerBMembership, @organizationB, @ownerB, 'owner', 'active', 1, now());
            INSERT INTO identity.organization_owner_invitations
                ("Id", "OrganizationId", "CreatedByUserId", "CreationSessionId",
                 "CreationKeyVersion", "CreationKeyDigest", "TokenKeyVersion", "TokenDigest",
                 "Status", "CreatedAtUtc", "ExpiresAtUtc", "Version")
            VALUES
                (@invitationA, @organizationA, @ownerA, @ownerASession,
                 'v1', @creationDigestA, 'v1', @tokenDigestA,
                 'pending', now() - interval '5 minutes', now() + interval '1 hour', @invitationVersionA),
                (@invitationB, @organizationB, @ownerB, @ownerBSession,
                 'v2', @creationDigestB, 'v2', @tokenDigestB,
                 'pending', now() - interval '5 minutes', now() + interval '1 hour', @invitationVersionB);
            """,
            connection);
        command.Parameters.AddWithValue("ownerA", fixture.OwnerA);
        command.Parameters.AddWithValue("ownerB", fixture.OwnerB);
        command.Parameters.AddWithValue("invitee", fixture.Invitee);
        command.Parameters.AddWithValue("ownerASession", fixture.OwnerASession);
        command.Parameters.AddWithValue("ownerBSession", fixture.OwnerBSession);
        command.Parameters.AddWithValue("sessionDigestA", Digest(0x01));
        command.Parameters.AddWithValue("sessionDigestB", Digest(0x02));
        command.Parameters.AddWithValue("sessionVersionA", Guid.NewGuid());
        command.Parameters.AddWithValue("sessionVersionB", Guid.NewGuid());
        command.Parameters.AddWithValue("organizationA", fixture.OrganizationA);
        command.Parameters.AddWithValue("organizationB", fixture.OrganizationB);
        command.Parameters.AddWithValue("organizationVersionA", Guid.NewGuid());
        command.Parameters.AddWithValue("organizationVersionB", Guid.NewGuid());
        command.Parameters.AddWithValue("ownerAMembership", fixture.OwnerAMembership);
        command.Parameters.AddWithValue("ownerBMembership", fixture.OwnerBMembership);
        command.Parameters.AddWithValue("invitationA", fixture.InvitationA);
        command.Parameters.AddWithValue("invitationB", fixture.InvitationB);
        command.Parameters.AddWithValue("creationDigestA", fixture.CreationDigestA);
        command.Parameters.AddWithValue("creationDigestB", fixture.CreationDigestB);
        command.Parameters.AddWithValue("tokenDigestA", fixture.TokenDigestA);
        command.Parameters.AddWithValue("tokenDigestB", fixture.TokenDigestB);
        command.Parameters.AddWithValue("invitationVersionA", Guid.NewGuid());
        command.Parameters.AddWithValue("invitationVersionB", Guid.NewGuid());
        Assert.AreEqual(11, await command.ExecuteNonQueryAsync());
        return fixture;
    }

    private static NpgsqlCommand InvitationInsertCommand(
        NpgsqlConnection connection,
        Guid id,
        Guid organizationId,
        Guid createdByUserId,
        Guid creationSessionId,
        byte[] creationDigest,
        byte[] tokenDigest)
    {
        var command = new NpgsqlCommand(
            """
            INSERT INTO identity.organization_owner_invitations
                ("Id", "OrganizationId", "CreatedByUserId", "CreationSessionId",
                 "CreationKeyVersion", "CreationKeyDigest", "TokenKeyVersion", "TokenDigest",
                 "Status", "CreatedAtUtc", "ExpiresAtUtc", "Version")
            VALUES
                (@id, @organizationId, @createdByUserId, @creationSessionId,
                 'v1', @creationDigest, 'v1', @tokenDigest,
                 'pending', now(), now() + interval '1 hour', @version)
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("organizationId", organizationId);
        command.Parameters.AddWithValue("createdByUserId", createdByUserId);
        command.Parameters.AddWithValue("creationSessionId", creationSessionId);
        command.Parameters.AddWithValue("creationDigest", creationDigest);
        command.Parameters.AddWithValue("tokenDigest", tokenDigest);
        command.Parameters.AddWithValue("version", Guid.NewGuid());
        return command;
    }

    private static async Task AssertSqlStateAsync(
        NpgsqlConnection connection,
        NpgsqlCommand command,
        string expectedSqlState)
    {
        await using (command)
        {
            PostgresException exception = await Assert.ThrowsExactlyAsync<PostgresException>(
                async () => await command.ExecuteNonQueryAsync());
            Assert.AreEqual(expectedSqlState, exception.SqlState);
        }
    }

    private static async Task<long> CountAsAppAsync(
        NpgsqlConnection connection,
        Guid? actorId,
        string? scopeKind,
        Guid? organizationId)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await SetRoleAndContextAsync(
            connection,
            transaction,
            "agro_identity_app",
            actorId,
            scopeKind,
            organizationId);
        long count = await ScalarInt64Async(
            connection,
            transaction,
            "SELECT count(*) FROM identity.organization_owner_invitations");
        await transaction.RollbackAsync();
        return count;
    }

    private static async Task<Guid?> ResolveAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string version,
        byte[] digest)
    {
        await using var command = new NpgsqlCommand(
            "SELECT identity.resolve_owner_invitation_by_token(@version, @digest)",
            connection,
            transaction);
        command.Parameters.AddWithValue("version", version);
        command.Parameters.AddWithValue("digest", digest);
        object? value = await command.ExecuteScalarAsync();
        return value is DBNull or null ? null : (Guid)value;
    }

    private static async Task<bool> RetainedKeysCoveredAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        params string[]? versions)
    {
        await using var command = new NpgsqlCommand(
            "SELECT identity.owner_invitation_retained_key_covered(@versions)",
            connection,
            transaction);
        command.Parameters.Add("versions", NpgsqlDbType.Array | NpgsqlDbType.Text).Value =
            versions ?? (object)DBNull.Value;
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The retained-key coverage probe returned null."));
    }

    private static async Task SetTenantContextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid organizationId)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT set_config('app.current_scope_kind', 'tenant', true),
                   set_config('app.current_organization_id', @organizationId, true)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("organizationId", organizationId.ToString("D"));
        await command.ExecuteNonQueryAsync();
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

        await using var roleCommand = new NpgsqlCommand(
            $"SET LOCAL ROLE {role}",
            connection,
            transaction);
        await roleCommand.ExecuteNonQueryAsync();
        await using var context = new NpgsqlCommand(
            """
            SELECT set_config('app.current_actor_id', @actorId, true),
                   set_config('app.current_scope_kind', @scopeKind, true),
                   set_config('app.current_organization_id', @organizationId, true),
                   set_config('app.current_invitation_id', '', true)
            """,
            connection,
            transaction);
        context.Parameters.AddWithValue("actorId", actorId?.ToString("D") ?? string.Empty);
        context.Parameters.AddWithValue("scopeKind", scopeKind ?? string.Empty);
        context.Parameters.AddWithValue(
            "organizationId",
            organizationId?.ToString("D") ?? string.Empty);
        await context.ExecuteNonQueryAsync();
    }

    private static async Task<bool> ScalarBooleanAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The database probe returned null."));
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

    private static IdentityDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static PostgreSqlTestServer RequirePostgreSql() =>
        IdentityTestAssembly.PostgreSql
        ?? throw new AssertFailedException(
            "PostgreSQL integration fixture could not start: "
            + IdentityTestAssembly.StartupError?.Message);

    private static byte[] Digest(byte value, int length = 32) =>
        Enumerable.Repeat(value, length).ToArray();

    private sealed record InvitationFixture(
        Guid OwnerA,
        Guid OwnerB,
        Guid Invitee,
        Guid OwnerASession,
        Guid OwnerBSession,
        Guid OrganizationA,
        Guid OrganizationB,
        Guid OwnerAMembership,
        Guid OwnerBMembership,
        Guid InvitationA,
        Guid InvitationB,
        byte[] CreationDigestA,
        byte[] CreationDigestB,
        byte[] TokenDigestA,
        byte[] TokenDigestB);
}

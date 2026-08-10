using AgropecuarIA.IdentitySpike.Api.Sessions;
using Npgsql;
using System.Globalization;

namespace AgropecuarIA.IdentitySpike.Tests;

[TestClass]
public sealed class MembershipDiscoveryRepositoryTests
{
    private static readonly Guid OrganizationA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid OrganizationB = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid UserOne = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UserTwo = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid UserThree = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid LimitUser = Guid.Parse("10000000-0000-0000-0000-000000000099");
    private static readonly Guid UnknownPermissionUser = Guid.Parse("10000000-0000-0000-0000-000000000098");

    [TestMethod]
    public async Task RepositoryReturnsOnlyActiveActorMembershipsInDeterministicOrder()
    {
        await using var repository = new PostgresMembershipDiscoveryRepository(
            TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));

        IReadOnlyList<OrganizationMembership> noMemberships =
            await repository.ListActiveForActorAsync(Guid.Parse("10000000-0000-0000-0000-000000000004"), default);
        IReadOnlyList<OrganizationMembership> oneMembership =
            await repository.ListActiveForActorAsync(UserOne, default);
        IReadOnlyList<OrganizationMembership> manyMemberships =
            await repository.ListActiveForActorAsync(UserThree, default);
        IReadOnlyList<OrganizationMembership> noPermissionMembership =
            await repository.ListActiveForActorAsync(UserTwo, default);

        Assert.IsEmpty(noMemberships);
        Assert.HasCount(1, oneMembership);
        AssertMembership(
            oneMembership[0],
            "a1111111-1111-4111-8111-111111111111",
            OrganizationA,
            ["tenant-record.read"]);

        Assert.HasCount(2, manyMemberships);
        AssertMembership(
            manyMemberships[0],
            "a2222222-2222-4222-8222-222222222221",
            OrganizationA,
            ["tenant-record.read"]);
        AssertMembership(
            manyMemberships[1],
            "a2222222-2222-4222-8222-222222222222",
            OrganizationB,
            ["tenant-record.read"]);

        Assert.HasCount(1, noPermissionMembership);
        AssertMembership(
            noPermissionMembership[0],
            "a3333333-3333-4333-8333-333333333333",
            OrganizationB,
            []);
    }

    [TestMethod]
    public async Task ConfigurationValidationAcceptsOnlyDiscoveryPrincipal()
    {
        await using (var discoveryRepository = new PostgresMembershipDiscoveryRepository(
            TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString")))
        {
            await discoveryRepository.ValidateConfigurationAsync(default);
        }

        await using var ownerRepository = new PostgresMembershipDiscoveryRepository(
            TestEnvironment.Require("IdentitySpike__OwnerConnectionString"));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => ownerRepository.ValidateConfigurationAsync(default));
    }

    [TestMethod]
    [DataRow("createdb")]
    [DataRow("createrole")]
    [DataRow("replication")]
    [DoNotParallelize]
    public async Task ConfigurationValidationRejectsDangerousRoleAttribute(string roleAttribute)
    {
        var ownerConnectionString = TestEnvironment.Require("IdentitySpike__OwnerConnectionString");
        await ResetDiscoveryRoleAttributesAsync(ownerConnectionString);
        try
        {
            await EnableDiscoveryRoleAttributeAsync(ownerConnectionString, roleAttribute);
            await using var repository = new PostgresMembershipDiscoveryRepository(
                TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => repository.ValidateConfigurationAsync(default));
        }
        finally
        {
            await ResetDiscoveryRoleAttributesAsync(ownerConnectionString);
        }
    }

    [TestMethod]
    public async Task ApplicationRejectsOwnerPrincipalBeforeServingRequests()
    {
        await using var factory = new IdentitySpikeFactory(
            TestEnvironment.Require("IdentitySpike__OwnerConnectionString"));

        Exception? startupFailure = null;
        try
        {
            using var client = factory.CreateBrowserClient();
            using var response = await client.GetAsync("/api/spike/session");
        }
        catch (Exception exception)
        {
            startupFailure = exception;
        }

        Assert.IsNotNull(startupFailure, "The application served requests with the owner principal.");
        StringAssert.Contains(
            startupFailure.ToString(),
            "Membership discovery database principal failed the required least-privilege checks.");
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task UnknownPermissionSetFailsClosedWithoutPartialResult()
    {
        var ownerConnectionString = TestEnvironment.Require("IdentitySpike__OwnerConnectionString");
        await DeleteUnknownPermissionFixturesAsync(ownerConnectionString);
        try
        {
            await using (var dataSource = NpgsqlDataSource.Create(ownerConnectionString))
            await using (var command = dataSource.CreateCommand(
                """
                insert into identity_spike.platform_user
                    (id, external_issuer, external_subject, contact_email, created_at)
                values
                    (@actor_user_id, 'https://fake-idp.invalid/email', 'unknown-permission',
                     'unknown-permission@example.invalid', now());

                insert into identity_spike.organization (id, display_name, is_active, created_at)
                values
                    ('00000000-0000-0000-0000-000000000098',
                     'Unknown permission fixture', true, now());

                insert into identity_spike.membership
                    (id, organization_id, platform_user_id, permission_set,
                     is_active, security_version, created_at)
                values
                    ('a9999999-9999-4999-8999-999999999998',
                     '00000000-0000-0000-0000-000000000098',
                     @actor_user_id, 'identity.unrecognized', true, 1, now());
                """))
            {
                command.Parameters.AddWithValue("actor_user_id", UnknownPermissionUser);
                await command.ExecuteNonQueryAsync();
            }

            await using var repository = new PostgresMembershipDiscoveryRepository(
                TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => repository.ListActiveForActorAsync(UnknownPermissionUser, default));
        }
        finally
        {
            await DeleteUnknownPermissionFixturesAsync(ownerConnectionString);
        }
    }

    [TestMethod]
    public async Task RepositoryRejectsMissingActorAndRlsHidesOtherActors()
    {
        await using var repository = new PostgresMembershipDiscoveryRepository(
            TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => repository.ListActiveForActorAsync(Guid.Empty, default));

        await using var dataSource = NpgsqlDataSource.Create(
            TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, UserOne);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "select id from identity_spike.membership order by id";
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(Guid.Parse("a1111111-1111-4111-8111-111111111111"), reader.GetGuid(0));
        Assert.IsFalse(await reader.ReadAsync(), "RLS exposed a membership owned by another actor.");
    }

    [TestMethod]
    public async Task ActorContextDoesNotSurviveCommitRollbackOrExceptionOnPooledConnection()
    {
        await using var dataSource = NpgsqlDataSource.Create(
            TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));

        await AssertActorCountAsync(dataSource, UserThree, expectedCount: 2, commit: true);
        await AssertNoActorContextAsync(dataSource);
        await AssertActorCountAsync(dataSource, UserOne, expectedCount: 1, commit: false);
        await AssertNoActorContextAsync(dataSource);

        await Assert.ThrowsExactlyAsync<SimulatedRequestException>(async () =>
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await SetActorAsync(connection, transaction, UserThree);
            Assert.AreEqual(2L, await CountMembershipsAsync(connection, transaction));
            throw new SimulatedRequestException();
        });

        await AssertNoActorContextAsync(dataSource);
    }

    [TestMethod]
    public async Task CancelledRequestDoesNotLeaveActorContextInPool()
    {
        await using var dataSource = NpgsqlDataSource.Create(
            TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));

        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await SetActorAsync(connection, transaction, UserThree);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "select pg_sleep(5)";
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await command.ExecuteNonQueryAsync(cancellation.Token);
            Assert.Fail("The PostgreSQL command should have been cancelled.");
        }
        catch (OperationCanceledException)
        {
            // The cancelled transaction is disposed before the pooled connection is reused.
        }

        await AssertNoActorContextAsync(dataSource);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task RepositoryFailsClosedAtOneHundredAndOneMemberships()
    {
        var ownerConnectionString = TestEnvironment.Require("IdentitySpike__OwnerConnectionString");
        await DeleteLimitFixturesAsync(ownerConnectionString);
        try
        {
            await using (var dataSource = NpgsqlDataSource.Create(ownerConnectionString))
            await using (var command = dataSource.CreateCommand(
                """
                insert into identity_spike.platform_user
                    (id, external_issuer, external_subject, contact_email, created_at)
                values
                    (@actor_user_id, 'https://fake-idp.invalid/email', 'membership-limit',
                     'membership-limit@example.invalid', now());

                insert into identity_spike.organization (id, display_name, is_active, created_at)
                select
                    md5('membership-limit-org-' || ordinal)::uuid,
                    'Membership limit fixture ' || lpad(ordinal::text, 3, '0'),
                    true,
                    now()
                from generate_series(1, 101) as ordinal;

                insert into identity_spike.membership
                    (id, organization_id, platform_user_id, permission_set,
                     is_active, security_version, created_at)
                select
                    md5('membership-limit-id-' || ordinal)::uuid,
                    md5('membership-limit-org-' || ordinal)::uuid,
                    @actor_user_id,
                    'tenant-record.read',
                    true,
                    1,
                    now()
                from generate_series(1, 101) as ordinal;
                """))
            {
                command.Parameters.AddWithValue("actor_user_id", LimitUser);
                await command.ExecuteNonQueryAsync();
            }

            await using var repository = new PostgresMembershipDiscoveryRepository(
                TestEnvironment.Require("IdentitySpike__DiscoveryConnectionString"));
            await Assert.ThrowsExactlyAsync<MembershipDiscoveryLimitExceededException>(
                () => repository.ListActiveForActorAsync(LimitUser, default));
        }
        finally
        {
            await DeleteLimitFixturesAsync(ownerConnectionString);
        }
    }

    private static void AssertMembership(
        OrganizationMembership actual,
        string expectedMembershipId,
        Guid expectedOrganizationId,
        string[] expectedPermissions)
    {
        Assert.AreEqual(Guid.Parse(expectedMembershipId), actual.MembershipId);
        Assert.AreEqual(expectedOrganizationId, actual.OrganizationId);
        Assert.AreEqual("active", actual.Status);
        Assert.AreEqual(1, actual.SecurityVersion);
        CollectionAssert.AreEqual(expectedPermissions, actual.Permissions.ToArray());
    }

    private static async Task AssertActorCountAsync(
        NpgsqlDataSource dataSource,
        Guid actorUserId,
        long expectedCount,
        bool commit)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actorUserId);
        Assert.AreEqual(expectedCount, await CountMembershipsAsync(connection, transaction));

        if (commit)
        {
            await transaction.CommitAsync();
        }
        else
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task AssertNoActorContextAsync(NpgsqlDataSource dataSource)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from identity_spike.membership";
        Assert.AreEqual(
            0L,
            Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture));
    }

    private static async Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actorUserId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "select set_config('app.current_actor_id', @actor_user_id, true)";
        command.Parameters.AddWithValue("actor_user_id", actorUserId.ToString("D"));
        await command.ExecuteScalarAsync();
    }

    private static async Task<long> CountMembershipsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "select count(*) from identity_spike.membership";
        return Convert.ToInt64(
            await command.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture);
    }

    private static async Task DeleteLimitFixturesAsync(string ownerConnectionString)
    {
        await using var dataSource = NpgsqlDataSource.Create(ownerConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            delete from identity_spike.membership
            where platform_user_id = @actor_user_id;

            delete from identity_spike.organization
            where display_name like 'Membership limit fixture %';

            delete from identity_spike.platform_user
            where id = @actor_user_id;
            """);
        command.Parameters.AddWithValue("actor_user_id", LimitUser);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteUnknownPermissionFixturesAsync(string ownerConnectionString)
    {
        await using var dataSource = NpgsqlDataSource.Create(ownerConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            delete from identity_spike.membership
            where platform_user_id = @actor_user_id;

            delete from identity_spike.organization
            where id = '00000000-0000-0000-0000-000000000098';

            delete from identity_spike.platform_user
            where id = @actor_user_id;
            """);
        command.Parameters.AddWithValue("actor_user_id", UnknownPermissionUser);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnableDiscoveryRoleAttributeAsync(
        string ownerConnectionString,
        string roleAttribute)
    {
        var commandText = roleAttribute switch
        {
            "createdb" => "alter role agro_membership_discovery createdb",
            "createrole" => "alter role agro_membership_discovery createrole",
            "replication" => "alter role agro_membership_discovery replication",
            _ => throw new ArgumentOutOfRangeException(
                nameof(roleAttribute),
                roleAttribute,
                "Unknown test role attribute.")
        };

        await using var dataSource = NpgsqlDataSource.Create(ownerConnectionString);
        await using var command = dataSource.CreateCommand(commandText);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ResetDiscoveryRoleAttributesAsync(string ownerConnectionString)
    {
        await using var dataSource = NpgsqlDataSource.Create(ownerConnectionString);
        await using var command = dataSource.CreateCommand(
            "alter role agro_membership_discovery nocreatedb nocreaterole noreplication");
        await command.ExecuteNonQueryAsync();
    }

    private sealed class SimulatedRequestException : Exception;
}

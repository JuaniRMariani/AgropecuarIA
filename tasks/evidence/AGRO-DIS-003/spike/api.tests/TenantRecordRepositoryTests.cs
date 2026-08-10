using AgropecuarIA.IdentitySpike.Api.Data;
using Npgsql;

namespace AgropecuarIA.IdentitySpike.Tests;

[TestClass]
public sealed class TenantRecordRepositoryTests
{
    private static readonly Guid OrganizationA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid OrganizationB = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid RecordA = Guid.Parse("30000000-0000-0000-0000-00000000000a");
    private static readonly Guid RecordB = Guid.Parse("30000000-0000-0000-0000-00000000000b");
    private static readonly Guid UserOne = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UserTwo = Guid.Parse("10000000-0000-0000-0000-000000000002");

    [TestMethod]
    public async Task ReadRequiresActorsOwnActiveMembershipAndReadPermission()
    {
        await using var dataSource = NpgsqlDataSource.Create(
            TestEnvironment.Require("ConnectionStrings__IdentitySpike"));
        var repository = new TenantRecordRepository(dataSource);

        TenantRecord? allowed = await repository.FindAsync(
            OrganizationA,
            UserOne,
            RecordA,
            default);
        TenantRecord? actorWithoutMembership = await repository.FindAsync(
            OrganizationA,
            UserTwo,
            RecordA,
            default);
        TenantRecord? actorWithoutPermission = await repository.FindAsync(
            OrganizationB,
            UserTwo,
            RecordB,
            default);

        Assert.IsNotNull(allowed);
        Assert.AreEqual(RecordA, allowed.Id);
        Assert.IsNull(actorWithoutMembership);
        Assert.IsNull(actorWithoutPermission);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task RevocationCommittedBeforeRecordStatementReturnsNoData()
    {
        var ownerConnectionString = TestEnvironment.Require("IdentitySpike__OwnerConnectionString");
        await SetUserOneMembershipStateAsync(ownerConnectionString, isActive: false, securityVersion: 2);
        try
        {
            await using var dataSource = NpgsqlDataSource.Create(
                TestEnvironment.Require("ConnectionStrings__IdentitySpike"));
            var repository = new TenantRecordRepository(dataSource);

            TenantRecord? record = await repository.FindAsync(
                OrganizationA,
                UserOne,
                RecordA,
                default);

            Assert.IsNull(record, "A revoked actor received tenant data.");
        }
        finally
        {
            await SetUserOneMembershipStateAsync(ownerConnectionString, isActive: true, securityVersion: 1);
        }
    }

    private static async Task SetUserOneMembershipStateAsync(
        string ownerConnectionString,
        bool isActive,
        int securityVersion)
    {
        await using var dataSource = NpgsqlDataSource.Create(ownerConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            update identity_spike.membership
            set is_active = @is_active,
                security_version = @security_version
            where id = 'a1111111-1111-4111-8111-111111111111'
            """);
        command.Parameters.AddWithValue("is_active", isActive);
        command.Parameters.AddWithValue("security_version", securityVersion);
        Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
    }
}

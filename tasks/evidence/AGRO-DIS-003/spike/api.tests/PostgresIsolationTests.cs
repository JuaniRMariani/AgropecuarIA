using Npgsql;
using System.Globalization;

namespace AgropecuarIA.IdentitySpike.Tests;

[TestClass]
public sealed class PostgresIsolationTests
{
    private static readonly Guid OrganizationA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid OrganizationB = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    [TestMethod]
    public async Task PooledConnectionDoesNotRetainTenantAfterRollbackOrException()
    {
        await using var dataSource = NpgsqlDataSource.Create(TestEnvironment.Require("ConnectionStrings__IdentitySpike"));

        await AssertTenantCountAsync(dataSource, OrganizationA, expectedCount: 1, commit: false);
        await AssertNoContextAsync(dataSource);

        await Assert.ThrowsExactlyAsync<SimulatedRequestException>(async () =>
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await SetTenantAsync(connection, transaction, OrganizationB);
            Assert.AreEqual(1L, await CountRecordsAsync(connection, transaction));
            throw new SimulatedRequestException();
        });

        await AssertNoContextAsync(dataSource);
        await AssertTenantCountAsync(dataSource, OrganizationB, expectedCount: 1, commit: true);
        await AssertNoContextAsync(dataSource);
    }

    [TestMethod]
    public async Task RlsBlocksCrossTenantSelectAndInsertWithCheck()
    {
        await using var dataSource = NpgsqlDataSource.Create(TestEnvironment.Require("ConnectionStrings__IdentitySpike"));
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetTenantAsync(connection, transaction, OrganizationA);

        await using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText =
                "select count(*) from identity_spike.tenant_record where organization_id = @organization_id";
            select.Parameters.AddWithValue("organization_id", OrganizationB);
            Assert.AreEqual(
                0L,
                Convert.ToInt64(await select.ExecuteScalarAsync(), CultureInfo.InvariantCulture));
        }

        var exception = await Assert.ThrowsExactlyAsync<PostgresException>(async () =>
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                insert into identity_spike.tenant_record
                    (organization_id, id, record_name, record_value, created_by_user_id, created_at, version)
                values
                    (@organization_id, @id, 'forbidden', 'forbidden', @user_id, now(), 1)
                """;
            insert.Parameters.AddWithValue("organization_id", OrganizationB);
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("user_id", Guid.Parse("10000000-0000-0000-0000-000000000002"));
            await insert.ExecuteNonQueryAsync();
        });
        Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }

    [TestMethod]
    public async Task JobRoleIsTenantScopedAndFailsClosedWithoutContext()
    {
        await using var dataSource = NpgsqlDataSource.Create(TestEnvironment.Require("IdentitySpike__JobConnectionString"));
        await AssertTenantCountAsync(dataSource, OrganizationB, expectedCount: 1, commit: true);
        await AssertNoContextAsync(dataSource);
    }

    private static async Task AssertTenantCountAsync(
        NpgsqlDataSource dataSource,
        Guid organizationId,
        long expectedCount,
        bool commit)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetTenantAsync(connection, transaction, organizationId);
        Assert.AreEqual(expectedCount, await CountRecordsAsync(connection, transaction));

        if (commit)
        {
            await transaction.CommitAsync();
        }
        else
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task AssertNoContextAsync(NpgsqlDataSource dataSource)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from identity_spike.tenant_record";
        Assert.AreEqual(
            0L,
            Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture));
    }

    private static async Task SetTenantAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid organizationId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "select set_config('app.current_organization_id', @organization_id, true)";
        command.Parameters.AddWithValue("organization_id", organizationId.ToString("D"));
        await command.ExecuteScalarAsync();
    }

    private static async Task<long> CountRecordsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "select count(*) from identity_spike.tenant_record";
        return Convert.ToInt64(
            await command.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture);
    }

    private sealed class SimulatedRequestException : Exception;
}

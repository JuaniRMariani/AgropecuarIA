using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AgropecuarIA.Identity.Tests.Infrastructure;

[TestClass]
public sealed class IdentityTestAssembly
{
    internal static PostgreSqlTestServer? PostgreSql { get; private set; }

    internal static Exception? StartupError { get; private set; }

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        try
        {
            PostgreSql = await PostgreSqlTestServer.StartAsync(CancellationToken.None);
        }
        catch (Exception error)
        {
            StartupError = error;
        }
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (PostgreSql is not null)
        {
            await PostgreSql.DisposeAsync();
        }
    }
}

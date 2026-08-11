using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
public sealed class TerritoryDatabaseTestAssembly
{
    internal static TerritoryDatabasePostgreSqlServer? PostgreSql { get; private set; }

    internal static Exception? StartupError { get; private set; }

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);
        try
        {
            PostgreSql = await TerritoryDatabasePostgreSqlServer.StartAsync(CancellationToken.None);
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

internal sealed class TerritoryDatabasePostgreSqlServer : IAsyncDisposable
{
    private readonly PostgreSqlContainer? _container;
    private readonly string? _localDataDirectory;
    private readonly string? _localPgCtl;

    private TerritoryDatabasePostgreSqlServer(
        string adminConnectionString,
        PostgreSqlContainer? container = null,
        string? localDataDirectory = null,
        string? localPgCtl = null)
    {
        AdminConnectionString = adminConnectionString;
        _container = container;
        _localDataDirectory = localDataDirectory;
        _localPgCtl = localPgCtl;
    }

    public string AdminConnectionString { get; }

    public static async Task<TerritoryDatabasePostgreSqlServer> StartAsync(
        CancellationToken cancellationToken)
    {
        string? supplied = Environment.GetEnvironmentVariable(
            "AGRO_TERRITORY_TEST_ADMIN_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            await VerifyConnectionAsync(supplied, cancellationToken);
            return new TerritoryDatabasePostgreSqlServer(supplied);
        }

        Exception? containerError = null;
        if (CanAttemptDocker())
        {
            PostgreSqlContainer? container = null;
            try
            {
                container = new PostgreSqlBuilder("postgres:17-alpine")
                    .WithDatabase("postgres")
                    .WithUsername("postgres")
                    .WithPassword($"territory-tests-{Guid.NewGuid():N}")
                    .Build();
                await container.StartAsync(cancellationToken);
                return new TerritoryDatabasePostgreSqlServer(
                    container.GetConnectionString(),
                    container);
            }
            catch (Exception error)
            {
                containerError = error;
                if (container is not null)
                {
                    await container.DisposeAsync();
                }
            }
        }

        return await StartLocalAsync(
            containerError ?? new InvalidOperationException("Docker CLI was not found on PATH."),
            cancellationToken);
    }

    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        string databaseName = $"agro_territory_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {databaseName}";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = databaseName,
            Pooling = false,
        }.ConnectionString;
    }

    public async Task DropDatabaseAsync(
        string databaseConnectionString,
        CancellationToken cancellationToken)
    {
        string? databaseName = new NpgsqlConnectionStringBuilder(databaseConnectionString).Database;
        if (string.IsNullOrWhiteSpace(databaseName)
            || !databaseName.StartsWith("agro_territory_", StringComparison.Ordinal)
            || databaseName.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character == '_')))
        {
            throw new InvalidOperationException("Refusing to drop a database not created by this test run.");
        }

        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE {databaseName} WITH (FORCE)";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_localDataDirectory is null || _localPgCtl is null)
        {
            return;
        }

        await RunProcessAsync(
            _localPgCtl,
            $"stop -D \"{_localDataDirectory}\" -m fast -w",
            throwOnFailure: false,
            CancellationToken.None);

        string resolved = Path.GetFullPath(_localDataDirectory);
        string allowedRoot = Path.GetFullPath(
            Path.Combine(Path.GetTempPath(), "AgropecuarIA.Territory.Tests"));
        if (!resolved.StartsWith(
            allowedRoot + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Refusing to remove a PostgreSQL directory outside the test root.");
        }

        Directory.Delete(resolved, recursive: true);
    }

    private static async Task<TerritoryDatabasePostgreSqlServer> StartLocalAsync(
        Exception containerError,
        CancellationToken cancellationToken)
    {
        string? binaryDirectory = FindPostgresBinaryDirectory();
        if (binaryDirectory is null)
        {
            throw new InvalidOperationException(
                "Docker could not start and no local PostgreSQL binaries were found. " +
                "Set AGRO_TERRITORY_POSTGRES_BIN or " +
                "AGRO_TERRITORY_TEST_ADMIN_CONNECTION_STRING.",
                containerError);
        }

        string root = Path.Combine(Path.GetTempPath(), "AgropecuarIA.Territory.Tests");
        Directory.CreateDirectory(root);
        string dataDirectory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDirectory);
        string initDb = Path.Combine(
            binaryDirectory,
            OperatingSystem.IsWindows() ? "initdb.exe" : "initdb");
        string pgCtl = Path.Combine(
            binaryDirectory,
            OperatingSystem.IsWindows() ? "pg_ctl.exe" : "pg_ctl");
        int port = GetAvailableTcpPort();

        try
        {
            await RunProcessAsync(
                initDb,
                $"-D \"{dataDirectory}\" -A trust -U postgres --encoding=UTF8 --no-locale",
                throwOnFailure: true,
                cancellationToken);
            await RunProcessAsync(
                pgCtl,
                $"start -D \"{dataDirectory}\" -l \"{Path.Combine(dataDirectory, "postgres.log")}\" " +
                $"-o \"-h 127.0.0.1 -p {port}\" -w",
                throwOnFailure: true,
                cancellationToken);
        }
        catch
        {
            if (Directory.Exists(dataDirectory))
            {
                Directory.Delete(dataDirectory, recursive: true);
            }

            throw;
        }

        string connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            Database = "postgres",
            Username = "postgres",
            Pooling = false,
            Timeout = 5,
        }.ConnectionString;
        await VerifyConnectionAsync(connectionString, cancellationToken);
        return new TerritoryDatabasePostgreSqlServer(
            connectionString,
            localDataDirectory: dataDirectory,
            localPgCtl: pgCtl);
    }

    private static string? FindPostgresBinaryDirectory()
    {
        string? configured = Environment.GetEnvironmentVariable("AGRO_TERRITORY_POSTGRES_BIN");
        if (!string.IsNullOrWhiteSpace(configured)
            && File.Exists(Path.Combine(
                configured,
                OperatingSystem.IsWindows() ? "initdb.exe" : "initdb")))
        {
            return configured;
        }

        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        const string installationRoot = @"C:\Program Files\PostgreSQL";
        return Directory.Exists(installationRoot)
            ? Directory.EnumerateDirectories(installationRoot)
                .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => Path.Combine(path, "bin"))
                .FirstOrDefault(path => File.Exists(Path.Combine(path, "initdb.exe")))
            : null;
    }

    private static bool CanAttemptDocker()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return true;
        }

        string executable = OperatingSystem.IsWindows() ? "docker.exe" : "docker";
        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(path => File.Exists(Path.Combine(path, executable)));
    }

    private static int GetAvailableTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task VerifyConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
    }

    private static async Task RunProcessAsync(
        string fileName,
        string arguments,
        bool throwOnFailure,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
            },
        };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        if (throwOnFailure && process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(fileName)} exited with {process.ExitCode}.");
        }
    }
}

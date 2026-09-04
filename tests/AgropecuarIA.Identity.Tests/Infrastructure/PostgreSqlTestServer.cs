using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AgropecuarIA.Identity.Tests.Infrastructure;

internal sealed class PostgreSqlTestServer : IAsyncDisposable
{
    private readonly PostgreSqlContainer? _container;
    private readonly string? _localDataDirectory;
    private readonly string? _localPgCtl;

    private PostgreSqlTestServer(
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

    public static async Task<PostgreSqlTestServer> StartAsync(CancellationToken cancellationToken)
    {
        var suppliedConnectionString = Environment.GetEnvironmentVariable(
            "AGRO_IDENTITY_TEST_ADMIN_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(suppliedConnectionString))
        {
            await VerifyConnectionAsync(suppliedConnectionString, cancellationToken);
            return new PostgreSqlTestServer(suppliedConnectionString);
        }

        Exception? containerError = null;
        bool explicitLocalRuntime = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AGRO_IDENTITY_POSTGRES_BIN"));
        if (!explicitLocalRuntime && CanAttemptDocker())
        {
            PostgreSqlContainer? container = null;
            try
            {
                container = new PostgreSqlBuilder(PostgisEnabled ? "postgis/postgis:17-3.6-alpine" : "postgres:17-alpine")
                    .WithDatabase("postgres")
                    .WithUsername("postgres")
                    .WithPassword($"identity-tests-{Guid.NewGuid():N}")
                    .Build();
                await container.StartAsync(cancellationToken);
                return new PostgreSqlTestServer(container.GetConnectionString(), container);
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
        var databaseName = $"agro_identity_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {databaseName}";
        await command.ExecuteNonQueryAsync(cancellationToken);

        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = databaseName,
            Pooling = false,
        };
        if (PostgisEnabled)
        {
            try
            {
                await using var spatialConnection = new NpgsqlConnection(builder.ConnectionString);
                await spatialConnection.OpenAsync(cancellationToken);
                await using var extension = spatialConnection.CreateCommand();
                // This hook operates only on the fresh database generated above, never the admin database.
                extension.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis WITH SCHEMA public; SELECT public.postgis_lib_version()";
                string? version = await extension.ExecuteScalarAsync(cancellationToken) as string;
                if (version is null || !version.StartsWith("3.6.", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("PostGIS 3.6.x is required for the spatial regression fixture.");
                }
            }
            catch (Exception exception)
            {
                await DropDatabaseAsync(builder.ConnectionString, CancellationToken.None);
                throw new InvalidOperationException(
                    "AGRO_TEST_POSTGIS=true requires PostGIS 3.6.x. Set AGRO_IDENTITY_POSTGRES_BIN to the pinned PG17/PostGIS runtime or use the PostGIS test container.", exception);
            }
        }

        return builder.ConnectionString;
    }

    private static bool PostgisEnabled => string.Equals(
        Environment.GetEnvironmentVariable("AGRO_TEST_POSTGIS"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task DropDatabaseAsync(string databaseConnectionString, CancellationToken cancellationToken)
    {
        var databaseName = new NpgsqlConnectionStringBuilder(databaseConnectionString).Database;
        if (string.IsNullOrWhiteSpace(databaseName)
            || !databaseName.StartsWith("agro_identity_", StringComparison.Ordinal)
            || databaseName.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '_')))
        {
            throw new InvalidOperationException("Refusing to drop a database not created by this test run.");
        }

        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
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

        var resolvedDataDirectory = Path.GetFullPath(_localDataDirectory);
        var allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "AgropecuarIA.Identity.Tests"));
        if (!resolvedDataDirectory.StartsWith(allowedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to remove a PostgreSQL directory outside the test root.");
        }

        Directory.Delete(resolvedDataDirectory, recursive: true);
    }

    private static async Task<PostgreSqlTestServer> StartLocalAsync(
        Exception containerError,
        CancellationToken cancellationToken)
    {
        var postgresBinaryDirectory = FindPostgresBinaryDirectory();
        if (postgresBinaryDirectory is null)
        {
            throw new InvalidOperationException(
                "Docker could not start and no local PostgreSQL binaries were found. "
                + "Install Docker, set AGRO_IDENTITY_POSTGRES_BIN, or provide "
                + "AGRO_IDENTITY_TEST_ADMIN_CONNECTION_STRING.",
                containerError);
        }

        var root = Path.Combine(Path.GetTempPath(), "AgropecuarIA.Identity.Tests");
        Directory.CreateDirectory(root);
        var dataDirectory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDirectory);

        var initDb = Path.Combine(postgresBinaryDirectory, OperatingSystem.IsWindows() ? "initdb.exe" : "initdb");
        var pgCtl = Path.Combine(postgresBinaryDirectory, OperatingSystem.IsWindows() ? "pg_ctl.exe" : "pg_ctl");
        var port = GetAvailableTcpPort();

        try
        {
            await RunProcessAsync(
                initDb,
                $"-D \"{dataDirectory}\" -A trust -U postgres --encoding=UTF8 --no-locale",
                throwOnFailure: true,
                cancellationToken);
            await RunProcessAsync(
                pgCtl,
                $"start -D \"{dataDirectory}\" -l \"{Path.Combine(dataDirectory, "postgres.log")}\" "
                    + $"-o \"-h 127.0.0.1 -p {port}\" -w",
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

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            Database = "postgres",
            Username = "postgres",
            Pooling = false,
            Timeout = 5,
        }.ConnectionString;
        await VerifyConnectionAsync(connectionString, cancellationToken);

        return new PostgreSqlTestServer(
            connectionString,
            localDataDirectory: dataDirectory,
            localPgCtl: pgCtl);
    }

    private static string? FindPostgresBinaryDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("AGRO_IDENTITY_POSTGRES_BIN");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            string resolved = Path.GetFullPath(configured);
            string executableSuffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
            if (!File.Exists(Path.Combine(resolved, "initdb" + executableSuffix)) ||
                !File.Exists(Path.Combine(resolved, "pg_ctl" + executableSuffix)) ||
                !File.Exists(Path.Combine(resolved, "postgres" + executableSuffix)))
            {
                throw new InvalidOperationException("AGRO_IDENTITY_POSTGRES_BIN must identify a complete PostgreSQL binary directory; no fallback is used for an explicit path.");
            }

            return resolved;
        }

        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        const string installationRoot = @"C:\Program Files\PostgreSQL";
        if (!Directory.Exists(installationRoot))
        {
            return null;
        }

        return Directory.EnumerateDirectories(installationRoot)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => Path.Combine(path, "bin"))
            .FirstOrDefault(path => File.Exists(Path.Combine(path, "initdb.exe")));
    }

    private static bool CanAttemptDocker()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return true;
        }

        var executableName = OperatingSystem.IsWindows() ? "docker.exe" : "docker";
        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(path => File.Exists(Path.Combine(path, executableName)));
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

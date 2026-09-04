using System.Text;
using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Catalog.Tests;

internal sealed class CatalogDatabaseScenario(string connectionString) : IAsyncDisposable
{
    public string ConnectionString { get; } = connectionString;
    public static CatalogEditorialContext Editor { get; } = new(Guid.Parse("f1900f37-95ea-48f3-8ffc-e473927d2904"),
        Guid.Parse("f76c4a77-3e52-48f8-9e83-257b41e7d012"), "catalog-postgres-proof");

    public static async Task<CatalogDatabaseScenario> CreateAsync(string? targetMigration = null)
    {
        PostgreSqlTestServer server = IdentityTestAssembly.PostgreSql ?? throw new InvalidOperationException("PostgreSQL fixture unavailable.", IdentityTestAssembly.StartupError);
        var scenario = new CatalogDatabaseScenario(await server.CreateDatabaseAsync(CancellationToken.None));
        try
        {
            await using CatalogDbContext context = scenario.OpenContext();
            await context.GetService<IMigrator>().MigrateAsync(targetMigration);
            return scenario;
        }
        catch { await scenario.DisposeAsync(); throw; }
    }

    public CatalogDbContext OpenContext() => new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(ConnectionString,
        options => options.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")).Options);

    public async Task<bool> IngestAsync(string source, string json)
    {
        await using CatalogDbContext context = OpenContext();
        return await new CatalogIngestionApplicationService(context).IngestAsync(new(source, Convert.ToBase64String(Encoding.UTF8.GetBytes(json))), Editor, CancellationToken.None);
    }

    public async Task<CatalogEditorialDiffResult> DiffAsync()
    {
        await using CatalogDbContext context = OpenContext();
        return await new CatalogDiffApplicationService(context).GenerateDiffAsync(CancellationToken.None);
    }

    public async Task<CatalogPublishResult> PublishAsync(string tag, string? hash = null)
    {
        hash ??= (await DiffAsync()).CandidateHash;
        await using CatalogDbContext context = OpenContext();
        return await new CatalogPublicationApplicationService(context).PublishAsync(new(tag, hash), Editor, CancellationToken.None);
    }

    public async Task<bool> RollbackAsync(Guid id)
    {
        await using CatalogDbContext context = OpenContext();
        return await new CatalogPublicationApplicationService(context).RollbackAsync(new(id), Editor, CancellationToken.None);
    }

    public async Task<CatalogSearchResult> SearchAsync(SearchCatalogQuery? query = null)
    {
        await using CatalogDbContext context = OpenContext();
        return await new CatalogSearchApplicationService(context).SearchAsync(query ?? new(), CancellationToken.None);
    }

    public async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<long[]> CountsAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT (SELECT count(*) FROM catalog.catalog_source_snapshots),(SELECT count(*) FROM catalog.catalog_staging_entries),
                (SELECT count(*) FROM catalog.catalog_published_versions),(SELECT count(*) FROM catalog.catalog_published_items),
                (SELECT count(*) FROM catalog.catalog_published_sources),(SELECT count(*) FROM catalog.catalog_editorial_audits),
                (SELECT count(*) FROM catalog.catalog_outbox_messages),(SELECT count(*) FROM catalog.catalog_published_versions WHERE "IsActive")
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return Enumerable.Range(0, 8).Select(reader.GetInt64).ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        if (IdentityTestAssembly.PostgreSql is { } server) await server.DropDatabaseAsync(ConnectionString, CancellationToken.None);
    }
}

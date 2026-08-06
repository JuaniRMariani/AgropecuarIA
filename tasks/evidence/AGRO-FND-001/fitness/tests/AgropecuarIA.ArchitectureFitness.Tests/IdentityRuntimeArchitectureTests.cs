using AgropecuarIA.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class IdentityRuntimeArchitectureTests
{
    private static readonly string[] CanonicalOutboxFields =
    [
        "Source",
        "SchemaVersion",
        "ScopeKind",
        "TenantId",
        "EffectiveAtUtc",
        "RecordedAtUtc",
        "ActorId",
        "CorrelationId",
        "CausationId",
        "AggregateType",
        "AggregateVersion",
    ];

    [TestMethod]
    public void IdentityEfModelOwnsOnlyIdentitySchemaAndCanonicalJournalAndOutbox()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=architecture_fitness;Username=unused;Password=unused")
            .Options;
        using var dbContext = new IdentityDbContext(options);
        IEntityType[] entityTypes = dbContext.Model.GetEntityTypes().ToArray();

        Assert.IsGreaterThan(0, entityTypes.Length);
        Assert.IsTrue(
            entityTypes.All(entity => string.Equals(entity.GetSchema(), "identity", StringComparison.Ordinal)),
            string.Join(
                Environment.NewLine,
                entityTypes.Select(entity => $"{entity.ClrType.Name}: {entity.GetSchema()}.{entity.GetTableName()}")));

        IEntityType journal = entityTypes.Single(
            entity => entity.ClrType.Name == "IdentitySecurityJournalEntry");
        Assert.AreEqual("identity", journal.GetSchema());
        Assert.AreEqual(
            "audit_events",
            journal.GetTableName(),
            "The N/N-1 expansion keeps the legacy physical journal table until the contract migration.");

        IEntityType outbox = entityTypes.Single(entity => entity.GetTableName() == "outbox_messages");
        string[] actualFields = outbox.GetProperties().Select(property => property.Name).ToArray();
        foreach (string field in CanonicalOutboxFields)
        {
            Assert.Contains(field, actualFields, $"Outbox is missing canonical field '{field}'.");
        }
    }
}

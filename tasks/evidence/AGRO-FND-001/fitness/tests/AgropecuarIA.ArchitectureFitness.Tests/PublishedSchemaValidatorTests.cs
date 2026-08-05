using System.Text.Json.Nodes;
using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class PublishedSchemaValidatorTests
{
    [TestMethod]
    public void PublishedSchemasAreRegisteredClosedAndSemanticallyComplete()
    {
        var issues = PublishedSchemaValidator.ValidateDirectory(EvidenceFixture.ContractsDirectory());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void ProblemSchemaCannotExposeSensitiveOrArbitraryFields()
    {
        var schema = Schema("problem-details.v1.schema.json");
        schema["additionalProperties"] = true;
        schema["properties"]!["tenantId"] = new JsonObject { ["type"] = "string" };

        var issues = PublishedSchemaValidator.Validate("problem-details.v1.schema.json", schema.ToJsonString());

        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.root.open"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.sensitive-property"));
    }

    [TestMethod]
    public void EventEnvelopeRequiresScopeAndSequenceFields()
    {
        var schema = Schema("event-envelope.v1.schema.json");
        var required = schema["required"]!.AsArray();
        var aggregateVersion = required.Single(node => node?.GetValue<string>() == "aggregateVersion");
        _ = required.Remove(aggregateVersion);
        schema["properties"]!["scope"]!["$ref"] = "ambiguous-scope.json";

        var issues = PublishedSchemaValidator.Validate("event-envelope.v1.schema.json", schema.ToJsonString());

        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.required.missing"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.event.scope-invalid"));
    }

    [TestMethod]
    public void RequestScopeMustDiscriminatePlatformFromTenant()
    {
        var schema = Schema("request-scope.v1.schema.json");
        schema["oneOf"]!.AsArray()[1]!["properties"]!["kind"]!["const"] = "platform";

        var issues = PublishedSchemaValidator.Validate("request-scope.v1.schema.json", schema.ToJsonString());

        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.scope.kinds-invalid"));
    }

    private static JsonObject Schema(string fileName) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(EvidenceFixture.ContractsDirectory(), fileName)))!.AsObject();
}

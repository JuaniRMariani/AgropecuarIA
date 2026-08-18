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

    [TestMethod]
    [DataRow("identity-linked.v1.schema.json", "linkedAtUtc")]
    [DataRow("identity-linked.v1.schema.json", "userId")]
    [DataRow("identity-step-up-completed.v1.schema.json", "completedAtUtc")]
    [DataRow("identity-step-up-completed.v1.schema.json", "previousSessionId")]
    [DataRow("organization-created.v1.schema.json", "organizationId")]
    [DataRow("organization-created.v1.schema.json", "ownerMembershipId")]
    [DataRow("organization-created.v1.schema.json", "createdAtUtc")]
    [DataRow("organization-owner-invited.v1.schema.json", "expiresAtUtc")]
    [DataRow("organization-owner-invited.v1.schema.json", "invitationId")]
    [DataRow("organization-owner-invitation-accepted.v1.schema.json", "membershipId")]
    [DataRow("organization-owner-invitation-accepted.v1.schema.json", "acceptedAtUtc")]
    [DataRow("organization-owner-invitation-revoked.v1.schema.json", "revokedAtUtc")]
    [DataRow("organization-owner-membership-removed.v1.schema.json", "organizationId")]
    [DataRow("organization-owner-membership-removed.v1.schema.json", "membershipId")]
    [DataRow("organization-owner-membership-removed.v1.schema.json", "removedAtUtc")]
    public void IdentityEventPayloadSchemasRejectMissingExtraAndWronglyTypedFields(
        string fileName,
        string requiredProperty)
    {
        var schema = Schema(fileName);
        var required = schema["required"]!.AsArray();
        _ = required.Remove(required.Single(node => node?.GetValue<string>() == requiredProperty));
        schema["properties"]!["unexpected"] = new JsonObject { ["type"] = "string" };
        schema["properties"]![requiredProperty]!["type"] = "integer";

        var issues = PublishedSchemaValidator.Validate(fileName, schema.ToJsonString());

        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.required.missing"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.properties.invalid"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.property-shape.invalid"));
    }

    [TestMethod]
    [DataRow("authorizationVersion")]
    [DataRow("revokedInvitationCount")]
    public void OwnerMembershipRemovalSchemaRequiresBoundedIntegers(string propertyName)
    {
        const string FileName = "organization-owner-membership-removed.v1.schema.json";
        var schema = Schema(FileName);
        schema["properties"]![propertyName]!["type"] = "string";
        schema["properties"]![propertyName]!["minimum"] = -1;

        var issues = PublishedSchemaValidator.Validate(FileName, schema.ToJsonString());

        Assert.IsTrue(issues.Any(issue => issue.Code == "schema.property-shape.invalid"));
    }

    private static JsonObject Schema(string fileName) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(EvidenceFixture.ContractsDirectory(), fileName)))!.AsObject();
}

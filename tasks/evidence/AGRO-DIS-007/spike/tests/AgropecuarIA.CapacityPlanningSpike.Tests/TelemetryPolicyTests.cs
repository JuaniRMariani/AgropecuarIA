using AgropecuarIA.CapacityPlanningSpike;

namespace AgropecuarIA.CapacityPlanningSpike.Tests;

[TestClass]
public sealed class TelemetryPolicyTests
{
    private static readonly string[] ExpectedAttributeNames =
    [
        "route_template",
        "http.request.method",
        "status_class",
        "dependency",
        "job",
        "cache",
        "deployment.environment",
        "result",
    ];

    [TestMethod]
    public void SanitizeKeepsOnlyBoundedOperationalDimensions()
    {
        var attributes = new Dictionary<string, string>
        {
            ["route_template"] = "/capacity/{scenarioId}",
            ["http.request.method"] = "GET",
            ["status_class"] = "2xx",
            ["dependency"] = "postgresql",
            ["job"] = "capacity-report",
            ["cache"] = "miss",
            ["deployment.environment"] = "local",
            ["result"] = "success",
            ["tenant_id"] = "secret-tenant",
            ["user_id"] = "secret-user",
            ["url.path"] = "/capacity/sensitive-id",
            ["url.query"] = "email=persona@example.test",
            ["request.payload"] = "sensitive payload",
            ["idempotency_key"] = "secret-key",
        };

        var sanitized = TelemetryPolicy.Sanitize(attributes);

        CollectionAssert.AreEquivalent(ExpectedAttributeNames, sanitized.Keys.ToArray());
    }

    [TestMethod]
    public void SanitizeTenThousandSensitiveValuesDoNotChangeTagsOrCardinality()
    {
        var baseline = new Dictionary<string, string>
        {
            ["route_template"] = "/capacity/{scenarioId}",
            ["http.request.method"] = "POST",
            ["status_class"] = "2xx",
            ["deployment.environment"] = "test",
            ["result"] = "success",
        };
        var distinctOutputs = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < 10_000; index++)
        {
            var attributes = baseline
                .Append(new KeyValuePair<string, string>("tenant_id", $"tenant-{index}"))
                .Append(new KeyValuePair<string, string>("email", $"person-{index}@example.test"))
                .Append(new KeyValuePair<string, string>("url.path", $"/capacity/{index}"))
                .Append(new KeyValuePair<string, string>("http.response.status_code", index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var sanitized = TelemetryPolicy.Sanitize(attributes);
            var fingerprint = string.Join(
                "|",
                sanitized.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => $"{pair.Key}={pair.Value}"));
            distinctOutputs.Add(fingerprint);
        }

        Assert.HasCount(1, distinctOutputs);
    }

    [TestMethod]
    public void SanitizeInvalidAllowedValueReturnsTypedValidationError()
    {
        var invalidStatusClass = new Dictionary<string, string>
        {
            ["status_class"] = "tenant-123",
        };

        var exception = Assert.ThrowsExactly<CapacityPlanningException>(
            () => TelemetryPolicy.Sanitize(invalidStatusClass));

        Assert.AreEqual(CapacityPlanningErrorCode.InvalidInput, exception.Code);
    }

    [TestMethod]
    public void SanitizeTenThousandDynamicRouteSlugsRejectsEverySeries()
    {
        var acceptedSeries = 0;

        for (var index = 0; index < 10_000; index++)
        {
            var attributes = new Dictionary<string, string>
            {
                ["route_template"] = $"/capacity/tenant-{index}",
                ["http.request.method"] = "GET",
                ["status_class"] = "2xx",
                ["result"] = "success",
            };

            try
            {
                _ = TelemetryPolicy.Sanitize(attributes);
                acceptedSeries++;
            }
            catch (CapacityPlanningException exception)
            {
                Assert.AreEqual(CapacityPlanningErrorCode.InvalidInput, exception.Code);
            }
        }

        Assert.AreEqual(0, acceptedSeries);
    }
}

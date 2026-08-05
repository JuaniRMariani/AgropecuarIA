using System.Text;

namespace AgropecuarIA.GisWeatherSpike.Tests;

[TestClass]
public sealed class CapParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 16, 40, 0, TimeSpan.Zero);
    [TestMethod]
    public void ParseRealSmnFixturePreservesProvenanceAndConvertsCoordinateOrder()
    {
        using var payload = FixtureFiles.Open(Path.Combine("cap", "smn-real-viento-update.xml"));

        var result = CapParser.Parse(payload, Now);

        Assert.IsTrue(result.IsSuccess, result.Error?.SafeMessage);
        var alert = result.Value ?? throw new AssertFailedException("CAP alert is required.");
        Assert.AreEqual("smn@smn.gob.ar", alert.Sender);
        Assert.AreEqual(CapMessageType.Update, alert.MessageType);
        Assert.AreEqual(CapLifecycleState.Updated, alert.LifecycleState);
        Assert.AreEqual(64, alert.SourceHash.Length);
        Assert.IsNotEmpty(alert.Limitations);
        var first = alert.Areas[0].Polygons[0][0];
        Assert.AreEqual(-60.46, first.Longitude, 0.000001);
        Assert.AreEqual(-33.6, first.Latitude, 0.000001);
    }

    [TestMethod]
    public void ParseExpiredFixtureIsNeverReportedActive()
    {
        using var payload = FixtureFiles.Open(Path.Combine("cap", "synthetic-expired.xml"));

        var result = CapParser.Parse(payload, Now);

        Assert.AreEqual(CapLifecycleState.Expired, result.Value?.LifecycleState);
    }

    [TestMethod]
    public void LifecycleTrackerAppliesAlertUpdateCancelInOrder()
    {
        var tracker = new CapLifecycleTracker();
        var alert = ParseFixture("synthetic-alert.xml");
        var update = ParseFixture("synthetic-update.xml");
        var cancel = ParseFixture("synthetic-cancel.xml");

        Assert.IsTrue(tracker.Apply(alert).IsSuccess);
        Assert.IsTrue(tracker.Apply(update).IsSuccess);
        var final = tracker.Apply(cancel);

        Assert.IsTrue(final.IsSuccess, final.Error?.SafeMessage);
        Assert.AreEqual(CapLifecycleState.Cancelled, final.Value?.LifecycleState);
    }

    [TestMethod]
    public void LifecycleTrackerRejectsUnknownOrOutOfOrderReference()
    {
        var tracker = new CapLifecycleTracker();
        var update = ParseFixture("synthetic-update.xml");

        var result = tracker.Apply(update);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ProviderErrorCode.OutOfOrder, result.Error?.Code);
    }

    [TestMethod]
    public void LifecycleTrackerPreservesOriginalIdentityForLaterCancellation()
    {
        var tracker = new CapLifecycleTracker();
        var alert = ParseFixture("synthetic-alert.xml");
        var update = ParseFixture("synthetic-update.xml");
        var cancel = ParseFixture("synthetic-cancel.xml") with
        {
            References = ["fixtures@agropecuaria.invalid,synthetic-alert-001,2026-08-05T10:00:00-03:00"],
        };

        Assert.IsTrue(tracker.Apply(alert).IsSuccess);
        Assert.IsTrue(tracker.Apply(update).IsSuccess);

        var result = tracker.Apply(cancel);

        Assert.IsTrue(result.IsSuccess, result.Error?.SafeMessage);
        Assert.AreEqual(CapLifecycleState.Cancelled, result.Value?.LifecycleState);
    }

    [TestMethod]
    public void LifecycleTrackerRejectsSpoofedSenderReferencingAnotherAuthority()
    {
        var tracker = new CapLifecycleTracker();
        var alert = ParseFixture("synthetic-alert.xml");
        var spoofed = ParseFixture("synthetic-update.xml") with
        {
            Sender = "attacker@invalid.example",
        };
        Assert.IsTrue(tracker.Apply(alert).IsSuccess);

        var result = tracker.Apply(spoofed);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ProviderErrorCode.OutOfOrder, result.Error?.Code);
    }

    [TestMethod]
    public void LifecycleTrackerRejectsLateUpdateThatReferencesTheOriginalAlert()
    {
        var tracker = new CapLifecycleTracker();
        var alert = ParseFixture("synthetic-alert.xml");
        var update = ParseFixture("synthetic-update.xml");
        var lateUpdate = update with
        {
            Identifier = "synthetic-update-late",
            Sent = alert.Sent.AddMinutes(30),
            References = ["fixtures@agropecuaria.invalid,synthetic-alert-001,2026-08-05T10:00:00-03:00"],
        };

        Assert.IsTrue(tracker.Apply(alert).IsSuccess);
        Assert.IsTrue(tracker.Apply(update).IsSuccess);

        var result = tracker.Apply(lateUpdate);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ProviderErrorCode.OutOfOrder, result.Error?.Code);
    }

    [TestMethod]
    public void LifecycleTrackerRejectsUpdateAfterCancellation()
    {
        var tracker = new CapLifecycleTracker();
        var alert = ParseFixture("synthetic-alert.xml");
        var update = ParseFixture("synthetic-update.xml");
        var cancel = ParseFixture("synthetic-cancel.xml");
        var postCancelUpdate = update with
        {
            Identifier = "synthetic-update-after-cancel",
            Sent = cancel.Sent.AddMinutes(30),
            References = ["fixtures@agropecuaria.invalid,synthetic-alert-001,2026-08-05T10:00:00-03:00"],
        };

        Assert.IsTrue(tracker.Apply(alert).IsSuccess);
        Assert.IsTrue(tracker.Apply(update).IsSuccess);
        Assert.IsTrue(tracker.Apply(cancel).IsSuccess);

        var result = tracker.Apply(postCancelUpdate);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ProviderErrorCode.OutOfOrder, result.Error?.Code);
    }

    [TestMethod]
    public void ParseXxeFixtureFailsClosedWithoutResolvingEntity()
    {
        using var payload = FixtureFiles.Open(Path.Combine("cap", "xxe.xml"));

        var result = CapParser.Parse(payload, Now);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ProviderErrorCode.SchemaInvalid, result.Error?.Code);
        Assert.IsNull(result.Value);
    }

    [TestMethod]
    public void ParsePayloadOverTwoMiBIsRejectedBeforeXmlParsing()
    {
        using var payload = new MemoryStream(new byte[CapParser.MaximumPayloadBytes + 1]);

        var result = CapParser.Parse(payload, Now);

        Assert.AreEqual(ProviderErrorCode.PayloadTooLarge, result.Error?.Code);
    }

    [TestMethod]
    public void ParseUnclosedPolygonIsSchemaInvalid()
    {
        const string xml = """
            <alert xmlns="urn:oasis:names:tc:emergency:cap:1.2">
              <identifier>bad-ring</identifier><sender>fixtures@agropecuaria.invalid</sender>
              <sent>2026-08-05T10:00:00-03:00</sent><status>Test</status><msgType>Alert</msgType><scope>Public</scope>
              <info><effective>2026-08-05T10:00:00-03:00</effective><expires>2026-08-05T18:00:00-03:00</expires>
                <area><areaDesc>Bad ring</areaDesc><polygon>-34,-59 -34,-58 -35,-58 -35,-59</polygon></area>
              </info>
            </alert>
            """;
        using var payload = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var result = CapParser.Parse(payload, Now);

        Assert.AreEqual(ProviderErrorCode.SchemaInvalid, result.Error?.Code);
    }

    [TestMethod]
    public void ParseTimestampWithoutExplicitOffsetIsSchemaInvalid()
    {
        const string xml = """
            <alert xmlns="urn:oasis:names:tc:emergency:cap:1.2">
              <identifier>missing-offset</identifier><sender>fixtures@agropecuaria.invalid</sender>
              <sent>2026-08-05T10:00:00</sent><status>Test</status><msgType>Alert</msgType><scope>Public</scope>
              <info><effective>2026-08-05T10:00:00-03:00</effective><expires>2026-08-05T18:00:00-03:00</expires>
                <area><areaDesc>Explicit offset test</areaDesc><polygon>-34,-59 -34,-58 -35,-58 -34,-59</polygon></area>
              </info>
            </alert>
            """;
        using var payload = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var result = CapParser.Parse(payload, Now);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ProviderErrorCode.SchemaInvalid, result.Error?.Code);
    }

    private static CapAlert ParseFixture(string fileName)
    {
        using var payload = FixtureFiles.Open(Path.Combine("cap", fileName));
        var result = CapParser.Parse(payload, Now);
        Assert.IsTrue(result.IsSuccess, result.Error?.SafeMessage);
        return result.Value ?? throw new AssertFailedException("CAP alert is required.");
    }
}

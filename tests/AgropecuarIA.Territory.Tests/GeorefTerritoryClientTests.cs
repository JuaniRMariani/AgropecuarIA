using System.Net;
using System.Text;
using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Providers.Georef;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
public sealed class GeorefTerritoryClientTests
{
    private static readonly DateTimeOffset CapturedAt =
        new(2026, 8, 11, 15, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task ResolveParsesOfficialV2ShapeAndUsesTheFixedRelativeUri()
    {
        CapturingHandler handler = new(_ => JsonResponse(OfficialResponse));
        GeorefTerritoryClient client = CreateClient(handler);

        ProviderTerritoryResolution? result = await client.ResolveAsync(
            -34.6037,
            -58.3816,
            CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("022007", result.Unit.OfficialCode);
        Assert.AreEqual("municipality", result.Unit.Level);
        Assert.AreEqual("02007", result.Unit.ParentCode);
        Assert.AreEqual("Comuna 1, Ciudad Autónoma de Buenos Aires", result.Unit.HierarchyLabel);
        Assert.AreEqual("georef", result.Source.Provider);
        Assert.AreEqual("2.0", result.Source.Version);
        Assert.AreEqual(CapturedAt, result.Source.CapturedAtUtc);
        Assert.AreEqual(
            "https://apis.datos.gob.ar/georef/api/v2.0/ubicacion?lat=-34.6037&lon=-58.3816",
            handler.LastRequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ResolveFallsBackToDepartmentThenProvinceWithoutInventingLocality()
    {
        const string json = """
            {
              "parametros": { "lat": -38.0, "lon": -68.0 },
              "ubicacion": {
                "lat": -38.0,
                "lon": -68.0,
                "provincia": { "id": "58", "nombre": "Neuquén" },
                "departamento": { "id": "58035", "nombre": "Confluencia" },
                "gobierno_local": { "id": null, "nombre": null }
              }
            }
            """;
        GeorefTerritoryClient client = CreateClient(
            new CapturingHandler(_ => JsonResponse(json)));

        ProviderTerritoryResolution? result = await client.ResolveAsync(
            -38,
            -68,
            CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("department", result.Unit.Level);
        Assert.AreEqual("58035", result.Unit.OfficialCode);
        Assert.AreEqual("58", result.Unit.ParentCode);
        Assert.AreEqual("Confluencia, Neuquén", result.Unit.HierarchyLabel);
    }

    [TestMethod]
    public async Task ResolveRejectsUnknownSchemaFields()
    {
        string json = OfficialResponse.Replace(
            "\"ubicacion\": {",
            "\"unexpected\": true, \"ubicacion\": {",
            StringComparison.Ordinal);
        GeorefTerritoryClient client = CreateClient(
            new CapturingHandler(_ => JsonResponse(json)));

        TerritoryProviderException exception =
            await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
                client.ResolveAsync(-34.6037, -58.3816, CancellationToken.None));

        StringAssert.Contains(exception.Message, "invalid JSON");
    }

    [TestMethod]
    public async Task ResolveRejectsTruncatedJsonAndCoordinateEchoMismatch()
    {
        GeorefTerritoryClient truncated = CreateClient(
            new CapturingHandler(_ => JsonResponse("{\"parametros\":{")));
        await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
            truncated.ResolveAsync(-34.6037, -58.3816, CancellationToken.None));

        string mismatched = OfficialResponse.Replace(
            "\"lat\": -34.6037",
            "\"lat\": -31.4167",
            StringComparison.Ordinal);
        GeorefTerritoryClient wrongCoordinate = CreateClient(
            new CapturingHandler(_ => JsonResponse(mismatched)));
        await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
            wrongCoordinate.ResolveAsync(-34.6037, -58.3816, CancellationToken.None));
    }

    [TestMethod]
    public async Task ResolveRejectsHtmlOversizedAndEmptyPayloads()
    {
        HttpResponseMessage html = new(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>unavailable</html>", Encoding.UTF8, "text/html"),
        };
        GeorefTerritoryClient htmlClient = CreateClient(new CapturingHandler(_ => html));
        await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
            htmlClient.ResolveAsync(-34, -58, CancellationToken.None));

        HttpResponseMessage oversized = new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[GeorefTerritoryClient.MaximumResponseBytes + 1]),
        };
        oversized.Content.Headers.ContentType = new("application/json");
        GeorefTerritoryClient oversizedClient = CreateClient(
            new CapturingHandler(_ => oversized));
        await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
            oversizedClient.ResolveAsync(-34, -58, CancellationToken.None));

        HttpResponseMessage empty = new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([]),
        };
        empty.Content.Headers.ContentType = new("application/json");
        GeorefTerritoryClient emptyClient = CreateClient(new CapturingHandler(_ => empty));
        await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
            emptyClient.ResolveAsync(-34, -58, CancellationToken.None));
    }

    [TestMethod]
    public async Task ResolveTreatsRedirectAsUnavailableWithoutFollowingLocation()
    {
        CapturingHandler redirectHandler = new(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("https://attacker.invalid/territory") },
        });
        GeorefTerritoryClient redirectClient = CreateClient(redirectHandler);

        await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
            redirectClient.ResolveAsync(-34, -58, CancellationToken.None));

        Assert.AreEqual(1, redirectHandler.Calls);
        Assert.AreEqual("apis.datos.gob.ar", redirectHandler.LastRequestUri!.Host);
    }

    [DataRow(429)]
    [DataRow(500)]
    [TestMethod]
    public async Task ResolveTreatsProviderHttpFailureAsUnavailable(int statusCode)
    {
        GeorefTerritoryClient failureClient = CreateClient(new CapturingHandler(_ =>
            new HttpResponseMessage((HttpStatusCode)statusCode)));

        await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
            failureClient.ResolveAsync(-34, -58, CancellationToken.None));
    }

    [TestMethod]
    public async Task ResolveNormalizesTimeoutAndRejectsAMisconfiguredHost()
    {
        GeorefTerritoryClient timeoutClient = CreateClient(new CapturingHandler(_ =>
            throw new TaskCanceledException("timeout")));

        TerritoryProviderException timeout =
            await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
                timeoutClient.ResolveAsync(-34, -58, CancellationToken.None));

        StringAssert.Contains(timeout.Message, "timed out");

        HttpClient unsafeHttpClient = new(new CapturingHandler(_ => JsonResponse(OfficialResponse)))
        {
            BaseAddress = new Uri("https://attacker.invalid/"),
        };
        GeorefTerritoryClient unsafeClient = new(
            unsafeHttpClient,
            new FixedTimeProvider(CapturedAt));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            unsafeClient.ResolveAsync(-34, -58, CancellationToken.None));
    }

    [TestMethod]
    public async Task ResolveNormalizesTransportStreamFailures()
    {
        GeorefTerritoryClient client = CreateClient(new ThrowingHandler());

        TerritoryProviderException exception =
            await Assert.ThrowsExactlyAsync<TerritoryProviderException>(() =>
                client.ResolveAsync(-34, -58, CancellationToken.None));

        StringAssert.Contains(exception.Message, "stream failed");
    }

    private static GeorefTerritoryClient CreateClient(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler)
        {
            BaseAddress = GeorefTerritoryClient.ServiceBaseAddress,
            Timeout = GeorefTerritoryClient.RequestTimeout,
        };
        return new GeorefTerritoryClient(httpClient, new FixedTimeProvider(CapturedAt));
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private const string OfficialResponse = """
        {
          "parametros": { "lat": -34.6037, "lon": -58.3816 },
          "ubicacion": {
            "lat": -34.6037,
            "lon": -58.3816,
            "provincia": { "id": "02", "nombre": "Ciudad Autónoma de Buenos Aires" },
            "departamento": { "id": "02007", "nombre": "Comuna 1" },
            "gobierno_local": { "id": "022007", "nombre": "Comuna 1" }
          }
        }
        """;

    private sealed class CapturingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastRequestUri = request.RequestUri;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new IOException("truncated response stream");
    }
}

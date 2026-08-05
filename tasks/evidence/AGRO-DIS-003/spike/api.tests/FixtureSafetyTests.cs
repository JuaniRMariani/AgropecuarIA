using AgropecuarIA.IdentitySpike.Api.Fixtures;
using Microsoft.Extensions.Configuration;

namespace AgropecuarIA.IdentitySpike.Tests;

[TestClass]
public sealed class FixtureSafetyTests
{
    [TestMethod]
    public void ExternalFixtureBindingIsRejected()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["urls"] = "http://0.0.0.0:5080"
            })
            .Build();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => SpikeFixtureSafety.EnsureLoopbackOnly(configuration));
    }

    [TestMethod]
    public void ExplicitLoopbackFixtureBindingsAreAccepted()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["urls"] = "http://127.0.0.1:5080;http://localhost:5081"
            })
            .Build();

        SpikeFixtureSafety.EnsureLoopbackOnly(configuration);
    }

    [TestMethod]
    public void ExternalKestrelEndpointBindingIsRejected()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kestrel:Endpoints:Http:Url"] = "http://0.0.0.0:5080"
            })
            .Build();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => SpikeFixtureSafety.EnsureLoopbackOnly(configuration));
    }

    [TestMethod]
    public void MissingExplicitFixtureBindingIsRejected()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => SpikeFixtureSafety.EnsureLoopbackOnly(configuration));
    }
}

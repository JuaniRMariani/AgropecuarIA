using AgropecuarIA.Api;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class OidcConfigurationContractTests
{
    [TestMethod]
    public void OidcUsesCodePkceQueryAndUnmappedStandardClaims()
    {
        var options = new OpenIdConnectOptions();

        IdentityEndpoints.ConfigureOidc(options);

        Assert.AreEqual(OpenIdConnectResponseType.Code, options.ResponseType);
        Assert.AreEqual(OpenIdConnectResponseMode.Query, options.ResponseMode);
        Assert.IsTrue(options.UsePkce);
        Assert.IsFalse(options.MapInboundClaims);
        Assert.IsFalse(options.SaveTokens);
    }
}

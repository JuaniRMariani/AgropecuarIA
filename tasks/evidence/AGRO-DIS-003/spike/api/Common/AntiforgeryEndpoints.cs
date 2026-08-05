using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgropecuarIA.IdentitySpike.Api.Common;

internal static class AntiforgeryEndpoints
{
    internal const string HeaderName = "X-CSRF-TOKEN";

    internal static void Map(RouteGroupBuilder api) =>
        api.MapGet("/antiforgery", IssueToken);

    private static Ok<AntiforgeryTokenResponse> IssueToken(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        if (tokens.RequestToken is null)
        {
            throw new InvalidOperationException("The antiforgery service did not issue a request token.");
        }

        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(new AntiforgeryTokenResponse(tokens.RequestToken, HeaderName));
    }

    private sealed record AntiforgeryTokenResponse(string RequestToken, string HeaderName);
}

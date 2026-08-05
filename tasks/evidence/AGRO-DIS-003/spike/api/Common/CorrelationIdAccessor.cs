namespace AgropecuarIA.IdentitySpike.Api.Common;

internal static class CorrelationIdAccessor
{
    internal const string HeaderName = "X-Correlation-ID";
    private const string ItemKey = "identity-spike.correlation-id";

    internal static Guid Get(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var stored) && stored is Guid correlationId)
        {
            return correlationId;
        }

        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var resolved = Guid.TryParse(supplied, out var parsed) && parsed != Guid.Empty
            ? parsed
            : Guid.NewGuid();

        context.Items[ItemKey] = resolved;
        return resolved;
    }
}

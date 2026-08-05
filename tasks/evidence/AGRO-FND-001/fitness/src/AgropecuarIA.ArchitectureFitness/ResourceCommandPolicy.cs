namespace AgropecuarIA.ArchitectureFitness;

public enum ResourceScopeKind
{
    Platform,
    Tenant,
}

public sealed record ResourceCommandFacts(
    bool IsAuthenticated,
    ResourceScopeKind EffectiveScope,
    ResourceScopeKind ResourceScope,
    bool HasPlatformCapability,
    bool HasTenantCapability,
    bool ResourceExists,
    bool IsSameTenant,
    bool HasResourceAccess,
    bool HasMatchingEtag,
    bool ResourceStateAllowsCommand);

public sealed record ResourceCommandDecision(int StatusCode, string Code)
{
    public bool IsAllowed => StatusCode is >= 200 and < 300;
}

public static class ResourceCommandPolicy
{
    public static ResourceCommandDecision Evaluate(ResourceCommandFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (!facts.IsAuthenticated)
        {
            return new ResourceCommandDecision(401, "authentication_required");
        }

        // A tenant context never inherits platform authority and a platform context never
        // bypasses tenant selection/resource authorization.
        if (facts.EffectiveScope != facts.ResourceScope || !HasCapabilityForScope(facts))
        {
            return new ResourceCommandDecision(403, "action_forbidden");
        }

        var wrongTenant = facts.ResourceScope == ResourceScopeKind.Tenant && !facts.IsSameTenant;
        if (!facts.ResourceExists || wrongTenant || !facts.HasResourceAccess)
        {
            return new ResourceCommandDecision(404, "resource_not_found");
        }

        // Resource authorization deliberately precedes If-Match evaluation.
        if (!facts.HasMatchingEtag)
        {
            return new ResourceCommandDecision(412, "precondition_failed");
        }

        if (!facts.ResourceStateAllowsCommand)
        {
            return new ResourceCommandDecision(409, "resource_conflict");
        }

        return new ResourceCommandDecision(204, "command_accepted");
    }

    private static bool HasCapabilityForScope(ResourceCommandFacts facts) =>
        facts.ResourceScope switch
        {
            ResourceScopeKind.Platform => facts.HasPlatformCapability,
            ResourceScopeKind.Tenant => facts.HasTenantCapability,
            _ => false,
        };
}

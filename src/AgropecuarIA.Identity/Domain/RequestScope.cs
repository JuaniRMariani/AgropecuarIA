namespace AgropecuarIA.Identity.Domain;

public abstract record RequestScope
{
    private protected RequestScope()
    {
    }

    public abstract string Kind { get; }

    public abstract Guid? TenantId { get; }

    public static RequestScope Platform { get; } = new PlatformRequestScope();

    public static RequestScope ForTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID is required for tenant scope.", nameof(tenantId));
        }

        return new TenantRequestScope(tenantId);
    }

    public sealed record PlatformRequestScope : RequestScope
    {
        internal PlatformRequestScope()
        {
        }

        public override string Kind => "platform";

        public override Guid? TenantId => null;
    }

    public sealed record TenantRequestScope : RequestScope
    {
        internal TenantRequestScope(Guid tenantId)
        {
            Tenant = tenantId;
        }

        public Guid Tenant { get; }

        public override string Kind => "tenant";

        public override Guid? TenantId => Tenant;
    }
}

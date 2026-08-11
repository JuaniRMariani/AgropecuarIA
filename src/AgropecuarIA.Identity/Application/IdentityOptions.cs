namespace AgropecuarIA.Identity.Application;

public sealed class IdentityRuntimeOptions
{
    public static string SectionName => "Identity:Runtime";

    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(12);

    public TimeSpan LinkAttemptLifetime { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan StepUpAttemptLifetime { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan StrongAuthenticationWindow { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan RecentAuthenticationWindow { get; set; } = TimeSpan.FromMinutes(15);
}

public sealed class OrganizationBootstrapOptions
{
    public static string SectionName => "Identity:OrganizationBootstrap";

    public bool Enabled { get; set; }

    public string CurrentKeyVersion { get; set; } = string.Empty;

    public Dictionary<string, string> IdempotencyHmacKeys { get; set; } = new(StringComparer.Ordinal);
}

public sealed class OrganizationOwnerInvitationOptions
{
    public static string SectionName => "Identity:OrganizationOwnerInvitations";

    public bool Enabled { get; set; }

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

    public string CurrentKeyVersion { get; set; } = string.Empty;

    public Dictionary<string, string> HmacKeys { get; set; } = new(StringComparer.Ordinal);
}

namespace AgropecuarIA.Identity.Application;

public sealed class IdentityRuntimeOptions
{
    public static string SectionName => "Identity:Runtime";

    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(12);

    public TimeSpan LinkAttemptLifetime { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan RecentAuthenticationWindow { get; set; } = TimeSpan.FromMinutes(15);
}

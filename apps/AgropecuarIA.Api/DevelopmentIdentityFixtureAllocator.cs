using System.Globalization;
using System.Security.Cryptography;
using AgropecuarIA.Identity.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Api;

internal sealed class DevelopmentIdentityFixtureAllocator
{
    private const string CookieName = "__Host-agro.fixture-profile";
    private readonly IDataProtector protector;
    private readonly DevelopmentIdentityProviderOptions options;
    private int allocationSequence;

    public DevelopmentIdentityFixtureAllocator(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<DevelopmentIdentityProviderOptions> options)
    {
        protector = dataProtectionProvider.CreateProtector(
            "AgropecuarIA.Identity.DevelopmentFixtureProfile.v1");
        this.options = options.Value;
    }

    public VerifiedExternalIdentity Resolve(
        HttpContext context,
        string fixture,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);
        int? explicitProfile = IdentityFixtures.GetExplicitProfile(fixture);
        if (!IdentityFixtures.RequiresSyntheticProfile(fixture))
        {
            return IdentityFixtures.Resolve(fixture, profile: 1, now);
        }

        int? protectedProfile = ReadProtectedProfile(context);
        int profile = explicitProfile ?? protectedProfile ?? AllocateProfile();
        if (profile < 1 || profile > options.SyntheticProfileCount)
        {
            throw IdentityFixtures.InvalidFixture();
        }

        if (protectedProfile != profile)
        {
            context.Response.Cookies.Append(
                CookieName,
                protector.Protect(profile.ToString(CultureInfo.InvariantCulture)),
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    IsEssential = true,
                });
        }

        return IdentityFixtures.Resolve(fixture, profile, now);
    }

    private int AllocateProfile()
    {
        uint sequence = unchecked((uint)Interlocked.Increment(ref allocationSequence));
        return (int)(((sequence - 1U) % (uint)options.SyntheticProfileCount) + 1U);
    }

    private int? ReadProtectedProfile(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out string? protectedProfile) ||
            string.IsNullOrWhiteSpace(protectedProfile))
        {
            return null;
        }

        try
        {
            string value = protector.Unprotect(protectedProfile);
            return int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int profile) &&
                profile >= 1 &&
                profile <= options.SyntheticProfileCount
                    ? profile
                    : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}

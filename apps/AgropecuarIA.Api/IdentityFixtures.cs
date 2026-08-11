using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Api;

internal static class IdentityFixtures
{
    public static VerifiedExternalIdentity Resolve(
        string fixture,
        int profile,
        DateTimeOffset now) => fixture switch
        {
            "email-owner" or
            "email-owner-1" or
            "email-owner-2" or
            "email-owner-3" or
            "email-owner-4" => new(
                IdentityConnections.Email,
                "urn:agropecuaria:development",
                $"email-owner-{profile}",
                $"p{profile}***@demo.invalid",
                $"Productor demo {profile}",
                now,
                now),
            "google-owner" or
            "google-owner-1" or
            "google-owner-2" or
            "google-owner-3" or
            "google-owner-4" => new(
                IdentityConnections.Google,
                "urn:agropecuaria:development",
                $"google-owner-{profile}",
                $"p{profile}***@google.invalid",
                $"Productor demo {profile}",
                now,
                now),
            "identity-owned-by-another-user" => new(
                IdentityConnections.Google,
                "urn:agropecuaria:development",
                "another-owner",
                "o***r@other.invalid",
                "Otro productor",
                now,
                now),
            "unverified-email" => throw IdentityErrors.IdentityNotVerified(),
            "provider-down" => throw IdentityErrors.ProviderUnavailable(),
            _ => throw InvalidFixture(),
        };

    public static bool RequiresSyntheticProfile(string fixture) =>
        fixture is "email-owner" or "google-owner" or
            "email-owner-1" or "email-owner-2" or "email-owner-3" or "email-owner-4" or
            "google-owner-1" or "google-owner-2" or "google-owner-3" or "google-owner-4";

    public static int? GetExplicitProfile(string fixture) => fixture switch
    {
        "email-owner-1" or "google-owner-1" => 1,
        "email-owner-2" or "google-owner-2" => 2,
        "email-owner-3" or "google-owner-3" => 3,
        "email-owner-4" or "google-owner-4" => 4,
        _ => null,
    };

    public static IdentityOperationException InvalidFixture() =>
        new(
            "development.invalid_fixture",
            StatusCodes.Status400BadRequest,
            "The development fixture is invalid.");
}

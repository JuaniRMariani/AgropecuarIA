using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Api;

public static class IdentityFixtures
{
    private static readonly Guid DemoOrganizationId = new("8db0e58c-0960-47df-9929-8e57f501c72c");

    public static VerifiedExternalIdentity Resolve(string fixture, DateTimeOffset now) => fixture switch
    {
        "email-owner" => new(
            IdentityConnections.Email,
            "urn:agropecuaria:development",
            "email-owner",
            "o***r@demo.invalid",
            "Productor demo",
            now,
            now),
        "google-owner" => new(
            IdentityConnections.Google,
            "urn:agropecuaria:development",
            "google-owner",
            "o***r@google.invalid",
            "Productor demo",
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
        _ => throw new IdentityOperationException(
            "development.invalid_fixture",
            StatusCodes.Status400BadRequest,
            "The development fixture is invalid."),
    };

    public static async Task EnsureOwnerMembershipAsync(
        string fixture,
        Guid userId,
        IdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (fixture != "email-owner")
        {
            return;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO identity.organization_memberships
                ("UserId", "OrganizationId", "OrganizationName", "Role")
            VALUES
                ({userId}, {DemoOrganizationId}, {"Establecimiento demo"}, {"owner"})
            ON CONFLICT ("UserId", "OrganizationId") DO NOTHING
            """,
            cancellationToken);
    }
}

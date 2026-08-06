using System.Security.Claims;
using AgropecuarIA.Identity;
using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AgropecuarIA.Api;

public static class IdentityEndpoints
{
    private const string IdentityRateLimitPolicy = "identity";
    private const string LinkAttemptProperty = "agro:link_attempt_id";
    private const string ConnectionProperty = "agro:connection";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder identity = endpoints.MapGroup("/api/identity")
            .RequireRateLimiting(IdentityRateLimitPolicy);

        identity.MapGet("/capabilities", (
            IHostEnvironment environment,
            IOptions<OidcProviderOptions> configuredOptions,
            IOptions<DevelopmentIdentityProviderOptions> developmentOptions) =>
        {
            OidcProviderOptions options = configuredOptions.Value;
            bool developmentProviderEnabled =
                IsDevelopmentOrTest(environment) && developmentOptions.Value.Enabled;
            return Results.Ok(new IdentityCapabilitiesResponse(
                environment.EnvironmentName,
                options.IsConfigured,
                developmentProviderEnabled,
                [
                    new(
                        "email",
                        "Correo electrónico",
                        options.EmailEnabled && (options.IsConfigured || developmentProviderEnabled)),
                    new(
                        "google",
                        "Google",
                        options.GoogleEnabled && (options.IsConfigured || developmentProviderEnabled)),
                ]));
        });

        identity.MapGet("/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
        {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!));
        });

        identity.MapGet("/session", async (
            ClaimsPrincipal principal,
            IdentityApplicationService service,
            CancellationToken cancellationToken) =>
        {
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(principal);
            IdentitySessionResult session = await service.GetSessionAsync(current.UserId, cancellationToken);
            return Results.Ok(ToResponse(session));
        }).RequireAuthorization();

        identity.MapGet("/login/{connection}", (
            string connection,
            Guid? linkAttemptId,
            IOptions<OidcProviderOptions> configuredOptions) =>
        {
            if (!IdentityConnections.IsSupported(connection))
            {
                throw IdentityErrors.InvalidConnection();
            }

            OidcProviderOptions options = configuredOptions.Value;
            if (!options.IsConfigured || !options.IsConnectionEnabled(connection))
            {
                throw IdentityErrors.ProviderUnavailable();
            }

            AuthenticationProperties properties = new()
            {
                RedirectUri = "/",
            };
            properties.Items[ConnectionProperty] = connection;
            if (linkAttemptId is not null)
            {
                properties.Items[LinkAttemptProperty] = linkAttemptId.Value.ToString("D");
            }

            return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        identity.MapPost("/link-attempts", async (
            StartLinkAttemptRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            IOptions<OidcProviderOptions> configuredOptions,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            if (!configuredOptions.Value.IsConnectionEnabled(request.Connection))
            {
                throw IdentityErrors.ProviderUnavailable();
            }

            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            StartedLinkAttempt attempt = await service.StartLinkAttemptAsync(
                current,
                request.Connection,
                RequestContext(context),
                cancellationToken);
            return Results.Created(
                $"/api/identity/link-attempts/{attempt.AttemptId:D}",
                new LinkAttemptResponse(
                    attempt.AttemptId,
                    attempt.Connection,
                    attempt.ExpiresAtUtc,
                    attempt.AuthorizationUrl));
        }).RequireAuthorization();

        identity.MapPost("/link-attempts/{attemptId:guid}/complete", async (
            Guid attemptId,
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            IdentitySessionResult session = await service.CompleteLinkAttemptAsync(
                attemptId,
                current,
                RequestContext(context),
                cancellationToken);
            return Results.Ok(ToResponse(session));
        }).RequireAuthorization();

        identity.MapDelete("/identities/{identityId:guid}", async (
            Guid identityId,
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            await service.UnlinkIdentityAsync(
                identityId,
                current,
                RequestContext(context),
                cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        identity.MapPost("/session/revoke", async (
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            await service.RevokeSessionAsync(current, RequestContext(context), cancellationToken);
            DeleteSessionCookie(context);
            return Results.NoContent();
        }).RequireAuthorization();

        IHostEnvironment hostEnvironment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        DevelopmentIdentityProviderOptions developmentProvider = endpoints.ServiceProvider
            .GetRequiredService<IOptions<DevelopmentIdentityProviderOptions>>().Value;
        if (IsDevelopmentOrTest(hostEnvironment) && developmentProvider.Enabled)
        {
            endpoints.MapDevelopmentIdentityEndpoints();
        }

        return endpoints;
    }

    public static void ConfigureOidc(OpenIdConnectOptions options)
    {
        options.ResponseType = "code";
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.UsePkce = true;
        options.MapInboundClaims = false;
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                if (context.Properties.Items.TryGetValue(ConnectionProperty, out string? connection) &&
                    connection is not null)
                {
                    OidcProviderOptions configured = context.HttpContext.RequestServices
                        .GetRequiredService<IOptions<OidcProviderOptions>>().Value;
                    context.ProtocolMessage.SetParameter(
                        "connection",
                        configured.GetProviderConnection(connection));
                }

                return Task.CompletedTask;
            },
            OnTicketReceived = CompleteOidcSignInAsync,
            OnRemoteFailure = async context =>
            {
                context.HandleResponse();
                context.HttpContext.RequestServices
                    .GetRequiredService<IdentityTelemetry>()
                    .Record("oidc_callback", "provider_unavailable");
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status503ServiceUnavailable,
                        Title = "The identity provider is unavailable.",
                        Type = "https://agropecuaria.local/problems/identity.provider_unavailable",
                        Extensions =
                        {
                            ["code"] = "identity.provider_unavailable",
                            ["correlationId"] = context.HttpContext.TraceIdentifier,
                        },
                    },
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: context.HttpContext.RequestAborted);
            },
        };
    }

    private static RouteGroupBuilder MapDevelopmentIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder development = endpoints.MapGroup("/api/development/identity")
            .RequireRateLimiting(IdentityRateLimitPolicy);

        development.MapPost("/sign-in", async (
            DevelopmentFixtureRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            IdentityDbContext dbContext,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            VerifiedExternalIdentity identity = IdentityFixtures.Resolve(request.Fixture, timeProvider.GetUtcNow());
            IssuedSession issued = await service.SignInAsync(identity, RequestContext(context), cancellationToken);
            await IdentityFixtures.EnsureOwnerMembershipAsync(
                request.Fixture,
                issued.UserId,
                dbContext,
                cancellationToken);
            AppendSessionCookie(context, issued);
            return Results.NoContent();
        });

        development.MapPost("/link-attempts/{attemptId:guid}/verify", async (
            Guid attemptId,
            DevelopmentFixtureRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            VerifiedExternalIdentity identity = IdentityFixtures.Resolve(request.Fixture, timeProvider.GetUtcNow());
            await service.AttachCandidateProofAsync(
                attemptId,
                current,
                identity,
                RequestContext(context),
                cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        return development;
    }

    private static async Task CompleteOidcSignInAsync(TicketReceivedContext context)
    {
        ClaimsPrincipal principal = context.Principal ?? throw IdentityErrors.IdentityNotVerified();
        string? subject = principal.FindFirstValue("sub");
        string? verifiedValue = principal.FindFirstValue("email_verified");
        string? connection = null;
        context.Properties?.Items.TryGetValue(ConnectionProperty, out connection);
        if (subject is null ||
            connection is null ||
            !bool.TryParse(verifiedValue, out bool verified) ||
            !verified)
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        OidcProviderOptions options = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<OidcProviderOptions>>().Value;
        if (!options.IsConnectionEnabled(connection))
        {
            throw IdentityErrors.ProviderUnavailable();
        }

        string issuer = principal.FindFirstValue("iss") ?? options.Authority.TrimEnd('/');
        string? email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        string label = MaskEmail(email);
        string displayName = principal.FindFirstValue("name") ?? label;
        DateTimeOffset now = context.HttpContext.RequestServices
            .GetRequiredService<TimeProvider>().GetUtcNow();
        VerifiedExternalIdentity externalIdentity = new(
            connection,
            issuer,
            subject,
            label,
            Limit(displayName, 160),
            now);
        IdentityApplicationService service = context.HttpContext.RequestServices
            .GetRequiredService<IdentityApplicationService>();
        IdentityRequestContext requestContext = RequestContext(context.HttpContext);

        if (context.Properties?.Items.TryGetValue(LinkAttemptProperty, out string? attemptValue) is true &&
            Guid.TryParse(attemptValue, out Guid attemptId))
        {
            AuthenticateResult applicationAuthentication = await context.HttpContext.AuthenticateAsync(
                IdentityAuthenticationDefaults.SessionScheme);
            if (!applicationAuthentication.Succeeded || applicationAuthentication.Principal is null)
            {
                throw IdentityErrors.SessionRequired();
            }

            AuthenticatedSession current = AuthenticatedSessionClaims.Read(applicationAuthentication.Principal);
            await service.AttachCandidateProofAsync(
                attemptId,
                current,
                externalIdentity,
                requestContext,
                context.HttpContext.RequestAborted);
            await service.CompleteLinkAttemptAsync(
                attemptId,
                current,
                requestContext,
                context.HttpContext.RequestAborted);
            context.Response.Redirect("/?identityLinked=true");
        }
        else
        {
            IssuedSession issued = await service.SignInAsync(
                externalIdentity,
                requestContext,
                context.HttpContext.RequestAborted);
            AppendSessionCookie(context.HttpContext, issued);
            context.Response.Redirect("/");
        }

        context.HandleResponse();
    }

    private static IdentitySessionResponse ToResponse(IdentitySessionResult session) => new(
        session.UserId,
        session.DisplayName,
        session.Identities
            .Select(identity => new LinkedIdentityResponse(
                identity.IdentityId,
                identity.Connection,
                identity.Label,
                identity.VerifiedAtUtc))
            .ToArray(),
        session.Memberships
            .Select(membership => new MembershipResponse(
                membership.OrganizationId,
                membership.OrganizationName,
                membership.Role))
            .ToArray());

    private static void AppendSessionCookie(HttpContext context, IssuedSession issued) =>
        context.Response.Cookies.Append(
            IdentityAuthenticationDefaults.SessionCookieName,
            issued.Token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = issued.ExpiresAtUtc,
                IsEssential = true,
            });

    private static void DeleteSessionCookie(HttpContext context) =>
        context.Response.Cookies.Delete(
            IdentityAuthenticationDefaults.SessionCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
            });

    private static IdentityRequestContext RequestContext(HttpContext context) =>
        new(context.TraceIdentifier);

    private static bool IsDevelopmentOrTest(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("Test");

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Identidad verificada";
        }

        int separator = email.IndexOf('@', StringComparison.Ordinal);
        if (separator <= 0 || separator == email.Length - 1)
        {
            return "Identidad verificada";
        }

        return $"{email[0]}***@{Limit(email[(separator + 1)..], 120)}";
    }

    private static string Limit(string value, int length) =>
        value.Length <= length ? value : value[..length];

    private sealed record IdentityCapabilitiesResponse(
        string Environment,
        bool OidcConfigured,
        bool DevelopmentProviderEnabled,
        IReadOnlyList<IdentityConnectionResponse> Connections);

    private sealed record IdentityConnectionResponse(string Id, string Label, bool Available);

    private sealed record AntiforgeryTokenResponse(string Token);

    private sealed record StartLinkAttemptRequest(string Connection);

    private sealed record LinkAttemptResponse(
        Guid AttemptId,
        string Connection,
        DateTimeOffset ExpiresAtUtc,
        string AuthorizationUrl);

    private sealed record DevelopmentFixtureRequest(string Fixture);

    private sealed record IdentitySessionResponse(
        Guid UserId,
        string DisplayName,
        IReadOnlyList<LinkedIdentityResponse> Identities,
        IReadOnlyList<MembershipResponse> Memberships);

    private sealed record LinkedIdentityResponse(
        Guid IdentityId,
        string Connection,
        string Label,
        DateTimeOffset VerifiedAtUtc);

    private sealed record MembershipResponse(Guid OrganizationId, string OrganizationName, string Role);
}

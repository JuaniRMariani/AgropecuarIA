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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AgropecuarIA.Api;

public static class IdentityEndpoints
{
    private const string IdentityRateLimitPolicy = "identity";
    private const string LinkAttemptProperty = "agro:link_attempt_id";
    private const string StepUpAttemptProperty = "agro:step_up_attempt_id";
    private const string ConnectionProperty = "agro:connection";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder identity = endpoints.MapGroup("/api/identity")
            .RequireRateLimiting(IdentityRateLimitPolicy);

        identity.MapGet("/capabilities", (
            IHostEnvironment environment,
            IOptions<OidcProviderOptions> configuredOptions,
            IOptions<DevelopmentIdentityProviderOptions> developmentOptions,
            IOptions<StrongAuthenticationOptions> strongAuthenticationOptions) =>
        {
            OidcProviderOptions options = configuredOptions.Value;
            bool developmentProviderEnabled =
                IsDevelopmentOrTest(environment) && developmentOptions.Value.Enabled;
            return Results.Ok(new IdentityCapabilitiesResponse(
                environment.EnvironmentName,
                options.IsConfigured,
                developmentProviderEnabled,
                strongAuthenticationOptions.Value.Enabled &&
                    (options.IsConfigured || developmentProviderEnabled),
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
            IdentitySessionResult session = await service.GetSessionAsync(current, cancellationToken);
            return Results.Ok(ToResponse(session));
        }).RequireAuthorization();

        identity.MapGet("/login/{connection}", (
            string connection,
            Guid? linkAttemptId,
            IOptions<OidcProviderOptions> configuredOptions,
            TimeProvider timeProvider) =>
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
            OidcReauthentication.PrepareChallenge(properties, timeProvider.GetUtcNow());
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
                RequestContext(context, current),
                cancellationToken);
            return Results.Created(
                $"/api/identity/link-attempts/{attempt.AttemptId:D}",
                new LinkAttemptResponse(
                    attempt.AttemptId,
                    attempt.Connection,
                    attempt.ExpiresAtUtc,
                    attempt.AuthorizationUrl));
        }).RequireAuthorization();

        identity.MapPost("/step-up-attempts", async (
            StartStepUpAttemptRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            IHostEnvironment environment,
            IOptions<OidcProviderOptions> oidcOptions,
            IOptions<DevelopmentIdentityProviderOptions> developmentOptions,
            IOptions<StrongAuthenticationOptions> strongAuthenticationOptions,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            bool developmentProviderEnabled =
                IsDevelopmentOrTest(environment) && developmentOptions.Value.Enabled;
            if (!strongAuthenticationOptions.Value.Enabled ||
                (!oidcOptions.Value.IsConfigured && !developmentProviderEnabled))
            {
                throw IdentityErrors.ProviderUnavailable();
            }

            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            StartedStepUpAttempt attempt = await service.StartStepUpAttemptAsync(
                current,
                request.Purpose,
                RequestContext(context, current),
                cancellationToken);
            return Results.Created(
                $"/api/identity/step-up-attempts/{attempt.AttemptId:D}",
                new StepUpAttemptResponse(
                    attempt.AttemptId,
                    attempt.Purpose,
                    attempt.ExpiresAtUtc,
                    attempt.AuthorizationUrl));
        })
            .RequireAuthorization()
            .RequireRateLimiting("identity-step-up");

        identity.MapGet("/step-up/{attemptId:guid}", async (
            Guid attemptId,
            HttpContext context,
            IdentityApplicationService service,
            IOptions<OidcProviderOptions> oidcOptions,
            IOptions<StrongAuthenticationOptions> strongAuthenticationOptions,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            if (!strongAuthenticationOptions.Value.Enabled || !oidcOptions.Value.IsConfigured)
            {
                throw IdentityErrors.ProviderUnavailable();
            }

            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            await service.ValidateStepUpAttemptAsync(
                attemptId,
                current,
                RequestContext(context, current),
                cancellationToken);
            AuthenticationProperties properties = new()
            {
                RedirectUri = "/",
            };
            OidcReauthentication.PrepareChallenge(
                properties,
                timeProvider.GetUtcNow(),
                requireStrongAuthentication: true);
            properties.Items[StepUpAttemptProperty] = attemptId.ToString("D");
            return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
        })
            .RequireAuthorization()
            .RequireRateLimiting("identity-step-up");

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
                RequestContext(context, current),
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
                RequestContext(context, current),
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
            await service.RevokeSessionAsync(
                current,
                RequestContext(context, current),
                cancellationToken);
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
                OidcReauthentication.ApplyChallenge(
                    context.ProtocolMessage,
                    context.Properties);
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
            OnTokenValidated = context =>
            {
                ClaimsPrincipal principal = context.Principal ?? throw IdentityErrors.IdentityNotVerified();
                OidcReauthentication.ValidateToken(
                    principal,
                    context.Properties ?? throw IdentityErrors.IdentityNotVerified(),
                    context.HttpContext.RequestServices
                        .GetRequiredService<TimeProvider>()
                        .GetUtcNow());
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
            IssuedSession issued = await service.SignInAsync(
                identity,
                RequestContext(context),
                cancellationToken);
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
                RequestContext(context, current),
                cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        development.MapPost("/step-up-attempts/{attemptId:guid}/complete", async (
            Guid attemptId,
            HttpContext context,
            IAntiforgery antiforgery,
            IdentityApplicationService service,
            IdentityDbContext dbContext,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            ExternalIdentity identity = await dbContext.ExternalIdentities
                .AsNoTracking()
                .Where(item => item.UserId == current.UserId)
                .OrderBy(item => item.Id)
                .FirstAsync(cancellationToken);
            IssuedSession issued = await service.CompleteStepUpAttemptAsync(
                attemptId,
                current,
                new VerifiedStepUpProof(
                    identity.Issuer,
                    identity.Subject,
                    timeProvider.GetUtcNow(),
                    IsStrongAuthentication: true),
                RequestContext(context, current),
                cancellationToken);
            AppendSessionCookie(context, issued);
            AuthenticatedSession rotated = await service.AuthenticateAsync(
                issued.Token,
                cancellationToken) ?? throw IdentityErrors.SessionRequired();
            IdentitySessionResult session = await service.GetSessionAsync(rotated, cancellationToken);
            return Results.Ok(ToResponse(session));
        })
            .RequireAuthorization()
            .RequireRateLimiting("identity-step-up");

        return development;
    }

    private static async Task CompleteOidcSignInAsync(TicketReceivedContext context)
    {
        ClaimsPrincipal principal = context.Principal ?? throw IdentityErrors.IdentityNotVerified();
        OidcValidatedAuthentication validatedToken =
            OidcReauthentication.ReadValidatedToken(context.Properties);
        if (context.Properties?.Items.TryGetValue(StepUpAttemptProperty, out string? stepUpAttemptValue) is true &&
            Guid.TryParse(stepUpAttemptValue, out Guid stepUpAttemptId))
        {
            AuthenticateResult authentication = await context.HttpContext.AuthenticateAsync(
                IdentityAuthenticationDefaults.SessionScheme);
            if (!authentication.Succeeded || authentication.Principal is null)
            {
                throw IdentityErrors.SessionRequired();
            }

            AuthenticatedSession current = AuthenticatedSessionClaims.Read(authentication.Principal);
            IdentityApplicationService stepUpService = context.HttpContext.RequestServices
                .GetRequiredService<IdentityApplicationService>();
            IssuedSession issued = await stepUpService.CompleteStepUpAttemptAsync(
                stepUpAttemptId,
                current,
                new VerifiedStepUpProof(
                    validatedToken.Issuer,
                    validatedToken.Subject,
                    validatedToken.AuthenticatedAtUtc,
                    validatedToken.IsStrongAuthentication),
                RequestContext(context.HttpContext, current),
                context.HttpContext.RequestAborted);
            AppendSessionCookie(context.HttpContext, issued);
            context.Response.Redirect("/?stepUp=success");
            context.HandleResponse();
            return;
        }

        string? verifiedValue = principal.FindFirstValue("email_verified");
        string? connection = null;
        context.Properties?.Items.TryGetValue(ConnectionProperty, out connection);
        if (connection is null ||
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

        string? email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        string label = MaskEmail(email);
        string displayName = principal.FindFirstValue("name") ?? label;
        DateTimeOffset now = context.HttpContext.RequestServices
            .GetRequiredService<TimeProvider>().GetUtcNow();
        VerifiedExternalIdentity externalIdentity = new(
            connection,
            validatedToken.Issuer,
            validatedToken.Subject,
            label,
            Limit(displayName, 160),
            now,
            validatedToken.AuthenticatedAtUtc);
        IdentityApplicationService service = context.HttpContext.RequestServices
            .GetRequiredService<IdentityApplicationService>();
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
            IdentityRequestContext requestContext = RequestContext(context.HttpContext, current);
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
                RequestContext(context.HttpContext),
                context.HttpContext.RequestAborted);
            AppendSessionCookie(context.HttpContext, issued);
            context.Response.Redirect("/");
        }

        context.HandleResponse();
    }

    private static IdentitySessionResponse ToResponse(IdentitySessionResult session) => new(
        session.UserId,
        session.DisplayName,
        new AuthenticationAssuranceResponse(
            session.AuthenticationAssurance.Level,
            session.AuthenticationAssurance.AuthenticatedAtUtc,
            session.AuthenticationAssurance.Purpose,
            session.AuthenticationAssurance.StrongAuthenticatedAtUtc,
            session.AuthenticationAssurance.ExpiresAtUtc),
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

    private static IdentityRequestContext RequestContext(
        HttpContext context,
        AuthenticatedSession? session = null) =>
        IdentityRequestContext.ForPlatform(context.TraceIdentifier, session?.UserId);

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
        bool StrongAuthenticationAvailable,
        IReadOnlyList<IdentityConnectionResponse> Connections);

    private sealed record IdentityConnectionResponse(string Id, string Label, bool Available);

    private sealed record AntiforgeryTokenResponse(string Token);

    private sealed record StartLinkAttemptRequest(string Connection);

    private sealed record StartStepUpAttemptRequest(string Purpose);

    private sealed record LinkAttemptResponse(
        Guid AttemptId,
        string Connection,
        DateTimeOffset ExpiresAtUtc,
        string AuthorizationUrl);

    private sealed record StepUpAttemptResponse(
        Guid AttemptId,
        string Purpose,
        DateTimeOffset ExpiresAtUtc,
        string AuthorizationUrl);

    private sealed record DevelopmentFixtureRequest(string Fixture);

    private sealed record IdentitySessionResponse(
        Guid UserId,
        string DisplayName,
        AuthenticationAssuranceResponse Authentication,
        IReadOnlyList<LinkedIdentityResponse> Identities,
        IReadOnlyList<MembershipResponse> Memberships);

    private sealed record AuthenticationAssuranceResponse(
        string Level,
        DateTimeOffset AuthenticatedAtUtc,
        string? Purpose,
        DateTimeOffset? StrongAuthenticatedAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    private sealed record LinkedIdentityResponse(
        Guid IdentityId,
        string Connection,
        string Label,
        DateTimeOffset VerifiedAtUtc);

    private sealed record MembershipResponse(Guid OrganizationId, string OrganizationName, string Role);
}

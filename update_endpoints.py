import sys

path = r'B:\Xenova\AgropecuarIA\apps\AgropecuarIA.Api\IdentityEndpoints.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

bad_part = '''
        identity.MapPost("/mfa/totp/setup", async (
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            if (context.GetAuthenticatedSession() is not { } session) return Results.Unauthorized();
            var result = await service.SetupTotpAsync(session, cancellationToken);
            return result.Match(
                res => Results.Ok(res),
                ErrorResults.Problem);
        });

        identity.MapPost("/mfa/totp/enable", async (
            [FromQuery] string unverifiedSecret,
            EnableTotpCommand request,
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            if (context.GetAuthenticatedSession() is not { } session) return Results.Unauthorized();
            var result = await service.EnableTotpAsync(request, unverifiedSecret, session, cancellationToken);
            return result.Match(
                res => Results.Ok(res),
                ErrorResults.Problem);
        });

        identity.MapPost("/mfa/totp/disable", async (
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            if (context.GetAuthenticatedSession() is not { } session) return Results.Unauthorized();
            var result = await service.DisableTotpAsync(session, cancellationToken);
            return result.Match(
                _ => Results.NoContent(),
                ErrorResults.Problem);
        });

        identity.MapPost("/mfa/recovery/consume", async (
            ConsumeRecoveryCodeCommand request,
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            if (context.GetAuthenticatedSession() is not { } session) return Results.Unauthorized();
            var result = await service.ConsumeRecoveryCodeAsync(request, session, cancellationToken);
            return result.Match(
                _ => Results.NoContent(),
                ErrorResults.Problem);
        });
'''

good_part = '''
        identity.MapPost("/mfa/totp/setup", async (
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            var result = await service.SetupTotpAsync(current, cancellationToken);
            return result.Match(
                res => Results.Ok(res),
                ErrorResults.Problem);
        }).RequireAuthorization();

        identity.MapPost("/mfa/totp/enable", async (
            [FromQuery] string unverifiedSecret,
            EnableTotpCommand request,
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            var result = await service.EnableTotpAsync(request, unverifiedSecret, current, cancellationToken);
            return result.Match(
                res => Results.Ok(res),
                ErrorResults.Problem);
        }).RequireAuthorization();

        identity.MapPost("/mfa/totp/disable", async (
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            var result = await service.DisableTotpAsync(current, cancellationToken);
            return result.Match(
                _ => Results.NoContent(),
                ErrorResults.Problem);
        }).RequireAuthorization();

        identity.MapPost("/mfa/recovery/consume", async (
            ConsumeRecoveryCodeCommand request,
            HttpContext context,
            MfaApplicationService service,
            CancellationToken cancellationToken) =>
        {
            AuthenticatedSession current = AuthenticatedSessionClaims.Read(context.User);
            var result = await service.ConsumeRecoveryCodeAsync(request, current, cancellationToken);
            return result.Match(
                _ => Results.NoContent(),
                ErrorResults.Problem);
        }).RequireAuthorization();
'''

content = content.replace(bad_part, good_part)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

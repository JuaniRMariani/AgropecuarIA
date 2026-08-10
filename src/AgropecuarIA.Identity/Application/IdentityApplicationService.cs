using System.Data;
using System.Diagnostics;
using System.Text.Json;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AgropecuarIA.Identity.Application;

public sealed class IdentityApplicationService(
    IdentityDbContext dbContext,
    IdentityTokenService tokenService,
    IdentityTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<IdentityRuntimeOptions> options)
{
    private static readonly TimeSpan AuthenticationClockSkew = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions EventSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IdentityRuntimeOptions runtimeOptions = options.Value;

    public async Task<AuthenticatedSession?> AuthenticateAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        byte[] hash = IdentityTokenService.HashToken(token);
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await dbContext.Sessions
            .AsNoTracking()
            .Where(session =>
                session.TokenHash == hash &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > now)
            .Select(session => new AuthenticatedSession(
                session.Id,
                session.UserId,
                session.AuthenticatedAtUtc,
                session.IsAuthenticationAssuranceVerified))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IssuedSession> SignInAsync(
        VerifiedExternalIdentity verifiedIdentity,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatform();
        try
        {
            ValidateVerifiedIdentity(verifiedIdentity);
        }
        catch (IdentityOperationException)
        {
            telemetry.Record("sign_in", "rejected");
            throw;
        }
        using Activity? activity = IdentityTelemetry.Start("identity.sign_in");

        ExternalIdentity? existingIdentity = await dbContext.ExternalIdentities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                identity => identity.Issuer == verifiedIdentity.Issuer &&
                    identity.Subject == verifiedIdentity.Subject,
                cancellationToken);

        Guid userId;
        if (existingIdentity is not null)
        {
            userId = existingIdentity.UserId;
        }
        else
        {
            userId = Guid.NewGuid();
            DateTimeOffset now = timeProvider.GetUtcNow();
            dbContext.Users.Add(new PlatformUser(userId, verifiedIdentity.DisplayName, now));
            dbContext.ExternalIdentities.Add(new ExternalIdentity(
                Guid.NewGuid(),
                userId,
                verifiedIdentity.Connection,
                verifiedIdentity.Issuer,
                verifiedIdentity.Subject,
                verifiedIdentity.Label,
                verifiedIdentity.VerifiedAtUtc));
        }

        IssuedSession issuedSession = IssueVerifiedSession(
            userId,
            verifiedIdentity.AuthenticatedAtUtc);
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            userId,
            issuedSession.SessionId,
            "sign_in",
            "succeeded",
            verifiedIdentity.Connection,
            requestContext));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsConcurrentExternalIdentityInsert(exception))
        {
            dbContext.ChangeTracker.Clear();
            ExternalIdentity concurrentIdentity = await dbContext.ExternalIdentities
                .AsNoTracking()
                .SingleAsync(
                    identity => identity.Issuer == verifiedIdentity.Issuer &&
                        identity.Subject == verifiedIdentity.Subject,
                    cancellationToken);
            IssuedSession concurrentSession = IssueVerifiedSession(
                concurrentIdentity.UserId,
                verifiedIdentity.AuthenticatedAtUtc);
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                concurrentIdentity.UserId,
                concurrentSession.SessionId,
                "sign_in",
                "succeeded",
                verifiedIdentity.Connection,
                requestContext));
            await dbContext.SaveChangesAsync(cancellationToken);
            telemetry.Record("sign_in", "succeeded", verifiedIdentity.Connection);
            return concurrentSession;
        }
        catch (DbUpdateException exception)
        {
            telemetry.Record("sign_in", "conflict", verifiedIdentity.Connection);
            throw new IdentityOperationException(
                "identity.concurrent_sign_in",
                409,
                "The sign-in could not be completed safely.")
            { Source = exception.Source };
        }

        telemetry.Record("sign_in", "succeeded", verifiedIdentity.Connection);
        return issuedSession;
    }

    public async Task<IdentitySessionResult> GetSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        PlatformUser user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw IdentityErrors.SessionRequired();

        LinkedIdentityResult[] identities = await dbContext.ExternalIdentities
            .AsNoTracking()
            .Where(identity => identity.UserId == userId)
            .OrderBy(identity => identity.VerifiedAtUtc)
            .Select(identity => new LinkedIdentityResult(
                identity.Id,
                identity.Connection,
                identity.Label,
                identity.VerifiedAtUtc))
            .ToArrayAsync(cancellationToken);

        MembershipResult[] memberships = await dbContext.Memberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .OrderBy(membership => membership.OrganizationName)
            .Select(membership => new MembershipResult(
                membership.OrganizationId,
                membership.OrganizationName,
                membership.Role))
            .ToArrayAsync(cancellationToken);

        return new IdentitySessionResult(user.Id, user.DisplayName, identities, memberships);
    }

    public async Task<StartedLinkAttempt> StartLinkAttemptAsync(
        AuthenticatedSession session,
        string connection,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(session.UserId);
        ValidateConnection(connection);
        if (!IsRecentAuthentication(session))
        {
            await AuditFailureAsync(session, "link_started", connection, requestContext, cancellationToken);
            throw IdentityErrors.RecentAuthenticationRequired();
        }

        bool alreadyLinked = await dbContext.ExternalIdentities
            .AsNoTracking()
            .AnyAsync(
                identity => identity.UserId == session.UserId && identity.Connection == connection,
                cancellationToken);
        if (alreadyLinked)
        {
            await AuditFailureAsync(session, "link_started", connection, requestContext, cancellationToken);
            throw IdentityErrors.IdentityConflict();
        }

        DateTimeOffset expiresAtUtc = timeProvider.GetUtcNow().Add(runtimeOptions.LinkAttemptLifetime);
        LinkAttempt attempt = new(Guid.NewGuid(), session.UserId, session.SessionId, connection, expiresAtUtc);
        dbContext.LinkAttempts.Add(attempt);
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            session.UserId,
            session.SessionId,
            "link_started",
            "succeeded",
            connection,
            requestContext));
        await dbContext.SaveChangesAsync(cancellationToken);

        telemetry.Record("link_started", "succeeded", connection);
        return new StartedLinkAttempt(
            attempt.Id,
            connection,
            expiresAtUtc,
            $"/api/identity/login/{connection}?linkAttemptId={attempt.Id:D}");
    }

    public async Task AttachCandidateProofAsync(
        Guid attemptId,
        AuthenticatedSession currentSession,
        VerifiedExternalIdentity candidate,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        try
        {
            ValidateVerifiedIdentity(candidate);
        }
        catch (IdentityOperationException)
        {
            await AuditFailureAsync(
                currentSession,
                "link_proof_attached",
                IdentityConnections.IsSupported(candidate.Connection) ? candidate.Connection : null,
                requestContext,
                cancellationToken);
            throw;
        }
        if (!IsRecentAuthentication(currentSession))
        {
            await AuditFailureAsync(
                currentSession,
                "link_proof_attached",
                candidate.Connection,
                requestContext,
                cancellationToken);
            throw IdentityErrors.RecentAuthenticationRequired();
        }
        LinkAttempt attempt = await dbContext.LinkAttempts
            .SingleOrDefaultAsync(item => item.Id == attemptId, cancellationToken)
            ?? throw IdentityErrors.LinkAttemptConflict();

        if (attempt.UserId != currentSession.UserId ||
            attempt.InitiatingSessionId != currentSession.SessionId ||
            attempt.Connection != candidate.Connection ||
            attempt.ExpiresAtUtc <= timeProvider.GetUtcNow() ||
            attempt.ConsumedAtUtc is not null ||
            attempt.HasCandidateProof)
        {
            await AuditFailureAsync(
                currentSession,
                "link_proof_attached",
                candidate.Connection,
                requestContext,
                cancellationToken);
            throw IdentityErrors.LinkAttemptConflict();
        }

        attempt.AttachCandidateProof(candidate);
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            attempt.UserId,
            attempt.InitiatingSessionId,
            "link_proof_attached",
            "succeeded",
            candidate.Connection,
            requestContext));
        await dbContext.SaveChangesAsync(cancellationToken);
        telemetry.Record("link_proof_attached", "succeeded", candidate.Connection);
    }

    public async Task<IdentitySessionResult> CompleteLinkAttemptAsync(
        Guid attemptId,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        if (!IsRecentAuthentication(currentSession))
        {
            await AuditFailureAsync(
                currentSession,
                "identity_linked",
                null,
                requestContext,
                cancellationToken);
            throw IdentityErrors.RecentAuthenticationRequired();
        }
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        LinkAttempt? attempt = await dbContext.LinkAttempts
            .SingleOrDefaultAsync(item => item.Id == attemptId, cancellationToken);
        if (attempt is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            await AuditFailureAsync(
                currentSession,
                "identity_linked",
                null,
                requestContext,
                cancellationToken);
            throw IdentityErrors.LinkAttemptConflict();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (attempt.UserId != currentSession.UserId ||
            attempt.InitiatingSessionId != currentSession.SessionId ||
            attempt.ExpiresAtUtc <= now ||
            attempt.ConsumedAtUtc is not null ||
            !attempt.HasCandidateProof ||
            attempt.CandidateVerifiedAtUtc is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            await AuditFailureAsync(
                currentSession,
                "identity_linked",
                attempt.Connection,
                requestContext,
                cancellationToken);
            throw IdentityErrors.LinkAttemptConflict();
        }

        ExternalIdentity? existingIdentity = await dbContext.ExternalIdentities
            .SingleOrDefaultAsync(
                identity => identity.Issuer == attempt.CandidateIssuer &&
                    identity.Subject == attempt.CandidateSubject,
                cancellationToken);
        if (existingIdentity is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            await AuditFailureAsync(
                currentSession,
                "identity_linked",
                attempt.Connection,
                requestContext,
                cancellationToken);
            throw IdentityErrors.IdentityConflict();
        }

        Guid linkedIdentityId = Guid.NewGuid();
        PlatformUser user = await dbContext.Users
            .SingleAsync(item => item.Id == currentSession.UserId, cancellationToken);
        long aggregateVersion = user.NextVersion();
        dbContext.ExternalIdentities.Add(new ExternalIdentity(
            linkedIdentityId,
            currentSession.UserId,
            attempt.Connection,
            attempt.CandidateIssuer!,
            attempt.CandidateSubject!,
            attempt.CandidateLabel!,
            attempt.CandidateVerifiedAtUtc.Value));
        attempt.Consume(now);
        dbContext.OutboxMessages.Add(new IdentityOutboxMessage(
            Guid.NewGuid(),
            "IdentityLinked",
            1,
            IdentityOutboxMessage.CurrentSchemaVersion,
            IdentityOutboxMessage.IdentitySource,
            requestContext.Scope,
            now,
            now,
            now,
            requestContext.ActorId!.Value,
            requestContext.CorrelationId,
            attempt.Id,
            nameof(PlatformUser),
            currentSession.UserId,
            aggregateVersion,
            JsonSerializer.Serialize(
                new IdentityLinkedEvent(
                    currentSession.UserId,
                    linkedIdentityId,
                    attempt.Connection,
                    now),
                EventSerializerOptions)));
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            currentSession.UserId,
            currentSession.SessionId,
            "identity_linked",
            "succeeded",
            attempt.Connection,
            requestContext));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is DbUpdateException or DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            await AuditFailureAsync(
                currentSession,
                "identity_linked",
                attempt.Connection,
                requestContext,
                cancellationToken);
            throw IdentityErrors.IdentityConflict();
        }

        telemetry.Record("identity_linked", "succeeded", attempt.Connection);
        return await GetSessionAsync(currentSession.UserId, cancellationToken);
    }

    public async Task UnlinkIdentityAsync(
        Guid identityId,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        if (!IsRecentAuthentication(currentSession))
        {
            await AuditFailureAsync(
                currentSession,
                "identity_unlinked",
                null,
                requestContext,
                cancellationToken);
            throw IdentityErrors.RecentAuthenticationRequired();
        }
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        ExternalIdentity? identity = await dbContext.ExternalIdentities
            .SingleOrDefaultAsync(
                item => item.Id == identityId && item.UserId == currentSession.UserId,
                cancellationToken);
        if (identity is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            await AuditFailureAsync(
                currentSession,
                "identity_unlinked",
                null,
                requestContext,
                cancellationToken);
            throw IdentityErrors.IdentityConflict();
        }

        int identityCount = await dbContext.ExternalIdentities
            .CountAsync(item => item.UserId == currentSession.UserId, cancellationToken);
        if (identityCount <= 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            await AuditFailureAsync(
                currentSession,
                "identity_unlinked",
                identity.Connection,
                requestContext,
                cancellationToken);
            throw IdentityErrors.LastIdentity();
        }

        dbContext.ExternalIdentities.Remove(identity);
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            currentSession.UserId,
            currentSession.SessionId,
            "identity_unlinked",
            "succeeded",
            identity.Connection,
            requestContext));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            await AuditFailureAsync(
                currentSession,
                "identity_unlinked",
                identity.Connection,
                requestContext,
                cancellationToken);
            throw IdentityErrors.IdentityConflict();
        }

        telemetry.Record("identity_unlinked", "succeeded", identity.Connection);
    }

    public async Task RevokeSessionAsync(
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        UserSession session = await dbContext.Sessions
            .SingleOrDefaultAsync(
                item => item.Id == currentSession.SessionId && item.UserId == currentSession.UserId,
                cancellationToken)
            ?? throw IdentityErrors.SessionRequired();

        session.Revoke(timeProvider.GetUtcNow());
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            currentSession.UserId,
            currentSession.SessionId,
            "session_revoked",
            "succeeded",
            null,
            requestContext));
        await dbContext.SaveChangesAsync(cancellationToken);
        telemetry.Record("session_revoked", "succeeded");
    }

    private IssuedSession IssueVerifiedSession(Guid userId, DateTimeOffset authenticatedAtUtc)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string token = tokenService.CreateToken();
        UserSession session = new(
            Guid.NewGuid(),
            userId,
            IdentityTokenService.HashToken(token),
            authenticatedAtUtc,
            now.Add(runtimeOptions.SessionLifetime),
            isAuthenticationAssuranceVerified: true);
        dbContext.Sessions.Add(session);
        return new IssuedSession(session.Id, userId, token, session.ExpiresAtUtc);
    }

    private bool IsRecentAuthentication(AuthenticatedSession session) =>
        session.IsAuthenticationAssuranceVerified &&
        session.AuthenticatedAtUtc.Add(runtimeOptions.RecentAuthenticationWindow) > timeProvider.GetUtcNow();

    private async Task AuditFailureAsync(
        AuthenticatedSession session,
        string action,
        string? connection,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            session.UserId,
            session.SessionId,
            action,
            "rejected",
            connection,
            requestContext));
        await dbContext.SaveChangesAsync(cancellationToken);
        telemetry.Record(action, "rejected", connection);
    }

    private IdentitySecurityJournalEntry CreateSecurityJournalEntry(
        Guid? userId,
        Guid? sessionId,
        string action,
        string outcome,
        string? connection,
        IdentityRequestContext requestContext) =>
        new(
            Guid.NewGuid(),
            requestContext.ActorId ?? userId,
            sessionId,
            action,
            outcome,
            connection,
            requestContext.CorrelationId,
            timeProvider.GetUtcNow());

    private void ValidateVerifiedIdentity(VerifiedExternalIdentity identity)
    {
        ValidateConnection(identity.Connection);
        if (string.IsNullOrWhiteSpace(identity.Issuer) ||
            string.IsNullOrWhiteSpace(identity.Subject) ||
            string.IsNullOrWhiteSpace(identity.Label) ||
            string.IsNullOrWhiteSpace(identity.DisplayName) ||
            identity.AuthenticatedAtUtc == default ||
            identity.AuthenticatedAtUtc > timeProvider.GetUtcNow().Add(AuthenticationClockSkew))
        {
            throw IdentityErrors.IdentityNotVerified();
        }
    }

    private static void ValidateConnection(string connection)
    {
        if (!IdentityConnections.IsSupported(connection))
        {
            throw IdentityErrors.InvalidConnection();
        }
    }

    private static bool IsConcurrentExternalIdentityInsert(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_external_identities_Issuer_Subject",
        };

    private sealed record IdentityLinkedEvent(
        Guid UserId,
        Guid IdentityId,
        string Connection,
        DateTimeOffset LinkedAtUtc);
}

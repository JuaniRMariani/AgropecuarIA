using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace AgropecuarIA.Identity.Application;

public sealed class IdentityApplicationService(
    IdentityDbContext dbContext,
    IdentityTokenService tokenService,
    IdentityTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<IdentityRuntimeOptions> options,
    IOptions<OrganizationBootstrapOptions> organizationBootstrapOptions,
    IOrganizationCreationCommitBoundary? organizationCommitBoundary = null,
    IOrganizationCreationRecoveryContextFactory? organizationRecoveryContextFactory = null,
    IOptions<OrganizationOwnerInvitationOptions>? organizationOwnerInvitationOptions = null)
{
    private static readonly TimeSpan AuthenticationClockSkew = TimeSpan.FromMinutes(1);
    private readonly IdentityRuntimeOptions runtimeOptions = options.Value;
    private readonly OrganizationBootstrapOptions bootstrapOptions = organizationBootstrapOptions.Value;
    private readonly OrganizationOwnerInvitationOptions ownerInvitationOptions =
        organizationOwnerInvitationOptions?.Value ?? new OrganizationOwnerInvitationOptions();
    private readonly IOrganizationCreationCommitBoundary commitBoundary =
        organizationCommitBoundary ?? new OrganizationCreationCommitBoundary();
    private readonly IOrganizationCreationRecoveryContextFactory recoveryContextFactory =
        organizationRecoveryContextFactory
        ?? new OrganizationCreationRecoveryContextFactory(dbContext);

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
                session.IsAuthenticationAssuranceVerified,
                session.StrongAuthenticatedAtUtc,
                session.StrongAuthenticationPurpose,
                session.Version))
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

    public async Task<IdentitySessionResult> GetSessionAsync(
        AuthenticatedSession currentSession,
        CancellationToken cancellationToken)
    {
        PlatformUser user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == currentSession.UserId, cancellationToken)
            ?? throw IdentityErrors.SessionRequired();

        LinkedIdentityResult[] identities = await dbContext.ExternalIdentities
            .AsNoTracking()
            .Where(identity => identity.UserId == currentSession.UserId)
            .OrderBy(identity => identity.VerifiedAtUtc)
            .Select(identity => new LinkedIdentityResult(
                identity.Id,
                identity.Connection,
                identity.Label,
                identity.VerifiedAtUtc))
            .ToArrayAsync(cancellationToken);

        MembershipResult[] legacyMemberships = await dbContext.Memberships
            .AsNoTracking()
            .Where(membership => membership.UserId == currentSession.UserId)
            .OrderBy(membership => membership.OrganizationName)
            .ThenBy(membership => membership.OrganizationId)
            .Select(membership => new MembershipResult(
                membership.OrganizationId,
                membership.OrganizationName,
                membership.Role))
            .ToArrayAsync(cancellationToken);

        // N/N-1 readers remain on the legacy projection while creation dual-writes the
        // authoritative membership. Tenant discovery becomes authoritative in its own slice.
        MembershipResult[] memberships = legacyMemberships;

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset? strongExpiresAtUtc = currentSession.StrongAuthenticatedAtUtc?.Add(
            runtimeOptions.StrongAuthenticationWindow);
        bool hasCurrentStrongAssurance =
            currentSession.StrongAuthenticatedAtUtc is not null &&
            StepUpPurposes.IsSupported(currentSession.StrongAuthenticationPurpose ?? string.Empty) &&
            strongExpiresAtUtc > now;

        return new IdentitySessionResult(
            user.Id,
            user.DisplayName,
            identities,
            memberships,
            new AuthenticationAssuranceResult(
                hasCurrentStrongAssurance ? "strong" : "primary",
                currentSession.AuthenticatedAtUtc,
                hasCurrentStrongAssurance ? currentSession.StrongAuthenticationPurpose : null,
                hasCurrentStrongAssurance ? currentSession.StrongAuthenticatedAtUtc : null,
                hasCurrentStrongAssurance ? strongExpiresAtUtc : null));
    }

    public async Task<StartedStepUpAttempt> StartStepUpAttemptAsync(
        AuthenticatedSession currentSession,
        string purpose,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        if (!StepUpPurposes.IsSupported(purpose))
        {
            await AuditFailureAsync(
                currentSession,
                "step_up_started",
                null,
                requestContext,
                cancellationToken,
                purpose);
            throw IdentityErrors.InvalidStepUpPurpose();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        await dbContext.StepUpAttempts
            .Where(attempt =>
                attempt.UserId == currentSession.UserId &&
                attempt.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);
        DateTimeOffset expiresAtUtc = now.Add(runtimeOptions.StepUpAttemptLifetime);
        StepUpAttempt attempt = new(
            Guid.NewGuid(),
            currentSession.UserId,
            currentSession.SessionId,
            purpose,
            now,
            expiresAtUtc);
        dbContext.StepUpAttempts.Add(attempt);
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            currentSession.UserId,
            currentSession.SessionId,
            "step_up_started",
            "succeeded",
            null,
            requestContext));
        await dbContext.SaveChangesAsync(cancellationToken);

        telemetry.Record("step_up_started", "succeeded", purpose: purpose);
        return new StartedStepUpAttempt(
            attempt.Id,
            attempt.Purpose,
            attempt.ExpiresAtUtc,
            $"/api/identity/step-up/{attempt.Id:D}");
    }

    public async Task<StepUpAttemptValidation> ValidateStepUpAttemptAsync(
        Guid attemptId,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        StepUpAttempt? attempt = await dbContext.StepUpAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == attemptId, cancellationToken);
        if (!IsValidStepUpAttempt(attempt, currentSession, timeProvider.GetUtcNow()))
        {
            await AuditFailureAsync(
                currentSession,
                "step_up_validated",
                null,
                requestContext,
                cancellationToken,
                attempt?.Purpose);
            throw IdentityErrors.StepUpAttemptConflict();
        }

        telemetry.Record("step_up_validated", "succeeded", purpose: attempt!.Purpose);
        return new StepUpAttemptValidation(attempt!.Id, attempt.Purpose, attempt.ExpiresAtUtc);
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
        dbContext.OutboxMessages.Add(IdentityOutboxMessage.CreateIdentityLinked(
            new IdentityIntegrationEventEnvelope(
                Guid.NewGuid(),
                requestContext.Scope,
                now,
                now,
                now,
                requestContext.ActorId!.Value,
                requestContext.CorrelationId,
                attempt.Id,
                currentSession.UserId,
                aggregateVersion),
            new IdentityLinkedIntegrationEventPayload(
                currentSession.UserId,
                linkedIdentityId,
                attempt.Connection,
                now)));
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
        return await GetSessionAsync(currentSession, cancellationToken);
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

    public async Task<IssuedSession> CompleteStepUpAttemptAsync(
        Guid attemptId,
        AuthenticatedSession currentSession,
        VerifiedStepUpProof proof,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (!IsValidStrongAuthenticationProof(proof, now))
        {
            string? purpose = await dbContext.StepUpAttempts
                .AsNoTracking()
                .Where(attempt =>
                    attempt.Id == attemptId &&
                    attempt.UserId == currentSession.UserId &&
                    attempt.InitiatingSessionId == currentSession.SessionId)
                .Select(attempt => attempt.Purpose)
                .SingleOrDefaultAsync(cancellationToken);
            await AuditFailureAsync(
                currentSession,
                "step_up_completed",
                null,
                requestContext,
                cancellationToken,
                purpose);
            throw IdentityErrors.StrongAuthenticationRequired();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        StepUpAttempt? attempt = await dbContext.StepUpAttempts
            .SingleOrDefaultAsync(item => item.Id == attemptId, cancellationToken);
        UserSession? initiatingSession = await dbContext.Sessions
            .SingleOrDefaultAsync(
                item => item.Id == currentSession.SessionId &&
                    item.UserId == currentSession.UserId &&
                    item.RevokedAtUtc == null &&
                    item.ExpiresAtUtc > now,
                cancellationToken);

        if (!IsValidStepUpAttempt(attempt, currentSession, now) ||
            initiatingSession is null ||
            proof.AuthenticatedAtUtc < attempt!.StartedAtUtc.Subtract(AuthenticationClockSkew))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            await AuditFailureAsync(
                currentSession,
                "step_up_completed",
                null,
                requestContext,
                cancellationToken,
                attempt?.Purpose);
            throw IdentityErrors.StepUpAttemptConflict();
        }

        bool proofBelongsToUser = await dbContext.ExternalIdentities
            .AsNoTracking()
            .AnyAsync(
                identity => identity.UserId == currentSession.UserId &&
                    identity.Issuer == proof.Issuer &&
                    identity.Subject == proof.Subject,
                cancellationToken);
        if (!proofBelongsToUser)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            await AuditFailureAsync(
                currentSession,
                "step_up_completed",
                null,
                requestContext,
                cancellationToken,
                attempt.Purpose);
            throw IdentityErrors.StepUpAttemptConflict();
        }

        string token = tokenService.CreateToken();
        UserSession rotatedSession = new(
            Guid.NewGuid(),
            currentSession.UserId,
            IdentityTokenService.HashToken(token),
            initiatingSession.AuthenticatedAtUtc,
            initiatingSession.ExpiresAtUtc,
            isAuthenticationAssuranceVerified: true,
            strongAuthenticatedAtUtc: proof.AuthenticatedAtUtc,
            strongAuthenticationPurpose: attempt.Purpose);
        dbContext.Sessions.Add(rotatedSession);
        initiatingSession.Revoke(now);
        attempt.Consume(now);

        PlatformUser user = await dbContext.Users
            .SingleAsync(item => item.Id == currentSession.UserId, cancellationToken);
        long aggregateVersion = user.NextVersion();
        dbContext.OutboxMessages.Add(IdentityOutboxMessage.CreateIdentityStepUpCompleted(
            new IdentityIntegrationEventEnvelope(
                Guid.NewGuid(),
                requestContext.Scope,
                now,
                now,
                now,
                requestContext.ActorId!.Value,
                requestContext.CorrelationId,
                attempt.Id,
                currentSession.UserId,
                aggregateVersion),
            new IdentityStepUpCompletedIntegrationEventPayload(
                currentSession.UserId,
                currentSession.SessionId,
                rotatedSession.Id,
                attempt.Purpose,
                now)));
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            currentSession.UserId,
            currentSession.SessionId,
            "step_up_completed",
            "succeeded",
            null,
            requestContext));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsConcurrentStepUpCompletion(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            await AuditFailureAsync(
                currentSession,
                "step_up_completed",
                null,
                requestContext,
                cancellationToken,
                attempt.Purpose);
            throw IdentityErrors.StepUpAttemptConflict();
        }

        telemetry.Record("step_up_completed", "succeeded", purpose: attempt.Purpose);
        return new IssuedSession(
            rotatedSession.Id,
            rotatedSession.UserId,
            token,
            rotatedSession.ExpiresAtUtc);
    }

    public async Task<CreatedOrganizationResult> CreateOrganizationAsync(
        CreateOrganizationCommand command,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken) =>
        await CreateOrganizationAsync(
            command,
            currentSession,
            requestContext,
            retryAfterUnknownRollback: true,
            cancellationToken);

    private async Task<CreatedOrganizationResult> CreateOrganizationAsync(
        CreateOrganizationCommand command,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        bool retryAfterUnknownRollback,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        using Activity? activity = IdentityTelemetry.Start("identity.organization_create");
        DateTimeOffset now = timeProvider.GetUtcNow();
        string displayName = NormalizeOrganizationDisplayName(command.DisplayName);
        ValidateIdempotencyKey(command.IdempotencyKey);
        Dictionary<string, byte[]> keyRing = GetOrganizationIdempotencyKeyRing();
        Dictionary<string, byte[]> aliases = CreateIdempotencyAliases(command.IdempotencyKey, keyRing);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            await SetOrganizationBootstrapDatabaseContextAsync(
                currentSession.UserId,
                null,
                cancellationToken);
            OrganizationCreationAuthorization authorization =
                await RequireOrganizationCreationAuthorizationAsync(
                    currentSession,
                    now,
                    cancellationToken);
            if (!bootstrapOptions.Enabled)
            {
                telemetry.RecordOrganizationCreate("unavailable");
                throw IdentityErrors.OrganizationCreationUnavailable();
            }

            await RequireRetainedOrganizationKeyCoverageAsync(
                keyRing.Keys,
                cancellationToken);

            byte[] fingerprint = CreateOrganizationRequestFingerprint(displayName, authorization);
            Guid? existingLedgerId = await FindOrganizationCreationLedgerIdAsync(
                aliases,
                cancellationToken);
            if (existingLedgerId is not null)
            {
                CreatedOrganizationResult replay = await ResolveOrganizationCreationReplayAsync(
                    existingLedgerId.Value,
                    authorization,
                    fingerprint,
                    cancellationToken);
                await AddMissingOrganizationCreationAliasesAsync(
                    existingLedgerId.Value,
                    aliases,
                    now,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                telemetry.RecordOrganizationCreate("replayed");
                return replay;
            }

            Guid organizationId = Guid.NewGuid();
            Guid membershipId = Guid.NewGuid();
            Guid ledgerId = Guid.NewGuid();
            Guid leaseOwner = Guid.NewGuid();
            OrganizationDirectoryEntry organization = new(
                organizationId,
                displayName,
                currentSession.UserId,
                now);
            OrganizationMembershipAssignment membership = new(
                membershipId,
                organizationId,
                currentSession.UserId,
                now);
            OrganizationCreationLedger ledger = new(
                ledgerId,
                currentSession.UserId,
                currentSession.SessionId,
                authorization.AuthorizationVersion,
                fingerprint,
                leaseOwner,
                now,
                now.AddMinutes(1));
            await SetOrganizationBootstrapOrganizationContextAsync(
                organizationId,
                cancellationToken);

            dbContext.Organizations.Add(organization);
            dbContext.AuthoritativeMemberships.Add(membership);
            dbContext.Memberships.Add(new OrganizationMembership(
                currentSession.UserId,
                organizationId,
                displayName,
                OrganizationMembershipRoles.Owner));
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.OrganizationCreationLedgers.Add(ledger);
            foreach ((string keyVersion, byte[] keyDigest) in aliases)
            {
                dbContext.OrganizationCreationKeyAliases.Add(new OrganizationCreationKeyAlias(
                    Guid.NewGuid(),
                    ledgerId,
                    keyVersion,
                    keyDigest,
                    now));
            }

            ledger.Complete(
                leaseOwner,
                ledger.FenceToken,
                organizationId,
                membershipId,
                now);
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                currentSession.UserId,
                currentSession.SessionId,
                "organization_created",
                "succeeded",
                null,
                requestContext));
            dbContext.OutboxMessages.Add(IdentityOutboxMessage.CreateOrganizationCreated(
                new IdentityIntegrationEventEnvelope(
                    Guid.NewGuid(),
                    requestContext.Scope,
                    now,
                    now,
                    now,
                    currentSession.UserId,
                    requestContext.CorrelationId,
                    ledgerId,
                    organizationId,
                    1),
                new OrganizationCreatedIntegrationEventPayload(
                    organizationId,
                    membershipId,
                    now)));

            await dbContext.SaveChangesAsync(cancellationToken);
            await commitBoundary.CommitAsync(
                transaction.CommitAsync,
                transaction.RollbackAsync,
                cancellationToken);
            telemetry.RecordOrganizationCreate("succeeded");
            return ToCreatedOrganizationResult(organization, membership);
        }
        catch (DbUpdateException exception) when (IsOrganizationIdempotencyRace(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            CreatedOrganizationResult replay = await ResolveOrganizationCreationRaceAsync(
                currentSession,
                displayName,
                aliases,
                timeProvider.GetUtcNow(),
                missingIsKeyReused: true,
                cancellationToken);
            telemetry.RecordOrganizationCreate("replayed");
            return replay;
        }
        catch (Exception exception) when (IsOrganizationSerializationRace(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            CreatedOrganizationResult replay = await ResolveOrganizationCreationRaceAsync(
                currentSession,
                displayName,
                aliases,
                timeProvider.GetUtcNow(),
                missingIsKeyReused: false,
                cancellationToken);
            telemetry.RecordOrganizationCreate("replayed");
            return replay;
        }
        catch (PostgresException exception) when (IsOrganizationBootstrapRoleUnavailable(exception))
        {
            dbContext.ChangeTracker.Clear();
            telemetry.RecordOrganizationCreate("unavailable");
            throw IdentityErrors.OrganizationCreationUnavailable();
        }
        catch (Exception exception) when (IsIndeterminateCommit(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await RecoverUnknownOrganizationCommitAsync(
                command,
                currentSession,
                requestContext,
                retryAfterUnknownRollback,
                cancellationToken);
        }
        catch (IdentityOperationException exception) when (
            IsOrganizationIdempotencyRejection(exception.Code))
        {
            telemetry.RecordOrganizationCreate(OrganizationIdempotencyOutcome(exception.Code));
            throw;
        }
    }

    public async Task<CreatedOrganizationOwnerInvitationResult> CreateOrganizationOwnerInvitationAsync(
        CreateOrganizationOwnerInvitationCommand command,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequireTenantActor(currentSession.UserId, command.OrganizationId);
        ValidateIdempotencyKey(command.IdempotencyKey);
        using Activity? activity = IdentityTelemetry.Start("identity.organization_owner_invitation_create");
        DateTimeOffset now = timeProvider.GetUtcNow();
        Dictionary<string, byte[]> keyRing = GetOwnerInvitationKeyRing();
        Dictionary<string, byte[]> creationAliases = CreateOwnerInvitationDigests(
            "create",
            command.OrganizationId,
            command.IdempotencyKey,
            keyRing);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            await SetOwnerInvitationDatabaseContextAsync(
                currentSession.UserId,
                command.OrganizationId,
                cancellationToken);
            await RequireOrganizationOwnerAuthorizationAsync(
                command.OrganizationId,
                currentSession,
                now,
                requireStrongAuthentication: true,
                cancellationToken);
            await LockOwnerManagementOrganizationAsync(cancellationToken);
            EnsureOwnerInvitationsEnabled(
                keyRing,
                "organization_owner_invitation_create");
            await RequireRetainedOwnerInvitationKeyCoverageAsync(
                keyRing.Keys,
                "organization_owner_invitation_create",
                cancellationToken);

            OrganizationOwnerInvitation? replay = await FindInvitationByCreationKeyAsync(
                command.OrganizationId,
                creationAliases,
                cancellationToken);
            if (replay is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                telemetry.RecordOrganizationOwnerInvitation(
                    "organization_owner_invitation_create",
                    "replayed");
                return ToCreatedOwnerInvitationResult(replay, token: null, isReplay: true, now);
            }

            string token = tokenService.CreateToken();
            string currentKeyVersion = ownerInvitationOptions.CurrentKeyVersion;
            byte[] currentKey = keyRing[currentKeyVersion];
            OrganizationOwnerInvitation invitation = new(
                Guid.NewGuid(),
                command.OrganizationId,
                currentSession.UserId,
                currentSession.SessionId,
                currentKeyVersion,
                creationAliases[currentKeyVersion],
                currentKeyVersion,
                CreateOwnerInvitationTokenDigest(token, currentKey),
                now,
                now.Add(ownerInvitationOptions.Lifetime));
            dbContext.OrganizationOwnerInvitations.Add(invitation);
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                currentSession.UserId,
                currentSession.SessionId,
                "organization_owner_invitation_created",
                "succeeded",
                null,
                requestContext));
            dbContext.OutboxMessages.Add(IdentityOutboxMessage.CreateOrganizationOwnerInvited(
                new IdentityIntegrationEventEnvelope(
                    Guid.NewGuid(),
                    requestContext.Scope,
                    now,
                    now,
                    now,
                    currentSession.UserId,
                    requestContext.CorrelationId,
                    null,
                    invitation.Id,
                    1),
                new OrganizationOwnerInvitedIntegrationEventPayload(
                    invitation.OrganizationId,
                    invitation.Id,
                    invitation.ExpiresAtUtc,
                    now)));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_create",
                "succeeded");
            return ToCreatedOwnerInvitationResult(invitation, token, isReplay: false, now);
        }
        catch (DbUpdateException exception) when (IsOwnerInvitationCreationRace(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            CreatedOrganizationOwnerInvitationResult replay =
                await ResolveOwnerInvitationCreationRaceAsync(
                    command.OrganizationId,
                    currentSession,
                    requestContext,
                    creationAliases,
                    cancellationToken);
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_create",
                "replayed");
            return replay;
        }
        catch (Exception exception) when (IsOrganizationSerializationRace(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            CreatedOrganizationOwnerInvitationResult replay =
                await ResolveOwnerInvitationCreationRaceAsync(
                    command.OrganizationId,
                    currentSession,
                    requestContext,
                    creationAliases,
                    cancellationToken);
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_create",
                "replayed");
            return replay;
        }
        catch (IdentityOperationException)
        {
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_create",
                "rejected");
            throw;
        }
    }

    public async Task<IReadOnlyList<OrganizationOwnerInvitationSummaryResult>>
        ListOrganizationOwnerInvitationsAsync(
            Guid organizationId,
            AuthenticatedSession currentSession,
            IdentityRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        requestContext.RequireTenantActor(currentSession.UserId, organizationId);
        DateTimeOffset now = timeProvider.GetUtcNow();
        Dictionary<string, byte[]> keyRing = GetOwnerInvitationKeyRing();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetOwnerInvitationDatabaseContextAsync(
            currentSession.UserId,
            organizationId,
            cancellationToken);
        await RequireOrganizationOwnerAuthorizationAsync(
            organizationId,
            currentSession,
            now,
            requireStrongAuthentication: false,
            cancellationToken);
        EnsureOwnerInvitationsEnabled(
            keyRing,
            "organization_owner_invitation_list");
        await RequireRetainedOwnerInvitationKeyCoverageAsync(
            keyRing.Keys,
            "organization_owner_invitation_list",
            cancellationToken);
        OrganizationOwnerInvitation[] invitations = await dbContext.OrganizationOwnerInvitations
            .AsNoTracking()
            .Where(invitation => invitation.OrganizationId == organizationId)
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .ThenBy(invitation => invitation.Id)
            .ToArrayAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        telemetry.RecordOrganizationOwnerInvitation(
            "organization_owner_invitation_list",
            "succeeded");
        return invitations
            .Select(invitation => ToOwnerInvitationSummary(invitation, now))
            .ToArray();
    }

    public async Task<OrganizationOwnerInvitationSummaryResult> RevokeOrganizationOwnerInvitationAsync(
        RevokeOrganizationOwnerInvitationCommand command,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequireTenantActor(currentSession.UserId, command.OrganizationId);
        DateTimeOffset now = timeProvider.GetUtcNow();
        Dictionary<string, byte[]> keyRing = GetOwnerInvitationKeyRing();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            await SetOwnerInvitationDatabaseContextAsync(
                currentSession.UserId,
                command.OrganizationId,
                cancellationToken);
            await RequireOrganizationOwnerAuthorizationAsync(
                command.OrganizationId,
                currentSession,
                now,
                requireStrongAuthentication: true,
                cancellationToken);
            EnsureOwnerInvitationsEnabled(
                keyRing,
                "organization_owner_invitation_revoke");
            await RequireRetainedOwnerInvitationKeyCoverageAsync(
                keyRing.Keys,
                "organization_owner_invitation_revoke",
                cancellationToken);
            OrganizationOwnerInvitation invitation = await dbContext.OrganizationOwnerInvitations
                .SingleOrDefaultAsync(
                    item => item.Id == command.InvitationId &&
                        item.OrganizationId == command.OrganizationId,
                    cancellationToken)
                ?? throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
            if (invitation.Version != command.ExpectedVersion)
            {
                throw IdentityErrors.OrganizationOwnerInvitationVersionMismatch();
            }

            invitation.Revoke(currentSession.UserId, now);
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                currentSession.UserId,
                currentSession.SessionId,
                "organization_owner_invitation_revoked",
                "succeeded",
                null,
                requestContext));
            dbContext.OutboxMessages.Add(IdentityOutboxMessage.CreateOrganizationOwnerInvitationRevoked(
                new IdentityIntegrationEventEnvelope(
                    Guid.NewGuid(),
                    requestContext.Scope,
                    now,
                    now,
                    now,
                    currentSession.UserId,
                    requestContext.CorrelationId,
                    invitation.Id,
                    invitation.Id,
                    2),
                new OrganizationOwnerInvitationRevokedIntegrationEventPayload(
                    invitation.OrganizationId,
                    invitation.Id,
                    now)));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_revoke",
                "succeeded");
            return ToOwnerInvitationSummary(invitation, now);
        }
        catch (InvalidOperationException)
        {
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_revoke",
                "conflict");
            throw IdentityErrors.OrganizationOwnerInvitationConflict();
        }
        catch (DbUpdateConcurrencyException)
        {
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_revoke",
                "conflict");
            throw IdentityErrors.OrganizationOwnerInvitationConflict();
        }
    }

    public async Task<IReadOnlyList<OrganizationOwnerMembershipSummaryResult>>
        ListOrganizationOwnerMembershipsAsync(
            Guid organizationId,
            AuthenticatedSession currentSession,
            IdentityRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        requestContext.RequireTenantActor(currentSession.UserId, organizationId);
        DateTimeOffset now = timeProvider.GetUtcNow();
        using Activity? activity = IdentityTelemetry.Start("identity.organization_owner_membership_list");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            await SetOwnerInvitationDatabaseContextAsync(
                currentSession.UserId,
                organizationId,
                cancellationToken);
            await RequireOrganizationOwnerMembershipAuthorizationAsync(
                organizationId,
                currentSession,
                now,
                requireStrongAuthentication: false,
                cancellationToken);
            OwnerMembershipDatabaseResult[] memberships =
                await LoadActiveOwnerMembershipsAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_list",
                "succeeded");
            return memberships
                .Select(item => ToOwnerMembershipSummary(
                    item,
                    organizationId))
                .ToArray();
        }
        catch (IdentityOperationException)
        {
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_list",
                "rejected");
            throw;
        }
        catch (PostgresException exception) when (IsOrganizationBootstrapRoleUnavailable(exception))
        {
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_list",
                "unavailable");
            throw IdentityErrors.OrganizationOwnerMembershipUnavailable();
        }
    }

    public async Task<RemovedOrganizationOwnerMembershipResult>
        RemoveOrganizationOwnerMembershipAsync(
            RemoveOrganizationOwnerMembershipCommand command,
            AuthenticatedSession currentSession,
            IdentityRequestContext requestContext,
            CancellationToken cancellationToken) =>
        await RemoveOrganizationOwnerMembershipAsync(
            command,
            currentSession,
            requestContext,
            retryAfterSerialization: true,
            cancellationToken);

    private async Task<RemovedOrganizationOwnerMembershipResult>
        RemoveOrganizationOwnerMembershipAsync(
            RemoveOrganizationOwnerMembershipCommand command,
            AuthenticatedSession currentSession,
            IdentityRequestContext requestContext,
            bool retryAfterSerialization,
            CancellationToken cancellationToken)
    {
        requestContext.RequireTenantActor(currentSession.UserId, command.OrganizationId);
        ValidateIdempotencyKey(command.IdempotencyKey);
        using Activity? activity = IdentityTelemetry.Start("identity.organization_owner_membership_remove");
        DateTimeOffset now = TruncateToPostgreSqlPrecision(timeProvider.GetUtcNow());
        Dictionary<string, byte[]> keyRing = GetOwnerRemovalIdempotencyKeyRing();
        Dictionary<string, byte[]> aliases = CreateIdempotencyAliases(
            command.IdempotencyKey,
            keyRing);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            await SetOwnerInvitationDatabaseContextAsync(
                currentSession.UserId,
                command.OrganizationId,
                cancellationToken);
            OwnerInvitationSessionAuthorization authorization =
                await RequireOrganizationOwnerMembershipAuthorizationAsync(
                    command.OrganizationId,
                    currentSession,
                    now,
                    requireStrongAuthentication: true,
                    cancellationToken);
            await SetOwnerRemovalAuthorizationContextAsync(
                currentSession.SessionId,
                authorization.Version,
                cancellationToken);
            await RequireRetainedOwnerRemovalKeyCoverageAsync(
                keyRing.Keys,
                cancellationToken);
            byte[] fingerprint = CreateOwnerRemovalRequestFingerprint(command);
            Guid? existingLedgerId = await FindOwnerRemovalLedgerIdAsync(
                command.OrganizationId,
                aliases,
                cancellationToken);
            if (existingLedgerId is not null)
            {
                RemovedOrganizationOwnerMembershipResult replay =
                    await ResolveOwnerRemovalReplayAsync(
                        existingLedgerId.Value,
                        authorization,
                        fingerprint,
                        currentSession.UserId,
                        currentSession.SessionId,
                        cancellationToken);
                await AddMissingOwnerRemovalAliasesAsync(
                    existingLedgerId.Value,
                    command.OrganizationId,
                    aliases,
                    now,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                telemetry.RecordOrganizationOwnerMembership(
                    "organization_owner_membership_remove",
                    "replayed");
                return replay;
            }

            OwnerMembershipDatabaseResult? target = (await LoadActiveOwnerMembershipsAsync(
                cancellationToken)).SingleOrDefault(item => item.MembershipId == command.MembershipId);
            if (target is null || target.IsCurrentUser)
            {
                throw IdentityErrors.OrganizationOwnerMembershipNotAvailable();
            }

            Guid ledgerId = Guid.NewGuid();
            Guid leaseOwner = Guid.NewGuid();
            Guid newMembershipVersion = Guid.NewGuid();
            OrganizationOwnerRemovalLedger ledger = new(
                ledgerId,
                command.OrganizationId,
                currentSession.UserId,
                currentSession.SessionId,
                authorization.Version,
                command.MembershipId,
                command.ExpectedVersion,
                fingerprint,
                leaseOwner,
                now,
                now.AddMinutes(1));
            dbContext.OrganizationOwnerRemovalLedgers.Add(ledger);
            foreach ((string keyVersion, byte[] keyDigest) in aliases)
            {
                dbContext.OrganizationOwnerRemovalKeyAliases.Add(
                    new OrganizationOwnerRemovalKeyAlias(
                        Guid.NewGuid(),
                        ledgerId,
                        command.OrganizationId,
                        keyVersion,
                        keyDigest,
                        now));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            OwnerRemovalDatabaseResult databaseResult = await RemoveActiveOwnerAsync(
                command.MembershipId,
                command.ExpectedVersion,
                now,
                newMembershipVersion,
                cancellationToken);
            ThrowForOwnerRemovalOutcome(databaseResult.Outcome);
            ValidateRemovedOwnerDatabaseResult(databaseResult, command, newMembershipVersion);

            ledger.Complete(
                leaseOwner,
                ledger.FenceToken,
                databaseResult.Version!.Value,
                databaseResult.SecurityVersion!.Value,
                databaseResult.RemovedAtUtc!.Value,
                now);
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                currentSession.UserId,
                currentSession.SessionId,
                "organization_owner_membership_removed",
                "succeeded",
                null,
                requestContext));
            dbContext.OutboxMessages.Add(IdentityOutboxMessage.CreateOrganizationOwnerMembershipRemoved(
                new IdentityIntegrationEventEnvelope(
                    Guid.NewGuid(),
                    requestContext.Scope,
                    now,
                    now,
                    now,
                    currentSession.UserId,
                    requestContext.CorrelationId,
                    ledgerId,
                    command.MembershipId,
                    databaseResult.SecurityVersion.Value),
                new OrganizationOwnerMembershipRemovedIntegrationEventPayload(
                    command.OrganizationId,
                    command.MembershipId,
                    databaseResult.SecurityVersion.Value,
                    databaseResult.RevokedInvitationIds.Length,
                    databaseResult.RemovedAtUtc.Value)));
            await dbContext.SaveChangesAsync(cancellationToken);
            await commitBoundary.CommitAsync(
                transaction.CommitAsync,
                transaction.RollbackAsync,
                cancellationToken);
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                "succeeded");
            return ToRemovedOwnerMembershipResult(
                databaseResult,
                command.OrganizationId,
                isReplay: false);
        }
        catch (DbUpdateException exception) when (IsOwnerRemovalIdempotencyRace(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            RemovedOrganizationOwnerMembershipResult replay =
                await ResolveOwnerRemovalRaceAsync(
                    command,
                    currentSession,
                    aliases,
                    missingIsConflict: true,
                    cancellationToken);
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                "replayed");
            return replay;
        }
        catch (Exception exception) when (IsOrganizationSerializationRace(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            RemovedOrganizationOwnerMembershipResult? replay =
                await TryResolveOwnerRemovalRaceAsync(
                    command,
                    currentSession,
                    aliases,
                    cancellationToken);
            if (replay is not null)
            {
                telemetry.RecordOrganizationOwnerMembership(
                    "organization_owner_membership_remove",
                    "replayed");
                return replay;
            }

            if (retryAfterSerialization)
            {
                return await RemoveOrganizationOwnerMembershipAsync(
                    command,
                    currentSession,
                    requestContext,
                    retryAfterSerialization: false,
                    cancellationToken);
            }

            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                "conflict");
            throw IdentityErrors.OrganizationOwnerMembershipConflict();
        }
        catch (Exception exception) when (IsIndeterminateCommit(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await RecoverUnknownOwnerRemovalCommitAsync(
                command,
                currentSession,
                aliases,
                cancellationToken);
        }
        catch (IdentityOperationException exception) when (
            IsOrganizationIdempotencyRejection(exception.Code))
        {
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                OwnerRemovalIdempotencyOutcome(exception.Code));
            throw;
        }
        catch (PostgresException exception) when (IsOrganizationBootstrapRoleUnavailable(exception))
        {
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                "unavailable");
            throw IdentityErrors.OrganizationOwnerMembershipUnavailable();
        }
        catch (IdentityOperationException)
        {
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                "rejected");
            throw;
        }
    }

    public async Task<AcceptedOrganizationOwnerInvitationResult>
        AcceptOrganizationOwnerInvitationAsync(
            AcceptOrganizationOwnerInvitationCommand command,
            AuthenticatedSession currentSession,
            IdentityRequestContext requestContext,
            CancellationToken cancellationToken) =>
        await AcceptOrganizationOwnerInvitationAsync(
            command,
            currentSession,
            requestContext,
            retryAfterRace: true,
            cancellationToken);

    private async Task<AcceptedOrganizationOwnerInvitationResult>
        AcceptOrganizationOwnerInvitationAsync(
            AcceptOrganizationOwnerInvitationCommand command,
            AuthenticatedSession currentSession,
            IdentityRequestContext requestContext,
            bool retryAfterRace,
            CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        ValidateOwnerInvitationToken(command.Token);
        DateTimeOffset now = timeProvider.GetUtcNow();
        Dictionary<string, byte[]> keyRing = GetOwnerInvitationKeyRing();
        EnsureOwnerInvitationsEnabled(
            keyRing,
            "organization_owner_invitation_accept");
        using Activity? activity = IdentityTelemetry.Start("identity.organization_owner_invitation_accept");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            await SetOwnerInvitationDatabaseContextAsync(
                currentSession.UserId,
                organizationId: null,
                cancellationToken);
            await RequireRecentVerifiedSessionAsync(currentSession, now, cancellationToken);
            await RequireRetainedOwnerInvitationKeyCoverageAsync(
                keyRing.Keys,
                "organization_owner_invitation_accept",
                cancellationToken);
            OrganizationOwnerInvitation invitation =
                await FindInvitationByTokenAsync(command.Token, keyRing, cancellationToken)
                ?? throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
            await SetOwnerInvitationTenantContextAsync(invitation.OrganizationId, cancellationToken);
            bool acceptanceLockAcquired = await dbContext.Database
                .SqlQueryRaw<bool>(
                    "SELECT identity.lock_owner_invitation_acceptance_organization({0}) AS \"Value\"",
                    invitation.Id)
                .SingleAsync(cancellationToken);
            if (!acceptanceLockAcquired)
            {
                throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
            }

            if (invitation.Status == OrganizationOwnerInvitationStatuses.Accepted)
            {
                AcceptedOrganizationOwnerInvitationResult replay =
                    await ResolveAcceptedOwnerInvitationReplayAsync(
                        invitation,
                        currentSession.UserId,
                        cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                telemetry.RecordOrganizationOwnerInvitation(
                    "organization_owner_invitation_accept",
                    "replayed");
                return replay;
            }

            if (invitation.Status != OrganizationOwnerInvitationStatuses.Pending ||
                invitation.ExpiresAtUtc <= now)
            {
                throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
            }

            OrganizationDirectoryEntry organization = await dbContext.Organizations
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == invitation.OrganizationId &&
                        item.Status == OrganizationStatuses.Active,
                    cancellationToken)
                ?? throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
            OrganizationMembershipAssignment? membership = await dbContext.AuthoritativeMemberships
                .SingleOrDefaultAsync(
                    item => item.OrganizationId == invitation.OrganizationId &&
                        item.UserId == currentSession.UserId,
                    cancellationToken);
            if (membership is null)
            {
                membership = new OrganizationMembershipAssignment(
                    Guid.NewGuid(),
                    invitation.OrganizationId,
                    currentSession.UserId,
                    now);
                dbContext.AuthoritativeMemberships.Add(membership);
                dbContext.Memberships.Add(new OrganizationMembership(
                    currentSession.UserId,
                    invitation.OrganizationId,
                    organization.DisplayName,
                    OrganizationMembershipRoles.Owner));
            }
            else if (membership.Status != OrganizationMembershipStatuses.Active ||
                membership.Role != OrganizationMembershipRoles.Owner)
            {
                throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
            }

            invitation.Accept(currentSession.UserId, membership.Id, now);
            IdentityRequestContext tenantRequestContext = IdentityRequestContext.ForTenant(
                requestContext.CorrelationId,
                currentSession.UserId,
                invitation.OrganizationId);
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                currentSession.UserId,
                currentSession.SessionId,
                "organization_owner_invitation_accepted",
                "succeeded",
                null,
                tenantRequestContext));
            dbContext.OutboxMessages.Add(IdentityOutboxMessage.CreateOrganizationOwnerInvitationAccepted(
                new IdentityIntegrationEventEnvelope(
                    Guid.NewGuid(),
                    tenantRequestContext.Scope,
                    now,
                    now,
                    now,
                    currentSession.UserId,
                    tenantRequestContext.CorrelationId,
                    invitation.Id,
                    invitation.Id,
                    2),
                new OrganizationOwnerInvitationAcceptedIntegrationEventPayload(
                    invitation.OrganizationId,
                    invitation.Id,
                    membership.Id,
                    now)));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_accept",
                "succeeded");
            return ToAcceptedOwnerInvitationResult(
                invitation,
                organization,
                membership,
                isReplay: false);
        }
        catch (Exception exception) when (
            retryAfterRace &&
            IsSafeOwnerInvitationAcceptanceRace(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
            return await AcceptOrganizationOwnerInvitationAsync(
                command,
                currentSession,
                requestContext,
                retryAfterRace: false,
                cancellationToken);
        }
        catch (Exception exception) when (IsSafeOwnerInvitationAcceptanceRace(exception))
        {
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_accept",
                "conflict");
            throw IdentityErrors.OrganizationOwnerInvitationConflict();
        }
        catch (InvalidOperationException)
        {
            telemetry.RecordOrganizationOwnerInvitation(
                "organization_owner_invitation_accept",
                "conflict");
            throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
        }
    }

    public async Task RevokeSessionAsync(
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await RevokeSessionCoreAsync(
                currentSession,
                requestContext,
                cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            IsSessionManagementInfrastructureFailure(exception))
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    private async Task RevokeSessionCoreAsync(
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        RequireSessionManagementContext(currentSession);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetSessionManagementDatabaseContextAsync(currentSession, cancellationToken);
        RevokeCurrentOwnSessionDatabaseResult result = await dbContext.Database
            .SqlQueryRaw<RevokeCurrentOwnSessionDatabaseResult>(
                """
                SELECT outcome AS "Outcome",
                       session_id AS "SessionId",
                       revoked_at_utc AS "RevokedAtUtc",
                       version AS "Version"
                FROM identity.revoke_current_own_session()
                """)
            .SingleAsync(cancellationToken);

        switch (result.Outcome)
        {
            case "revoked":
                ValidateCurrentSessionRevocationResult(result, currentSession.SessionId);
                dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                    currentSession.UserId,
                    currentSession.SessionId,
                    "session_revoked",
                    "succeeded",
                    null,
                    requestContext));
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                telemetry.Record("session_revoked", "succeeded");
                return;
            case "already_revoked":
                ValidateCurrentSessionRevocationResult(result, currentSession.SessionId);
                await transaction.CommitAsync(cancellationToken);
                telemetry.Record("session_revoked", "replayed");
                return;
            case "not_available" when IsEmptySessionManagementResult(result):
                throw IdentityErrors.SessionManagementUnavailable();
            default:
                throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    public async Task<OwnSessionPageResult> ListOwnActiveSessionsAsync(
        AuthenticatedSession currentSession,
        int offset,
        int limit,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ListOwnActiveSessionsCoreAsync(
                currentSession,
                offset,
                limit,
                requestContext,
                cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            IsSessionManagementInfrastructureFailure(exception))
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    private async Task<OwnSessionPageResult> ListOwnActiveSessionsCoreAsync(
        AuthenticatedSession currentSession,
        int offset,
        int limit,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        if (offset < 0 || limit is < 1 or > 50)
        {
            throw IdentityErrors.InvalidSessionPage();
        }

        RequireSessionManagementContext(currentSession);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        await SetSessionManagementDatabaseContextAsync(currentSession, cancellationToken);

        long total = await dbContext.Database
            .SqlQueryRaw<long>(
                "SELECT identity.count_own_active_sessions() AS \"Value\"")
            .SingleAsync(cancellationToken);
        OwnSessionDatabaseResult[] rows = await dbContext.Database
            .SqlQueryRaw<OwnSessionDatabaseResult>(
                """
                SELECT session_id AS "SessionId",
                       authenticated_at_utc AS "AuthenticatedAtUtc",
                       expires_at_utc AS "ExpiresAtUtc",
                       version AS "Version",
                       is_current AS "IsCurrent",
                       total_count AS "TotalCount"
                FROM identity.list_own_active_sessions({0}, {1})
                """,
                offset,
                limit)
            .ToArrayAsync(cancellationToken);

        if (total < 1 ||
            rows.Length > limit ||
            (offset < total && rows.Length == 0) ||
            rows.Any(row => row.TotalCount != total))
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }

        await transaction.CommitAsync(cancellationToken);
        return new OwnSessionPageResult(
            rows.Select(row => new OwnSessionSummaryResult(
                    row.SessionId,
                    row.AuthenticatedAtUtc,
                    row.ExpiresAtUtc,
                    row.IsCurrent,
                    row.Version))
                .ToArray(),
            total,
            offset,
            limit);
    }

    public async Task RevokeOtherOwnSessionAsync(
        RevokeOwnSessionCommand command,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await RevokeOtherOwnSessionCoreAsync(
                command,
                currentSession,
                requestContext,
                cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            IsSessionManagementInfrastructureFailure(exception))
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    public async Task RevokeAllOtherOwnSessionsAsync(
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await RevokeAllOtherOwnSessionsCoreAsync(
                currentSession,
                requestContext,
                cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            IsSessionManagementInfrastructureFailure(exception))
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    public async Task RevokeAllOwnSessionsAsync(
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await RevokeAllOwnSessionsCoreAsync(
                currentSession,
                requestContext,
                cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            IsSessionManagementInfrastructureFailure(exception))
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    private async Task RevokeAllOwnSessionsCoreAsync(
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        RequireSessionManagementContext(currentSession);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (!HasCurrentSessionManagementAssurance(currentSession, now))
        {
            throw IdentityErrors.StrongAuthenticationRequired();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetSessionManagementDatabaseContextAsync(currentSession, cancellationToken);
        RevokeAllOwnSessionsDatabaseResult[] rows = await dbContext.Database
            .SqlQueryRaw<RevokeAllOwnSessionsDatabaseResult>(
                """
                SELECT outcome AS "Outcome",
                       session_id AS "SessionId",
                       revoked_at_utc AS "RevokedAtUtc",
                       version AS "Version"
                FROM identity.revoke_all_own_sessions({0})
                """,
                now.Subtract(runtimeOptions.StrongAuthenticationWindow))
            .ToArrayAsync(cancellationToken);

        if (rows.Length == 1 && IsEmptySessionManagementResult(rows[0]))
        {
            switch (rows[0].Outcome)
            {
                case "no_sessions":
                    await transaction.CommitAsync(cancellationToken);
                    telemetry.Record(
                        "session_revoke_all",
                        "replayed",
                        purpose: StepUpPurposes.ManageSessions);
                    return;
                case "strong_authentication_required":
                    throw IdentityErrors.StrongAuthenticationRequired();
                case "not_available":
                    throw IdentityErrors.SessionManagementUnavailable();
            }
        }

        DateTimeOffset? revokedAtUtc = null;
        HashSet<Guid> revokedSessionIds = [];
        foreach (RevokeAllOwnSessionsDatabaseResult row in rows)
        {
            if (row.Outcome != "revoked" ||
                row.SessionId is not Guid sessionId ||
                sessionId == Guid.Empty ||
                row.RevokedAtUtc is not DateTimeOffset rowRevokedAtUtc ||
                row.Version is not Guid version ||
                version == Guid.Empty ||
                !revokedSessionIds.Add(sessionId) ||
                (revokedAtUtc is not null && revokedAtUtc != rowRevokedAtUtc))
            {
                throw IdentityErrors.SessionManagementUnavailable();
            }

            revokedAtUtc ??= rowRevokedAtUtc;
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                currentSession.UserId,
                sessionId,
                "session_revoked",
                "succeeded",
                null,
                requestContext));
        }

        if (!revokedSessionIds.Contains(currentSession.SessionId))
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        telemetry.Record(
            "session_revoke_all",
            "succeeded",
            purpose: StepUpPurposes.ManageSessions);
    }

    private async Task RevokeAllOtherOwnSessionsCoreAsync(
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        RequireSessionManagementContext(currentSession);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (!HasCurrentSessionManagementAssurance(currentSession, now))
        {
            throw IdentityErrors.StrongAuthenticationRequired();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetSessionManagementDatabaseContextAsync(currentSession, cancellationToken);
        RevokeAllOtherOwnSessionsDatabaseResult[] rows = await dbContext.Database
            .SqlQueryRaw<RevokeAllOtherOwnSessionsDatabaseResult>(
                """
                SELECT outcome AS "Outcome",
                       session_id AS "SessionId",
                       revoked_at_utc AS "RevokedAtUtc",
                       version AS "Version"
                FROM identity.revoke_all_other_own_sessions({0})
                """,
                now.Subtract(runtimeOptions.StrongAuthenticationWindow))
            .ToArrayAsync(cancellationToken);

        if (rows.Length == 1 && IsEmptySessionManagementResult(rows[0]))
        {
            switch (rows[0].Outcome)
            {
                case "no_sessions":
                    await transaction.CommitAsync(cancellationToken);
                    telemetry.Record(
                        "session_revoke_all_others",
                        "replayed",
                        purpose: StepUpPurposes.ManageSessions);
                    return;
                case "strong_authentication_required":
                    throw IdentityErrors.StrongAuthenticationRequired();
                case "not_available":
                    throw IdentityErrors.SessionManagementUnavailable();
            }
        }

        DateTimeOffset? revokedAtUtc = null;
        HashSet<Guid> revokedSessionIds = [];
        foreach (RevokeAllOtherOwnSessionsDatabaseResult row in rows)
        {
            if (row.Outcome != "revoked" ||
                row.SessionId is not Guid sessionId ||
                sessionId == Guid.Empty ||
                sessionId == currentSession.SessionId ||
                row.RevokedAtUtc is not DateTimeOffset rowRevokedAtUtc ||
                row.Version is not Guid version ||
                version == Guid.Empty ||
                !revokedSessionIds.Add(sessionId) ||
                (revokedAtUtc is not null && revokedAtUtc != rowRevokedAtUtc))
            {
                throw IdentityErrors.SessionManagementUnavailable();
            }

            revokedAtUtc ??= rowRevokedAtUtc;
            dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                currentSession.UserId,
                sessionId,
                "session_revoked",
                "succeeded",
                null,
                requestContext));
        }

        if (revokedSessionIds.Count == 0)
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        telemetry.Record(
            "session_revoke_all_others",
            "succeeded",
            purpose: StepUpPurposes.ManageSessions);
    }

    private static bool IsEmptySessionManagementResult(
        RevokeAllOtherOwnSessionsDatabaseResult result) =>
        result.SessionId is null &&
        result.RevokedAtUtc is null &&
        result.Version is null;

    private static bool IsEmptySessionManagementResult(
        RevokeAllOwnSessionsDatabaseResult result) =>
        result.SessionId is null &&
        result.RevokedAtUtc is null &&
        result.Version is null;

    private static bool IsEmptySessionManagementResult(
        RevokeCurrentOwnSessionDatabaseResult result) =>
        result.SessionId is null &&
        result.RevokedAtUtc is null &&
        result.Version is null;

    private static void ValidateCurrentSessionRevocationResult(
        RevokeCurrentOwnSessionDatabaseResult result,
        Guid currentSessionId)
    {
        if (result.SessionId != currentSessionId ||
            result.RevokedAtUtc is null ||
            result.Version is not Guid version ||
            version == Guid.Empty)
        {
            throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    private async Task RevokeOtherOwnSessionCoreAsync(
        RevokeOwnSessionCommand command,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        requestContext.RequirePlatformActor(currentSession.UserId);
        if (command.SessionId == Guid.Empty)
        {
            throw IdentityErrors.SessionNotAvailable();
        }

        if (command.ExpectedVersion == Guid.Empty)
        {
            throw IdentityErrors.InvalidSessionVersion();
        }

        if (command.SessionId == currentSession.SessionId)
        {
            throw IdentityErrors.CurrentSessionRequiresLogout();
        }

        RequireSessionManagementContext(currentSession);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (!HasCurrentSessionManagementAssurance(currentSession, now))
        {
            throw IdentityErrors.StrongAuthenticationRequired();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetSessionManagementDatabaseContextAsync(currentSession, cancellationToken);
        RevokeOwnSessionDatabaseResult result = await dbContext.Database
            .SqlQueryRaw<RevokeOwnSessionDatabaseResult>(
                """
                SELECT outcome AS "Outcome",
                       session_id AS "SessionId",
                       revoked_at_utc AS "RevokedAtUtc",
                       version AS "Version"
                FROM identity.revoke_other_own_session({0}, {1}, {2}, {3})
                """,
                command.SessionId,
                command.ExpectedVersion,
                now.Subtract(runtimeOptions.StrongAuthenticationWindow),
                Guid.NewGuid())
            .SingleAsync(cancellationToken);

        switch (result.Outcome)
        {
            case "revoked":
                if (result.SessionId != command.SessionId ||
                    result.RevokedAtUtc is null ||
                    result.Version is not Guid resultVersion ||
                    resultVersion == Guid.Empty)
                {
                    throw IdentityErrors.SessionManagementUnavailable();
                }

                dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
                    currentSession.UserId,
                    command.SessionId,
                    "session_revoked",
                    "succeeded",
                    null,
                    requestContext));
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                telemetry.Record(
                    "session_revoked",
                    "succeeded",
                    purpose: StepUpPurposes.ManageSessions);
                return;
            case "already_revoked":
                await transaction.CommitAsync(cancellationToken);
                telemetry.Record(
                    "session_revoked",
                    "replayed",
                    purpose: StepUpPurposes.ManageSessions);
                return;
            case "current_session":
                throw IdentityErrors.CurrentSessionRequiresLogout();
            case "version_mismatch":
                throw IdentityErrors.SessionVersionMismatch();
            case "strong_authentication_required":
                throw IdentityErrors.StrongAuthenticationRequired();
            case "not_available":
                throw IdentityErrors.SessionNotAvailable();
            default:
                throw IdentityErrors.SessionManagementUnavailable();
        }
    }

    private static void RequireSessionManagementContext(AuthenticatedSession currentSession)
    {
        if (currentSession.SessionId == Guid.Empty ||
            currentSession.UserId == Guid.Empty ||
            currentSession.Version == Guid.Empty)
        {
            throw IdentityErrors.SessionRequired();
        }
    }

    private static bool IsSessionManagementInfrastructureFailure(Exception exception) =>
        exception is InvalidOperationException or DbUpdateException or NpgsqlException;

    private bool HasCurrentSessionManagementAssurance(
        AuthenticatedSession currentSession,
        DateTimeOffset now)
    {
        if (!currentSession.IsAuthenticationAssuranceVerified ||
            currentSession.StrongAuthenticatedAtUtc is not DateTimeOffset strongAuthenticatedAtUtc ||
            currentSession.StrongAuthenticationPurpose != StepUpPurposes.ManageSessions)
        {
            return false;
        }

        TimeSpan authenticationAge = now - strongAuthenticatedAtUtc;
        return authenticationAge >= TimeSpan.Zero &&
            authenticationAge < runtimeOptions.StrongAuthenticationWindow;
    }

    private async Task SetSessionManagementDatabaseContextAsync(
        AuthenticatedSession currentSession,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "SET LOCAL ROLE agro_identity_app",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_actor_id', {currentSession.UserId.ToString("D")}, true)",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_scope_kind', {"platform"}, true)",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_session_id', {currentSession.SessionId.ToString("D")}, true)",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_authorization_version', {currentSession.Version.ToString("D")}, true)",
            cancellationToken);
    }

    private Dictionary<string, byte[]> GetOwnerInvitationKeyRing()
    {
        if (ownerInvitationOptions.HmacKeys.Count is < 1 or > 8 ||
            string.IsNullOrWhiteSpace(ownerInvitationOptions.CurrentKeyVersion) ||
            !ownerInvitationOptions.HmacKeys.ContainsKey(ownerInvitationOptions.CurrentKeyVersion) ||
            ownerInvitationOptions.Lifetime <= TimeSpan.Zero ||
            ownerInvitationOptions.Lifetime > TimeSpan.FromDays(30))
        {
            throw IdentityErrors.OrganizationOwnerInvitationUnavailable();
        }

        Dictionary<string, byte[]> decoded = new(StringComparer.Ordinal);
        HashSet<string> uniqueKeys = new(StringComparer.Ordinal);
        foreach ((string version, string encodedKey) in ownerInvitationOptions.HmacKeys)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32 ||
                string.IsNullOrWhiteSpace(encodedKey))
            {
                throw IdentityErrors.OrganizationOwnerInvitationUnavailable();
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(encodedKey);
            }
            catch (FormatException)
            {
                throw IdentityErrors.OrganizationOwnerInvitationUnavailable();
            }

            if (key.Length < 32 || !uniqueKeys.Add(Convert.ToHexString(key)))
            {
                throw IdentityErrors.OrganizationOwnerInvitationUnavailable();
            }

            decoded.Add(version, key);
        }

        return decoded;
    }

    private void EnsureOwnerInvitationsEnabled(
        Dictionary<string, byte[]> keyRing,
        string operation)
    {
        if (!ownerInvitationOptions.Enabled ||
            !keyRing.ContainsKey(ownerInvitationOptions.CurrentKeyVersion))
        {
            telemetry.RecordOrganizationOwnerInvitation(
                operation,
                "unavailable");
            throw IdentityErrors.OrganizationOwnerInvitationUnavailable();
        }
    }

    private async Task RequireRetainedOwnerInvitationKeyCoverageAsync(
        IEnumerable<string> retainedKeyVersions,
        string operation,
        CancellationToken cancellationToken)
    {
        string[] versions = retainedKeyVersions
            .Order(StringComparer.Ordinal)
            .ToArray();
        bool covered = await dbContext.Database
            .SqlQueryRaw<bool>(
                "SELECT identity.owner_invitation_retained_key_covered({0}) AS \"Value\"",
                (object)versions)
            .SingleAsync(cancellationToken);
        if (!covered)
        {
            telemetry.RecordOrganizationOwnerInvitation(
                operation,
                "unavailable");
            throw IdentityErrors.OrganizationOwnerInvitationUnavailable();
        }
    }

    private static Dictionary<string, byte[]> CreateOwnerInvitationDigests(
        string purpose,
        Guid organizationId,
        string value,
        IReadOnlyDictionary<string, byte[]> keyRing)
    {
        Dictionary<string, byte[]> digests = new(StringComparer.Ordinal);
        foreach ((string version, byte[] key) in keyRing)
        {
            digests.Add(version, CreateOwnerInvitationDigest(purpose, organizationId, value, key));
        }

        return digests;
    }

    private static byte[] CreateOwnerInvitationDigest(
        string purpose,
        Guid organizationId,
        string value,
        byte[] key)
    {
        string canonicalValue = string.Join(
            '|',
            "organization-owner-invitation-v1",
            purpose,
            organizationId.ToString("D"),
            value);
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(canonicalValue));
    }

    private static byte[] CreateOwnerInvitationTokenDigest(string token, byte[] key) =>
        CreateOwnerInvitationDigest("token", Guid.Empty, token, key);

    private static void ValidateOwnerInvitationToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 43)
        {
            throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
        }

        try
        {
            byte[] decoded = WebEncoders.Base64UrlDecode(token);
            if (decoded.Length != 32 ||
                !string.Equals(WebEncoders.Base64UrlEncode(decoded), token, StringComparison.Ordinal))
            {
                throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
            }
        }
        catch (FormatException)
        {
            throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
        }
    }

    private async Task SetOwnerInvitationDatabaseContextAsync(
        Guid actorUserId,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "SET LOCAL ROLE agro_identity_app",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_actor_id', {actorUserId.ToString("D")}, true)",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_scope_kind', {(organizationId is null ? "platform" : "tenant")}, true)",
            cancellationToken);
        if (organizationId is not null)
        {
            await SetOwnerInvitationTenantContextAsync(organizationId.Value, cancellationToken);
        }
    }

    private async Task LockOwnerManagementOrganizationAsync(CancellationToken cancellationToken)
    {
        bool lockAcquired = await dbContext.Database
            .SqlQueryRaw<bool>(
                "SELECT identity.lock_owner_management_organization() AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!lockAcquired)
        {
            throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
        }
    }

    private async Task SetOwnerInvitationTenantContextAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_scope_kind', {"tenant"}, true)",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_organization_id', {organizationId.ToString("D")}, true)",
            cancellationToken);
    }

    private async Task SetOwnerRemovalAuthorizationContextAsync(
        Guid sessionId,
        Guid authorizationVersion,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_session_id', {sessionId.ToString("D")}, true)",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_authorization_version', {authorizationVersion.ToString("D")}, true)",
            cancellationToken);
    }

    private async Task RequireOrganizationOwnerAuthorizationAsync(
        Guid organizationId,
        AuthenticatedSession currentSession,
        DateTimeOffset now,
        bool requireStrongAuthentication,
        CancellationToken cancellationToken)
    {
        OwnerInvitationSessionAuthorization? authorization = await dbContext.Sessions
            .AsNoTracking()
            .Where(session =>
                session.Id == currentSession.SessionId &&
                session.UserId == currentSession.UserId &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > now)
            .Select(session => new OwnerInvitationSessionAuthorization(
                session.IsAuthenticationAssuranceVerified,
                session.AuthenticatedAtUtc,
                session.StrongAuthenticatedAtUtc,
                session.StrongAuthenticationPurpose,
                session.Version))
            .SingleOrDefaultAsync(cancellationToken);
        if (authorization is null)
        {
            throw IdentityErrors.SessionRequired();
        }

        if (requireStrongAuthentication &&
            (!authorization.IsAuthenticationAssuranceVerified ||
                authorization.StrongAuthenticatedAtUtc is null ||
                authorization.StrongAuthenticationPurpose != StepUpPurposes.ManageOrganizationOwners ||
                now - authorization.StrongAuthenticatedAtUtc.Value < TimeSpan.Zero ||
                now - authorization.StrongAuthenticatedAtUtc.Value >= runtimeOptions.StrongAuthenticationWindow))
        {
            throw IdentityErrors.StrongAuthenticationRequired();
        }

        bool isOwner = await dbContext.AuthoritativeMemberships
            .AsNoTracking()
            .AnyAsync(
                membership => membership.OrganizationId == organizationId &&
                    membership.UserId == currentSession.UserId &&
                    membership.Role == OrganizationMembershipRoles.Owner &&
                    membership.Status == OrganizationStatuses.Active,
                cancellationToken);
        if (!isOwner)
        {
            throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
        }
    }

    private async Task<OwnerInvitationSessionAuthorization>
        RequireOrganizationOwnerMembershipAuthorizationAsync(
            Guid organizationId,
            AuthenticatedSession currentSession,
            DateTimeOffset now,
            bool requireStrongAuthentication,
            CancellationToken cancellationToken)
    {
        OwnerInvitationSessionAuthorization? authorization = await dbContext.Sessions
            .AsNoTracking()
            .Where(session =>
                session.Id == currentSession.SessionId &&
                session.UserId == currentSession.UserId &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > now)
            .Select(session => new OwnerInvitationSessionAuthorization(
                session.IsAuthenticationAssuranceVerified,
                session.AuthenticatedAtUtc,
                session.StrongAuthenticatedAtUtc,
                session.StrongAuthenticationPurpose,
                session.Version))
            .SingleOrDefaultAsync(cancellationToken);
        if (authorization is null)
        {
            throw IdentityErrors.SessionRequired();
        }

        if (requireStrongAuthentication &&
            (!authorization.IsAuthenticationAssuranceVerified ||
                authorization.StrongAuthenticatedAtUtc is null ||
                authorization.StrongAuthenticationPurpose != StepUpPurposes.ManageOrganizationOwners ||
                now - authorization.StrongAuthenticatedAtUtc.Value < TimeSpan.Zero ||
                now - authorization.StrongAuthenticatedAtUtc.Value >= runtimeOptions.StrongAuthenticationWindow))
        {
            throw IdentityErrors.StrongAuthenticationRequired();
        }

        bool isOwner = await dbContext.AuthoritativeMemberships
            .AsNoTracking()
            .AnyAsync(
                membership => membership.OrganizationId == organizationId &&
                    membership.UserId == currentSession.UserId &&
                    membership.Role == OrganizationMembershipRoles.Owner &&
                    membership.Status == OrganizationMembershipStatuses.Active,
                cancellationToken);
        if (!isOwner)
        {
            throw IdentityErrors.OrganizationOwnerMembershipNotAvailable();
        }

        return authorization;
    }

    private async Task RequireRecentVerifiedSessionAsync(
        AuthenticatedSession currentSession,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        OwnerInvitationSessionAuthorization? authorization = await dbContext.Sessions
            .AsNoTracking()
            .Where(session =>
                session.Id == currentSession.SessionId &&
                session.UserId == currentSession.UserId &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > now)
            .Select(session => new OwnerInvitationSessionAuthorization(
                session.IsAuthenticationAssuranceVerified,
                session.AuthenticatedAtUtc,
                session.StrongAuthenticatedAtUtc,
                session.StrongAuthenticationPurpose,
                session.Version))
            .SingleOrDefaultAsync(cancellationToken);
        if (authorization is null)
        {
            throw IdentityErrors.SessionRequired();
        }

        TimeSpan authenticationAge = now - authorization.AuthenticatedAtUtc;
        if (!authorization.IsAuthenticationAssuranceVerified ||
            authenticationAge < TimeSpan.Zero ||
            authenticationAge >= runtimeOptions.RecentAuthenticationWindow)
        {
            throw IdentityErrors.RecentAuthenticationRequired();
        }
    }

    private async Task<OrganizationOwnerInvitation?> FindInvitationByCreationKeyAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> digests,
        CancellationToken cancellationToken)
    {
        List<OrganizationOwnerInvitation> matches = [];
        foreach ((string keyVersion, byte[] digest) in digests)
        {
            OrganizationOwnerInvitation[] currentMatches = await dbContext.OrganizationOwnerInvitations
                .AsNoTracking()
                .Where(invitation => invitation.OrganizationId == organizationId &&
                    invitation.CreationKeyVersion == keyVersion &&
                    invitation.CreationKeyDigest == digest)
                .Take(2)
                .ToArrayAsync(cancellationToken);
            matches.AddRange(currentMatches);
        }

        Guid[] distinctIds = matches.Select(item => item.Id).Distinct().Take(2).ToArray();
        return distinctIds.Length switch
        {
            0 => null,
            1 => matches.First(item => item.Id == distinctIds[0]),
            _ => throw IdentityErrors.OrganizationOwnerInvitationConflict(),
        };
    }

    private async Task<OrganizationOwnerInvitation?> FindInvitationByTokenAsync(
        string token,
        IReadOnlyDictionary<string, byte[]> keyRing,
        CancellationToken cancellationToken)
    {
        foreach ((string keyVersion, byte[] key) in keyRing)
        {
            byte[] digest = CreateOwnerInvitationTokenDigest(token, key);
            Guid? invitationId = await dbContext.Database
                .SqlQueryRaw<Guid?>(
                    "SELECT identity.resolve_owner_invitation_by_token({0}, {1}) AS \"Value\"",
                    keyVersion,
                    digest)
                .SingleAsync(cancellationToken);
            if (invitationId is not null)
            {
                return await dbContext.OrganizationOwnerInvitations
                    .SingleOrDefaultAsync(
                        invitation => invitation.Id == invitationId.Value,
                        cancellationToken);
            }
        }

        return null;
    }

    private async Task<CreatedOrganizationOwnerInvitationResult>
        ResolveOwnerInvitationCreationRaceAsync(
            Guid organizationId,
            AuthenticatedSession currentSession,
            IdentityRequestContext requestContext,
            IReadOnlyDictionary<string, byte[]> creationAliases,
            CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetOwnerInvitationDatabaseContextAsync(
            currentSession.UserId,
            organizationId,
            cancellationToken);
        await RequireOrganizationOwnerAuthorizationAsync(
            organizationId,
            currentSession,
            now,
            requireStrongAuthentication: true,
            cancellationToken);
        OrganizationOwnerInvitation invitation = await FindInvitationByCreationKeyAsync(
            organizationId,
            creationAliases,
            cancellationToken)
            ?? throw IdentityErrors.OrganizationOwnerInvitationConflict();
        await transaction.CommitAsync(cancellationToken);
        return ToCreatedOwnerInvitationResult(invitation, token: null, isReplay: true, now);
    }

    private async Task<AcceptedOrganizationOwnerInvitationResult>
        ResolveAcceptedOwnerInvitationReplayAsync(
            OrganizationOwnerInvitation invitation,
            Guid currentUserId,
            CancellationToken cancellationToken)
    {
        if (invitation.AcceptedByUserId != currentUserId ||
            invitation.AcceptedMembershipId is null)
        {
            throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
        }

        OrganizationMembershipAssignment membership = await dbContext.AuthoritativeMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == invitation.AcceptedMembershipId &&
                    item.OrganizationId == invitation.OrganizationId &&
                    item.UserId == currentUserId &&
                    item.Role == OrganizationMembershipRoles.Owner &&
                    item.Status == OrganizationMembershipStatuses.Active,
                cancellationToken)
            ?? throw IdentityErrors.OrganizationOwnerInvitationNotAvailable();
        OrganizationDirectoryEntry organization = await dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == invitation.OrganizationId &&
                    item.Status == OrganizationStatuses.Active,
                cancellationToken)
            ?? throw IdentityErrors.OrganizationOwnerInvitationConflict();
        return ToAcceptedOwnerInvitationResult(
            invitation,
            organization,
            membership,
            isReplay: true);
    }

    private static CreatedOrganizationOwnerInvitationResult ToCreatedOwnerInvitationResult(
        OrganizationOwnerInvitation invitation,
        string? token,
        bool isReplay,
        DateTimeOffset now) =>
        new(
            invitation.Id,
            invitation.OrganizationId,
            invitation.GetEffectiveStatus(now),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.RevokedAtUtc,
            invitation.Version,
            token,
            isReplay);

    private static OrganizationOwnerInvitationSummaryResult ToOwnerInvitationSummary(
        OrganizationOwnerInvitation invitation,
        DateTimeOffset now) =>
        new(
            invitation.Id,
            invitation.OrganizationId,
            invitation.GetEffectiveStatus(now),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.RevokedAtUtc,
            invitation.Version);

    private static AcceptedOrganizationOwnerInvitationResult ToAcceptedOwnerInvitationResult(
        OrganizationOwnerInvitation invitation,
        OrganizationDirectoryEntry organization,
        OrganizationMembershipAssignment membership,
        bool isReplay) =>
        new(
            invitation.Id,
            invitation.OrganizationId,
            organization.DisplayName,
            organization.Status,
            membership.Id,
            membership.Role,
            membership.Status,
            membership.SecurityVersion,
            isReplay);

    private static bool IsOwnerInvitationCreationRace(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: string constraintName,
        } && constraintName.Contains("organization_owner_invitations", StringComparison.Ordinal);

    private static bool IsSafeOwnerInvitationAcceptanceRace(Exception exception) =>
        exception is DbUpdateConcurrencyException ||
        exception is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
            },
        } ||
        exception is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected,
        };

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

    private static bool IsValidStepUpAttempt(
        StepUpAttempt? attempt,
        AuthenticatedSession currentSession,
        DateTimeOffset now) =>
        attempt is not null &&
        attempt.UserId == currentSession.UserId &&
        attempt.InitiatingSessionId == currentSession.SessionId &&
        StepUpPurposes.IsSupported(attempt.Purpose) &&
        attempt.ExpiresAtUtc > now &&
        attempt.ConsumedAtUtc is null;

    private static bool IsValidStrongAuthenticationProof(
        VerifiedStepUpProof proof,
        DateTimeOffset now) =>
        proof.IsStrongAuthentication &&
        !string.IsNullOrWhiteSpace(proof.Issuer) &&
        !string.IsNullOrWhiteSpace(proof.Subject) &&
        proof.AuthenticatedAtUtc != default &&
        proof.AuthenticatedAtUtc <= now.Add(AuthenticationClockSkew);

    private async Task AuditFailureAsync(
        AuthenticatedSession session,
        string action,
        string? connection,
        IdentityRequestContext requestContext,
        CancellationToken cancellationToken,
        string? purpose = null)
    {
        dbContext.SecurityJournalEntries.Add(CreateSecurityJournalEntry(
            session.UserId,
            session.SessionId,
            action,
            "rejected",
            connection,
            requestContext));
        await dbContext.SaveChangesAsync(cancellationToken);
        telemetry.Record(action, "rejected", connection, purpose);
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

    private async Task<OrganizationCreationAuthorization> RequireOrganizationCreationAuthorizationAsync(
        AuthenticatedSession currentSession,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        OrganizationCreationAuthorization? authorization = await dbContext.Sessions
            .AsNoTracking()
            .Where(session =>
                session.Id == currentSession.SessionId &&
                session.UserId == currentSession.UserId &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > now)
            .Select(session => new OrganizationCreationAuthorization(
                session.UserId,
                session.Id,
                session.Version,
                session.AuthenticatedAtUtc,
                session.IsAuthenticationAssuranceVerified))
            .SingleOrDefaultAsync(cancellationToken);

        if (authorization is null)
        {
            throw IdentityErrors.SessionRequired();
        }

        TimeSpan authenticationAge = now - authorization.AuthenticatedAtUtc;
        if (!authorization.IsAuthenticationAssuranceVerified ||
            authenticationAge < TimeSpan.Zero ||
            authenticationAge >= runtimeOptions.RecentAuthenticationWindow)
        {
            telemetry.RecordOrganizationCreate("reauthentication_required");
            throw IdentityErrors.RecentAuthenticationRequired();
        }

        return authorization;
    }

    private static string NormalizeOrganizationDisplayName(string? displayName)
    {
        if (displayName is null)
        {
            throw IdentityErrors.InvalidOrganizationDisplayName();
        }

        string normalized = displayName.Trim().Normalize(NormalizationForm.FormC);
        int characterCount = normalized.EnumerateRunes().Count();
        if (characterCount is < 2 or > 160)
        {
            throw IdentityErrors.InvalidOrganizationDisplayName();
        }

        return normalized;
    }

    private static void ValidateIdempotencyKey(string? idempotencyKey)
    {
        if (idempotencyKey is null ||
            idempotencyKey.Length is < 16 or > 128 ||
            idempotencyKey.Any(character => character is < '!' or > '~'))
        {
            throw IdentityErrors.InvalidIdempotencyKey();
        }
    }

    private Dictionary<string, byte[]> GetOwnerRemovalIdempotencyKeyRing()
    {
        try
        {
            return GetOrganizationIdempotencyKeyRing();
        }
        catch (IdentityOperationException exception) when (
            exception.Code == "identity.organization_creation_unavailable")
        {
            throw IdentityErrors.OrganizationOwnerMembershipUnavailable();
        }
    }

    private static byte[] CreateOwnerRemovalRequestFingerprint(
        RemoveOrganizationOwnerMembershipCommand command)
    {
        string canonicalRequest = string.Join(
            '|',
            "remove-owner-membership-v1",
            command.OrganizationId.ToString("D"),
            command.MembershipId.ToString("D"),
            command.ExpectedVersion.ToString("D"));
        return SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest));
    }

    private static DateTimeOffset TruncateToPostgreSqlPrecision(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));

    private async Task<Guid?> FindOwnerRemovalLedgerIdAsync(
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> ledgerIds = [];
        foreach ((string keyVersion, byte[] keyDigest) in aliases)
        {
            Guid[] matches = await dbContext.OrganizationOwnerRemovalKeyAliases
                .AsNoTracking()
                .Where(alias =>
                    alias.OrganizationId == organizationId &&
                    alias.ScopeKind == OrganizationOwnerRemovalProtocol.ScopeKind &&
                    alias.Namespace == OrganizationOwnerRemovalProtocol.Namespace &&
                    alias.Operation == OrganizationOwnerRemovalProtocol.Operation &&
                    alias.KeyVersion == keyVersion &&
                    alias.KeyDigest == keyDigest)
                .OrderBy(alias => alias.LedgerId)
                .Select(alias => alias.LedgerId)
                .Take(2)
                .ToArrayAsync(cancellationToken);
            foreach (Guid ledgerId in matches)
            {
                ledgerIds.Add(ledgerId);
            }
        }

        return ledgerIds.Count switch
        {
            0 => null,
            1 => ledgerIds.Single(),
            _ => throw IdentityErrors.ReconciliationRequired(),
        };
    }

    private async Task RequireRetainedOwnerRemovalKeyCoverageAsync(
        IEnumerable<string> retainedKeyVersions,
        CancellationToken cancellationToken)
    {
        string[] versions = retainedKeyVersions
            .Order(StringComparer.Ordinal)
            .ToArray();
        bool covered = await dbContext.Database
            .SqlQueryRaw<bool>(
                "SELECT identity.owner_removal_retained_key_covered({0}) AS \"Value\"",
                (object)versions)
            .SingleAsync(cancellationToken);
        if (!covered)
        {
            telemetry.RecordOrganizationOwnerMembership(
                "organization_owner_membership_remove",
                "unavailable");
            throw IdentityErrors.OrganizationOwnerMembershipUnavailable();
        }
    }

    private async Task AddMissingOwnerRemovalAliasesAsync(
        Guid ledgerId,
        Guid organizationId,
        IReadOnlyDictionary<string, byte[]> aliases,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        OrganizationOwnerRemovalKeyAlias[] existingAliases = await dbContext
            .OrganizationOwnerRemovalKeyAliases
            .Where(alias => alias.LedgerId == ledgerId)
            .OrderBy(alias => alias.KeyVersion)
            .ToArrayAsync(cancellationToken);
        Dictionary<string, OrganizationOwnerRemovalKeyAlias> existingByVersion = existingAliases
            .ToDictionary(alias => alias.KeyVersion, StringComparer.Ordinal);

        foreach ((string keyVersion, byte[] keyDigest) in aliases.OrderBy(
            item => item.Key,
            StringComparer.Ordinal))
        {
            if (existingByVersion.TryGetValue(
                keyVersion,
                out OrganizationOwnerRemovalKeyAlias? existingAlias))
            {
                if (!CryptographicOperations.FixedTimeEquals(existingAlias.KeyDigest, keyDigest))
                {
                    throw IdentityErrors.IdempotencyKeyReused();
                }

                continue;
            }

            dbContext.OrganizationOwnerRemovalKeyAliases.Add(
                new OrganizationOwnerRemovalKeyAlias(
                    Guid.NewGuid(),
                    ledgerId,
                    organizationId,
                    keyVersion,
                    keyDigest,
                    now));
        }
    }

    private async Task<OwnerRemovalDatabaseResult> RemoveActiveOwnerAsync(
        Guid membershipId,
        Guid expectedVersion,
        DateTimeOffset removedAtUtc,
        Guid newVersion,
        CancellationToken cancellationToken) =>
        await dbContext.Database
            .SqlQueryRaw<OwnerRemovalDatabaseResult>(
                """
                SELECT
                    outcome AS "Outcome",
                    membership_id AS "MembershipId",
                    display_name AS "DisplayName",
                    role AS "Role",
                    status AS "Status",
                    security_version AS "SecurityVersion",
                    created_at_utc AS "CreatedAtUtc",
                    removed_at_utc AS "RemovedAtUtc",
                    version AS "Version",
                    is_current_user AS "IsCurrentUser",
                    revoked_invitation_ids AS "RevokedInvitationIds"
                FROM identity.remove_active_owner({0}, {1}, {2}, {3})
                """,
                membershipId,
                expectedVersion,
                removedAtUtc,
                newVersion)
            .SingleAsync(cancellationToken);

    private static void ThrowForOwnerRemovalOutcome(string? outcome)
    {
        switch (outcome)
        {
            case "removed":
                return;
            case "not_available":
                throw IdentityErrors.OrganizationOwnerMembershipNotAvailable();
            case "version_mismatch":
                throw IdentityErrors.OrganizationOwnerMembershipVersionMismatch();
            case "last_owner":
                throw IdentityErrors.OrganizationLastOwnerRequired();
            default:
                throw IdentityErrors.OrganizationOwnerMembershipUnavailable();
        }
    }

    private static void ValidateRemovedOwnerDatabaseResult(
        OwnerRemovalDatabaseResult result,
        RemoveOrganizationOwnerMembershipCommand command,
        Guid expectedNewVersion)
    {
        if (result.MembershipId != command.MembershipId ||
            string.IsNullOrWhiteSpace(result.DisplayName) ||
            result.Role != OrganizationMembershipRoles.Owner ||
            result.Status != OrganizationMembershipStatuses.Removed ||
            result.SecurityVersion is null or < 2 ||
            result.CreatedAtUtc is null ||
            result.RemovedAtUtc is null ||
            result.RemovedAtUtc < result.CreatedAtUtc ||
            result.Version != expectedNewVersion ||
            result.IsCurrentUser is not false ||
            result.RevokedInvitationIds.Any(id => id == Guid.Empty) ||
            result.RevokedInvitationIds.Distinct().Count() != result.RevokedInvitationIds.Length)
        {
            throw IdentityErrors.OrganizationOwnerMembershipUnavailable();
        }
    }

    private async Task<RemovedOrganizationOwnerMembershipResult> ResolveOwnerRemovalReplayAsync(
        Guid ledgerId,
        OwnerInvitationSessionAuthorization authorization,
        byte[] fingerprint,
        Guid currentUserId,
        Guid currentSessionId,
        CancellationToken cancellationToken)
    {
        OrganizationOwnerRemovalLedger ledger = await dbContext.OrganizationOwnerRemovalLedgers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ledgerId, cancellationToken)
            ?? throw IdentityErrors.ReconciliationRequired();
        if (ledger.ActorUserId != currentUserId ||
            ledger.SessionId != currentSessionId ||
            ledger.AuthorizationVersion != authorization.Version ||
            !CryptographicOperations.FixedTimeEquals(ledger.RequestFingerprint, fingerprint))
        {
            throw IdentityErrors.IdempotencyKeyReused();
        }

        if (ledger.State == OrganizationOwnerRemovalProtocol.States.InProgress)
        {
            throw IdentityErrors.IdempotencyInProgress();
        }

        if (ledger.State == OrganizationOwnerRemovalProtocol.States.FailedTerminal)
        {
            throw IdentityErrors.IdempotencyFailedTerminal();
        }

        if (ledger.State == OrganizationOwnerRemovalProtocol.States.ResponseExpired ||
            ledger.State != OrganizationOwnerRemovalProtocol.States.Succeeded ||
            ledger.ResultMembershipVersion is null ||
            ledger.ResultAuthorizationVersion is null ||
            ledger.RemovedAtUtc is null)
        {
            throw IdentityErrors.ReconciliationRequired();
        }

        OwnerRemovalDatabaseResult? databaseResult = await dbContext.Database
            .SqlQueryRaw<OwnerRemovalDatabaseResult>(
                """
                SELECT
                    'removed'::text AS "Outcome",
                    membership_id AS "MembershipId",
                    display_name AS "DisplayName",
                    role AS "Role",
                    status AS "Status",
                    security_version AS "SecurityVersion",
                    created_at_utc AS "CreatedAtUtc",
                    removed_at_utc AS "RemovedAtUtc",
                    version AS "Version",
                    is_current_user AS "IsCurrentUser",
                    ARRAY[]::uuid[] AS "RevokedInvitationIds"
                FROM identity.resolve_owner_removal_result({0})
                """,
                ledgerId)
            .SingleOrDefaultAsync(cancellationToken);
        if (databaseResult is null ||
            databaseResult.MembershipId != ledger.MembershipId ||
            databaseResult.Version != ledger.ResultMembershipVersion ||
            databaseResult.SecurityVersion != ledger.ResultAuthorizationVersion ||
            databaseResult.RemovedAtUtc != ledger.RemovedAtUtc)
        {
            throw IdentityErrors.ReconciliationRequired();
        }

        ValidateRemovedOwnerReplayDatabaseResult(databaseResult);
        return ToRemovedOwnerMembershipResult(
            databaseResult,
            ledger.OrganizationId,
            isReplay: true);
    }

    private static void ValidateRemovedOwnerReplayDatabaseResult(OwnerRemovalDatabaseResult result)
    {
        if (result.MembershipId is null ||
            string.IsNullOrWhiteSpace(result.DisplayName) ||
            result.Role != OrganizationMembershipRoles.Owner ||
            result.Status != OrganizationMembershipStatuses.Removed ||
            result.SecurityVersion is null or < 2 ||
            result.CreatedAtUtc is null ||
            result.RemovedAtUtc is null ||
            result.RemovedAtUtc < result.CreatedAtUtc ||
            result.Version is null ||
            result.IsCurrentUser is not false)
        {
            throw IdentityErrors.ReconciliationRequired();
        }
    }

    private static OrganizationOwnerMembershipSummaryResult ToOwnerMembershipSummary(
        OwnerMembershipDatabaseResult membership,
        Guid organizationId) =>
        new(
            membership.MembershipId,
            organizationId,
            membership.DisplayName,
            membership.IsCurrentUser,
            membership.Role,
            membership.Status,
            membership.SecurityVersion,
            membership.CreatedAtUtc,
            membership.RemovedAtUtc,
            membership.Version);

    private async Task<OwnerMembershipDatabaseResult[]> LoadActiveOwnerMembershipsAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Database
            .SqlQueryRaw<OwnerMembershipDatabaseResult>(
                """
                SELECT
                    membership_id AS "MembershipId",
                    display_name AS "DisplayName",
                    role AS "Role",
                    status AS "Status",
                    security_version AS "SecurityVersion",
                    created_at_utc AS "CreatedAtUtc",
                    NULL::timestamptz AS "RemovedAtUtc",
                    version AS "Version",
                    is_current_user AS "IsCurrentUser"
                FROM identity.list_active_owner_memberships()
                """)
            .ToArrayAsync(cancellationToken);

    private static RemovedOrganizationOwnerMembershipResult ToRemovedOwnerMembershipResult(
        OwnerRemovalDatabaseResult membership,
        Guid organizationId,
        bool isReplay) =>
        new(
            membership.MembershipId!.Value,
            organizationId,
            membership.DisplayName!,
            false,
            membership.Role!,
            membership.Status!,
            membership.SecurityVersion!.Value,
            membership.CreatedAtUtc!.Value,
            membership.RemovedAtUtc!.Value,
            membership.Version!.Value,
            isReplay);

    private async Task<RemovedOrganizationOwnerMembershipResult?> TryResolveOwnerRemovalRaceAsync(
        RemoveOrganizationOwnerMembershipCommand command,
        AuthenticatedSession currentSession,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetOwnerInvitationDatabaseContextAsync(
            currentSession.UserId,
            command.OrganizationId,
            cancellationToken);
        OwnerInvitationSessionAuthorization authorization =
            await RequireOrganizationOwnerMembershipAuthorizationAsync(
                command.OrganizationId,
                currentSession,
                timeProvider.GetUtcNow(),
                requireStrongAuthentication: true,
                cancellationToken);
        await SetOwnerRemovalAuthorizationContextAsync(
            currentSession.SessionId,
            authorization.Version,
            cancellationToken);
        Guid? ledgerId = await FindOwnerRemovalLedgerIdAsync(
            command.OrganizationId,
            aliases,
            cancellationToken);
        if (ledgerId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        RemovedOrganizationOwnerMembershipResult replay =
            await ResolveOwnerRemovalReplayAsync(
                ledgerId.Value,
                authorization,
                CreateOwnerRemovalRequestFingerprint(command),
                currentSession.UserId,
                currentSession.SessionId,
                cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return replay;
    }

    private async Task<RemovedOrganizationOwnerMembershipResult> ResolveOwnerRemovalRaceAsync(
        RemoveOrganizationOwnerMembershipCommand command,
        AuthenticatedSession currentSession,
        IReadOnlyDictionary<string, byte[]> aliases,
        bool missingIsConflict,
        CancellationToken cancellationToken)
    {
        RemovedOrganizationOwnerMembershipResult? replay =
            await TryResolveOwnerRemovalRaceAsync(
                command,
                currentSession,
                aliases,
                cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        throw missingIsConflict
            ? IdentityErrors.IdempotencyKeyReused()
            : IdentityErrors.ReconciliationRequired();
    }

    private async Task<RemovedOrganizationOwnerMembershipResult> RecoverUnknownOwnerRemovalCommitAsync(
        RemoveOrganizationOwnerMembershipCommand command,
        AuthenticatedSession currentSession,
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken)
    {
        try
        {
            await using IdentityDbContext recoveryDbContext =
                await recoveryContextFactory.CreateDbContextAsync(cancellationToken);
            IdentityApplicationService recoveryService = new(
                recoveryDbContext,
                tokenService,
                telemetry,
                timeProvider,
                Options.Create(runtimeOptions),
                Options.Create(bootstrapOptions),
                commitBoundary,
                recoveryContextFactory,
                Options.Create(ownerInvitationOptions));
            RemovedOrganizationOwnerMembershipResult? replay =
                await recoveryService.TryResolveOwnerRemovalRaceAsync(
                    command,
                    currentSession,
                    aliases,
                    cancellationToken);
            if (replay is not null)
            {
                telemetry.RecordOrganizationOwnerMembership(
                    "organization_owner_membership_remove",
                    "replayed");
                return replay;
            }
        }
        catch (IdentityOperationException exception) when (
            IsOrganizationIdempotencyRejection(exception.Code))
        {
            throw;
        }
        catch (Exception exception) when (IsIndeterminateCommit(exception))
        {
            // The recovery connection also failed ambiguously. Fall through to a typed retry.
        }

        telemetry.RecordOrganizationOwnerMembership(
            "organization_owner_membership_remove",
            "reconciliation_required");
        throw IdentityErrors.ReconciliationRequired();
    }

    private static bool IsOwnerRemovalIdempotencyRace(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: string constraintName,
        } && constraintName.Contains("organization_owner_removal_key_aliases", StringComparison.Ordinal);

    private static string OwnerRemovalIdempotencyOutcome(string code) => code switch
    {
        "idempotency.key_reused" => "conflict",
        "idempotency.in_progress" => "in_progress",
        "idempotency.failed_terminal" => "failed_terminal",
        "idempotency.reconciliation_required" => "reconciliation_required",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown idempotency code."),
    };

    private Dictionary<string, byte[]> GetOrganizationIdempotencyKeyRing()
    {
        if (bootstrapOptions.IdempotencyHmacKeys.Count is < 1 or > 8 ||
            string.IsNullOrWhiteSpace(bootstrapOptions.CurrentKeyVersion) ||
            !bootstrapOptions.IdempotencyHmacKeys.ContainsKey(bootstrapOptions.CurrentKeyVersion))
        {
            throw IdentityErrors.OrganizationCreationUnavailable();
        }

        Dictionary<string, byte[]> decoded = new(StringComparer.Ordinal);
        HashSet<string> uniqueKeys = new(StringComparer.Ordinal);
        foreach ((string version, string encodedKey) in bootstrapOptions.IdempotencyHmacKeys)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32 ||
                string.IsNullOrWhiteSpace(encodedKey))
            {
                throw IdentityErrors.OrganizationCreationUnavailable();
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(encodedKey);
            }
            catch (FormatException)
            {
                throw IdentityErrors.OrganizationCreationUnavailable();
            }

            if (key.Length < 32 || !uniqueKeys.Add(Convert.ToHexString(key)))
            {
                throw IdentityErrors.OrganizationCreationUnavailable();
            }

            decoded.Add(version, key);
        }

        return decoded;
    }

    private static Dictionary<string, byte[]> CreateIdempotencyAliases(
        string idempotencyKey,
        Dictionary<string, byte[]> keyRing)
    {
        byte[] keyBytes = Encoding.ASCII.GetBytes(idempotencyKey);
        Dictionary<string, byte[]> aliases = new(StringComparer.Ordinal);
        foreach ((string version, byte[] secret) in keyRing)
        {
            aliases.Add(version, HMACSHA256.HashData(secret, keyBytes));
        }

        return aliases;
    }

    private static byte[] CreateOrganizationRequestFingerprint(
        string displayName,
        OrganizationCreationAuthorization authorization)
    {
        string canonicalRequest = string.Join(
            '|',
            "create-organization-v1",
            authorization.UserId.ToString("D"),
            authorization.SessionId.ToString("D"),
            authorization.AuthorizationVersion.ToString("D"),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(displayName)));
        return SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest));
    }

    private async Task<Guid?> FindOrganizationCreationLedgerIdAsync(
        IReadOnlyDictionary<string, byte[]> aliases,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> ledgerIds = [];
        foreach ((string keyVersion, byte[] keyDigest) in aliases)
        {
            Guid[] matches = await dbContext.OrganizationCreationKeyAliases
                .AsNoTracking()
                .Where(alias =>
                    alias.ScopeKind == OrganizationCreationProtocol.ScopeKind &&
                    alias.Namespace == OrganizationCreationProtocol.Namespace &&
                    alias.Operation == OrganizationCreationProtocol.Operation &&
                    alias.KeyVersion == keyVersion &&
                    alias.KeyDigest == keyDigest)
                .OrderBy(alias => alias.LedgerId)
                .Select(alias => alias.LedgerId)
                .Take(2)
                .ToArrayAsync(cancellationToken);
            foreach (Guid ledgerId in matches)
            {
                ledgerIds.Add(ledgerId);
            }
        }

        return ledgerIds.Count switch
        {
            0 => null,
            1 => ledgerIds.Single(),
            _ => throw IdentityErrors.ReconciliationRequired(),
        };
    }

    private async Task RequireRetainedOrganizationKeyCoverageAsync(
        IEnumerable<string> retainedKeyVersions,
        CancellationToken cancellationToken)
    {
        foreach (string keyVersion in retainedKeyVersions.Order(StringComparer.Ordinal))
        {
            bool covered = await dbContext.Database
                .SqlQueryRaw<bool>(
                    "SELECT identity.organization_creation_current_key_covered({0}) AS \"Value\"",
                    keyVersion)
                .SingleAsync(cancellationToken);
            if (covered)
            {
                return;
            }
        }

        telemetry.RecordOrganizationCreate("unavailable");
        throw IdentityErrors.OrganizationCreationUnavailable();
    }

    private async Task AddMissingOrganizationCreationAliasesAsync(
        Guid ledgerId,
        IReadOnlyDictionary<string, byte[]> aliases,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        OrganizationCreationKeyAlias[] existingAliases = await dbContext
            .OrganizationCreationKeyAliases
            .Where(alias => alias.LedgerId == ledgerId)
            .OrderBy(alias => alias.KeyVersion)
            .ToArrayAsync(cancellationToken);
        Dictionary<string, OrganizationCreationKeyAlias> existingByVersion = existingAliases
            .ToDictionary(alias => alias.KeyVersion, StringComparer.Ordinal);

        foreach ((string keyVersion, byte[] keyDigest) in aliases.OrderBy(
            item => item.Key,
            StringComparer.Ordinal))
        {
            if (existingByVersion.TryGetValue(
                keyVersion,
                out OrganizationCreationKeyAlias? existingAlias))
            {
                if (!CryptographicOperations.FixedTimeEquals(existingAlias.KeyDigest, keyDigest))
                {
                    throw IdentityErrors.ReconciliationRequired();
                }

                continue;
            }

            dbContext.OrganizationCreationKeyAliases.Add(new OrganizationCreationKeyAlias(
                Guid.NewGuid(),
                ledgerId,
                keyVersion,
                keyDigest,
                now));
        }
    }

    private async Task<CreatedOrganizationResult> ResolveOrganizationCreationReplayAsync(
        Guid ledgerId,
        OrganizationCreationAuthorization authorization,
        byte[] fingerprint,
        CancellationToken cancellationToken)
    {
        OrganizationCreationLedger? ledger = await dbContext.OrganizationCreationLedgers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ledgerId, cancellationToken);
        if (ledger is null)
        {
            throw IdentityErrors.IdempotencyKeyReused();
        }

        if (ledger.ActorUserId != authorization.UserId ||
            ledger.SessionId != authorization.SessionId ||
            ledger.AuthorizationVersion != authorization.AuthorizationVersion ||
            !CryptographicOperations.FixedTimeEquals(ledger.RequestFingerprint, fingerprint))
        {
            throw IdentityErrors.IdempotencyKeyReused();
        }

        if (ledger.State == OrganizationCreationProtocol.States.InProgress)
        {
            throw IdentityErrors.IdempotencyInProgress();
        }

        if (ledger.State == OrganizationCreationProtocol.States.FailedTerminal)
        {
            throw IdentityErrors.IdempotencyFailedTerminal();
        }

        if (ledger.State == OrganizationCreationProtocol.States.ResponseExpired)
        {
            throw IdentityErrors.ReconciliationRequired();
        }

        if (ledger.State != OrganizationCreationProtocol.States.Succeeded ||
            ledger.OrganizationId is null ||
            ledger.MembershipId is null)
        {
            throw IdentityErrors.ReconciliationRequired();
        }

        await SetOrganizationBootstrapOrganizationContextAsync(
            ledger.OrganizationId.Value,
            cancellationToken);

        OrganizationDirectoryEntry? organization = await dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ledger.OrganizationId, cancellationToken);
        OrganizationMembershipAssignment? membership = await dbContext.AuthoritativeMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == ledger.MembershipId &&
                item.OrganizationId == ledger.OrganizationId &&
                item.UserId == authorization.UserId,
                cancellationToken);
        if (organization is null || membership is null)
        {
            throw IdentityErrors.ReconciliationRequired();
        }

        return ToCreatedOrganizationResult(organization, membership);
    }

    private async Task<CreatedOrganizationResult> ResolveOrganizationCreationRaceAsync(
        AuthenticatedSession currentSession,
        string displayName,
        Dictionary<string, byte[]> aliases,
        DateTimeOffset now,
        bool missingIsKeyReused,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetOrganizationBootstrapDatabaseContextAsync(
            currentSession.UserId,
            null,
            cancellationToken);
        OrganizationCreationAuthorization authorization =
            await RequireOrganizationCreationAuthorizationAsync(
                currentSession,
                now,
                cancellationToken);
        Guid? ledgerId = await FindOrganizationCreationLedgerIdAsync(
            aliases,
            cancellationToken);
        if (ledgerId is null)
        {
            throw missingIsKeyReused
                ? IdentityErrors.IdempotencyKeyReused()
                : IdentityErrors.ReconciliationRequired();
        }

        byte[] fingerprint = CreateOrganizationRequestFingerprint(displayName, authorization);
        CreatedOrganizationResult replay = await ResolveOrganizationCreationReplayAsync(
            ledgerId.Value,
            authorization,
            fingerprint,
            cancellationToken);
        await AddMissingOrganizationCreationAliasesAsync(
            ledgerId.Value,
            aliases,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return replay;
    }

    private async Task<CreatedOrganizationResult> RecoverUnknownOrganizationCommitAsync(
        CreateOrganizationCommand command,
        AuthenticatedSession currentSession,
        IdentityRequestContext requestContext,
        bool retryAfterUnknownRollback,
        CancellationToken cancellationToken)
    {
        try
        {
            await using IdentityDbContext recoveryDbContext =
                await recoveryContextFactory.CreateDbContextAsync(cancellationToken);
            IdentityApplicationService recoveryService = new(
                recoveryDbContext,
                tokenService,
                telemetry,
                timeProvider,
                Options.Create(runtimeOptions),
                Options.Create(bootstrapOptions),
                commitBoundary,
                recoveryContextFactory);
            CreatedOrganizationResult? committed;
            try
            {
                committed = await recoveryService.TryResolveUnknownOrganizationCommitAsync(
                    command,
                    currentSession,
                    cancellationToken);
            }
            catch (IdentityOperationException exception) when (
                IsOrganizationIdempotencyRejection(exception.Code))
            {
                telemetry.RecordOrganizationCreate(OrganizationIdempotencyOutcome(exception.Code));
                throw;
            }
            if (committed is not null)
            {
                telemetry.RecordOrganizationCreate("replayed");
                return committed;
            }

            if (!retryAfterUnknownRollback)
            {
                telemetry.RecordOrganizationCreate("reconciliation_required");
                throw IdentityErrors.ReconciliationRequired();
            }

            return await recoveryService.CreateOrganizationAsync(
                command,
                currentSession,
                requestContext,
                retryAfterUnknownRollback: false,
                cancellationToken);
        }
        catch (Exception exception) when (IsIndeterminateCommit(exception))
        {
            telemetry.RecordOrganizationCreate("reconciliation_required");
            throw IdentityErrors.ReconciliationRequired();
        }
    }

    private async Task<CreatedOrganizationResult?> TryResolveUnknownOrganizationCommitAsync(
        CreateOrganizationCommand command,
        AuthenticatedSession currentSession,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string displayName = NormalizeOrganizationDisplayName(command.DisplayName);
        ValidateIdempotencyKey(command.IdempotencyKey);
        Dictionary<string, byte[]> keyRing = GetOrganizationIdempotencyKeyRing();
        Dictionary<string, byte[]> aliases = CreateIdempotencyAliases(
            command.IdempotencyKey,
            keyRing);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await SetOrganizationBootstrapDatabaseContextAsync(
            currentSession.UserId,
            null,
            cancellationToken);
        OrganizationCreationAuthorization authorization =
            await RequireOrganizationCreationAuthorizationAsync(
                currentSession,
                now,
                cancellationToken);
        await RequireRetainedOrganizationKeyCoverageAsync(
            keyRing.Keys,
            cancellationToken);
        Guid? ledgerId = await FindOrganizationCreationLedgerIdAsync(
            aliases,
            cancellationToken);
        if (ledgerId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        byte[] fingerprint = CreateOrganizationRequestFingerprint(displayName, authorization);
        CreatedOrganizationResult replay = await ResolveOrganizationCreationReplayAsync(
            ledgerId.Value,
            authorization,
            fingerprint,
            cancellationToken);
        await AddMissingOrganizationCreationAliasesAsync(
            ledgerId.Value,
            aliases,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return replay;
    }

    private async Task SetOrganizationBootstrapDatabaseContextAsync(
        Guid actorUserId,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "SET LOCAL ROLE agro_identity_app",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_actor_id', {actorUserId.ToString("D")}, true)",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_scope_kind', {OrganizationCreationProtocol.ScopeKind}, true)",
            cancellationToken);
        if (organizationId is not null)
        {
            await SetOrganizationBootstrapOrganizationContextAsync(
                organizationId.Value,
                cancellationToken);
        }
    }

    private async Task SetOrganizationBootstrapOrganizationContextAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_organization_id', {organizationId.ToString("D")}, true)",
            cancellationToken);

    private static CreatedOrganizationResult ToCreatedOrganizationResult(
        OrganizationDirectoryEntry organization,
        OrganizationMembershipAssignment membership) =>
        new(
            organization.Id,
            organization.DisplayName,
            organization.Status,
            membership.Id,
            membership.Role,
            membership.Status,
            membership.SecurityVersion);

    private static bool IsOrganizationIdempotencyRace(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: string constraintName,
        } && constraintName.Contains("organization_creation_key_aliases", StringComparison.Ordinal);

    private static bool IsOrganizationIdempotencyRejection(string code) => code is
        "idempotency.key_reused" or
        "idempotency.in_progress" or
        "idempotency.failed_terminal" or
        "idempotency.reconciliation_required";

    private static string OrganizationIdempotencyOutcome(string code) => code switch
    {
        "idempotency.key_reused" => "conflict",
        "idempotency.in_progress" => "in_progress",
        "idempotency.failed_terminal" => "failed_terminal",
        "idempotency.reconciliation_required" => "reconciliation_required",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown idempotency code."),
    };

    private static bool IsIndeterminateCommit(Exception exception) =>
        exception is OrganizationCommitOutcomeUnknownException ||
        FindNpgsqlException(exception) is { IsTransient: true };

    private static bool IsOrganizationSerializationRace(Exception exception) =>
        FindNpgsqlException(exception) is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected,
        };

    private static NpgsqlException? FindNpgsqlException(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is NpgsqlException npgsqlException)
            {
                return npgsqlException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static bool IsOrganizationBootstrapRoleUnavailable(PostgresException exception) =>
        exception.SqlState is PostgresErrorCodes.InsufficientPrivilege or "42704";

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

    private static bool IsConcurrentStepUpCompletion(Exception exception) =>
        exception is DbUpdateConcurrencyException or DbUpdateException ||
        exception is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected,
        };

    private sealed record OrganizationCreationAuthorization(
        Guid UserId,
        Guid SessionId,
        Guid AuthorizationVersion,
        DateTimeOffset AuthenticatedAtUtc,
        bool IsAuthenticationAssuranceVerified);

    private sealed class OwnSessionDatabaseResult
    {
        public Guid SessionId { get; init; }

        public DateTimeOffset AuthenticatedAtUtc { get; init; }

        public DateTimeOffset ExpiresAtUtc { get; init; }

        public Guid Version { get; init; }

        public bool IsCurrent { get; init; }

        public long TotalCount { get; init; }
    }

    private sealed class RevokeOwnSessionDatabaseResult
    {
        public string Outcome { get; init; } = string.Empty;

        public Guid? SessionId { get; init; }

        public DateTimeOffset? RevokedAtUtc { get; init; }

        public Guid? Version { get; init; }
    }

    private sealed class RevokeAllOtherOwnSessionsDatabaseResult
    {
        public string Outcome { get; init; } = string.Empty;

        public Guid? SessionId { get; init; }

        public DateTimeOffset? RevokedAtUtc { get; init; }

        public Guid? Version { get; init; }
    }

    private sealed class RevokeAllOwnSessionsDatabaseResult
    {
        public string Outcome { get; init; } = string.Empty;

        public Guid? SessionId { get; init; }

        public DateTimeOffset? RevokedAtUtc { get; init; }

        public Guid? Version { get; init; }
    }

    private sealed class RevokeCurrentOwnSessionDatabaseResult
    {
        public string Outcome { get; init; } = string.Empty;

        public Guid? SessionId { get; init; }

        public DateTimeOffset? RevokedAtUtc { get; init; }

        public Guid? Version { get; init; }
    }

    private sealed class OwnerMembershipDatabaseResult
    {
        public Guid MembershipId { get; init; }

        public string DisplayName { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public long SecurityVersion { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public DateTimeOffset? RemovedAtUtc { get; init; }

        public Guid Version { get; init; }

        public bool IsCurrentUser { get; init; }
    }

    private sealed class OwnerRemovalDatabaseResult
    {
        public string? Outcome { get; init; }

        public Guid? MembershipId { get; init; }

        public string? DisplayName { get; init; }

        public string? Role { get; init; }

        public string? Status { get; init; }

        public long? SecurityVersion { get; init; }

        public DateTimeOffset? CreatedAtUtc { get; init; }

        public DateTimeOffset? RemovedAtUtc { get; init; }

        public Guid? Version { get; init; }

        public bool? IsCurrentUser { get; init; }

        public Guid[] RevokedInvitationIds { get; init; } = [];
    }

    private sealed record OwnerInvitationSessionAuthorization(
        bool IsAuthenticationAssuranceVerified,
        DateTimeOffset AuthenticatedAtUtc,
        DateTimeOffset? StrongAuthenticatedAtUtc,
        string? StrongAuthenticationPurpose,
        Guid Version);

}

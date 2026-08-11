using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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
    IOptions<IdentityRuntimeOptions> options,
    IOptions<OrganizationBootstrapOptions> organizationBootstrapOptions,
    IOrganizationCreationCommitBoundary? organizationCommitBoundary = null,
    IOrganizationCreationRecoveryContextFactory? organizationRecoveryContextFactory = null)
{
    private static readonly TimeSpan AuthenticationClockSkew = TimeSpan.FromMinutes(1);
    private readonly IdentityRuntimeOptions runtimeOptions = options.Value;
    private readonly OrganizationBootstrapOptions bootstrapOptions = organizationBootstrapOptions.Value;
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
                session.StrongAuthenticationPurpose))
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

}

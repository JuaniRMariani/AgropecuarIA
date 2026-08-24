using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Identity.Application;

public sealed record AddMemberCommand(
    Guid OrganizationId,
    Guid UserId,
    string Role,
    IReadOnlyList<Guid>? FieldScopes,
    Guid ActorUserId);

public sealed record UpdateMemberRoleCommand(
    Guid OrganizationId,
    Guid MembershipId,
    string NewRole,
    Guid ActorUserId);

public sealed record AssignFieldScopeCommand(
    Guid OrganizationId,
    Guid MembershipId,
    Guid FieldId,
    Guid ActorUserId);

public sealed record RevokeFieldScopeCommand(
    Guid OrganizationId,
    Guid MembershipId,
    Guid FieldId,
    Guid ActorUserId);

public sealed record MemberDto(
    Guid MembershipId,
    Guid UserId,
    string Role,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<Guid> FieldScopes);

public sealed record EffectivePermissionsResult(
    Guid OrganizationId,
    Guid UserId,
    string Role,
    bool HasAllFieldsAccess,
    IReadOnlyList<Guid> AllowedFieldIds,
    IReadOnlyList<string> AllowedModules);

public sealed class OrganizationMembershipApplicationService(IdentityDbContext dbContext)
{
    public async Task<MemberDto> AddMemberAsync(
        AddMemberCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool orgExists = await dbContext.Organizations
            .AnyAsync(x => x.Id == command.OrganizationId, cancellationToken);
        if (!orgExists)
            throw new InvalidOperationException("Organization not found.");

        bool userExists = await dbContext.Users
            .AnyAsync(x => x.Id == command.UserId, cancellationToken);
        if (!userExists)
            throw new InvalidOperationException("User not found.");

        bool existingActive = await dbContext.AuthoritativeMemberships
            .AnyAsync(x => x.OrganizationId == command.OrganizationId &&
                           x.UserId == command.UserId &&
                           x.Status == OrganizationMembershipStatuses.Active,
                      cancellationToken);
        if (existingActive)
            throw new InvalidOperationException("User already has an active membership in this organization.");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid membershipId = Guid.NewGuid();

        var membership = new OrganizationMembershipAssignment(
            membershipId,
            command.OrganizationId,
            command.UserId,
            now,
            command.Role);

        dbContext.AuthoritativeMemberships.Add(membership);

        List<Guid> grantedScopes = [];
        if (command.FieldScopes is not null && command.FieldScopes.Count > 0)
        {
            foreach (var fieldId in command.FieldScopes.Distinct())
            {
                var scope = new OrganizationFieldScopeAssignment(
                    Guid.NewGuid(),
                    command.OrganizationId,
                    membershipId,
                    fieldId,
                    command.ActorUserId,
                    now);
                dbContext.FieldScopes.Add(scope);
                grantedScopes.Add(fieldId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MemberDto(
            membership.Id,
            membership.UserId,
            membership.Role,
            membership.Status,
            membership.CreatedAtUtc,
            grantedScopes);
    }

    public async Task<MemberDto> UpdateMemberRoleAsync(
        UpdateMemberRoleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await dbContext.AuthoritativeMemberships
            .FirstOrDefaultAsync(x => x.Id == command.MembershipId && x.OrganizationId == command.OrganizationId, cancellationToken);

        if (membership is null)
            throw new InvalidOperationException("Membership not found in this organization.");

        // Protect last owner
        if (membership.Role == OrganizationMembershipRoles.Owner && command.NewRole != OrganizationMembershipRoles.Owner)
        {
            int ownerCount = await dbContext.AuthoritativeMemberships
                .CountAsync(x => x.OrganizationId == command.OrganizationId &&
                                 x.Role == OrganizationMembershipRoles.Owner &&
                                 x.Status == OrganizationMembershipStatuses.Active,
                            cancellationToken);
            if (ownerCount <= 1)
                throw new InvalidOperationException("Cannot demote the last remaining owner of the organization.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        membership.UpdateRole(command.NewRole, command.ActorUserId, now, Guid.NewGuid());
        await dbContext.SaveChangesAsync(cancellationToken);

        var scopes = await dbContext.FieldScopes
            .Where(x => x.MembershipId == membership.Id)
            .Select(x => x.FieldId)
            .ToListAsync(cancellationToken);

        return new MemberDto(
            membership.Id,
            membership.UserId,
            membership.Role,
            membership.Status,
            membership.CreatedAtUtc,
            scopes);
    }

    public async Task<bool> AssignFieldScopeAsync(
        AssignFieldScopeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await dbContext.AuthoritativeMemberships
            .FirstOrDefaultAsync(x => x.Id == command.MembershipId && x.OrganizationId == command.OrganizationId, cancellationToken);

        if (membership is null || membership.Status != OrganizationMembershipStatuses.Active)
            return false;

        bool alreadyAssigned = await dbContext.FieldScopes
            .AnyAsync(x => x.OrganizationId == command.OrganizationId &&
                           x.MembershipId == command.MembershipId &&
                           x.FieldId == command.FieldId,
                      cancellationToken);

        if (alreadyAssigned)
            return true;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var scope = new OrganizationFieldScopeAssignment(
            Guid.NewGuid(),
            command.OrganizationId,
            command.MembershipId,
            command.FieldId,
            command.ActorUserId,
            now);

        dbContext.FieldScopes.Add(scope);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeFieldScopeAsync(
        RevokeFieldScopeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var scope = await dbContext.FieldScopes
            .FirstOrDefaultAsync(x => x.OrganizationId == command.OrganizationId &&
                                      x.MembershipId == command.MembershipId &&
                                      x.FieldId == command.FieldId,
                                 cancellationToken);

        if (scope is null)
            return false;

        dbContext.FieldScopes.Remove(scope);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MemberDto>> ListMembersAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var memberships = await dbContext.AuthoritativeMemberships
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Status == OrganizationMembershipStatuses.Active)
            .ToListAsync(cancellationToken);

        var membershipIds = memberships.Select(x => x.Id).ToList();
        var allScopes = await dbContext.FieldScopes
            .AsNoTracking()
            .Where(x => membershipIds.Contains(x.MembershipId))
            .ToListAsync(cancellationToken);

        var scopesByMembership = allScopes
            .GroupBy(x => x.MembershipId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.FieldId).ToList());

        return memberships.Select(m => new MemberDto(
            m.Id,
            m.UserId,
            m.Role,
            m.Status,
            m.CreatedAtUtc,
            scopesByMembership.TryGetValue(m.Id, out var s) ? s : [])).ToList();
    }

    public async Task<EffectivePermissionsResult?> GetEffectivePermissionsAsync(
        Guid organizationId,
        Guid userId,
        Guid? fieldId,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.AuthoritativeMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId &&
                                      x.UserId == userId &&
                                      x.Status == OrganizationMembershipStatuses.Active,
                                 cancellationToken);

        if (membership is null)
            return null;

        bool hasAllFieldsAccess = membership.Role is OrganizationMembershipRoles.Owner or OrganizationMembershipRoles.Admin;

        var fieldScopes = hasAllFieldsAccess
            ? (IReadOnlyList<Guid>)[]
            : await dbContext.FieldScopes
                .AsNoTracking()
                .Where(x => x.MembershipId == membership.Id)
                .Select(x => x.FieldId)
                .ToListAsync(cancellationToken);

        // If fieldId specified and user does not have all fields access, check if fieldId is in fieldScopes
        if (fieldId.HasValue && !hasAllFieldsAccess && !fieldScopes.Contains(fieldId.Value))
        {
            // Deny by default: resource out of scope returns null / no permission
            return null;
        }

        string[] allowedModules = membership.Role switch
        {
            OrganizationMembershipRoles.Owner or OrganizationMembershipRoles.Admin =>
                ["identity", "productive_core", "catalog", "territory", "operations", "finance", "weather"],
            OrganizationMembershipRoles.Agronomist =>
                ["productive_core", "catalog", "territory", "operations", "weather"],
            OrganizationMembershipRoles.Operator =>
                ["productive_core", "operations"],
            OrganizationMembershipRoles.Accountant =>
                ["finance", "inventory", "reports"],
            _ => ["productive_core", "catalog"]
        };

        return new EffectivePermissionsResult(
            organizationId,
            userId,
            membership.Role,
            hasAllFieldsAccess,
            fieldScopes,
            allowedModules);
    }
}

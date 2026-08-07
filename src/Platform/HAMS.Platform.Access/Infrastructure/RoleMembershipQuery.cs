using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Infrastructure;

/// <summary>
/// Deliberately creates its own short-lived <see cref="AccessDbContext"/> per call via
/// <see cref="IDbContextFactory{TContext}"/> instead of depending on the ambient scoped instance.
/// This service is called from places that legitimately run concurrently within the SAME DI scope
/// on Blazor Server - a page's own admin-check (e.g. NavMenu.razor, AssessmentMarks.razor) and the
/// SystemOrSchoolAdminPolicy authorization handler for that same request both resolve
/// IRoleMembershipQuery from one per-circuit scope. Sharing one scoped AccessDbContext across those
/// concurrent callers throws "A second operation was started on this context instance before a
/// previous operation completed" - EF Core's DbContext is not safe for concurrent use even from the
/// same logical request. A factory-created context per call has no shared state to race on.
/// </summary>
internal sealed class RoleMembershipQuery(IDbContextFactory<AccessDbContext> dbContextFactory) : IRoleMembershipQuery
{
    public async Task<bool> HasRoleAsync(Guid personId, string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.PersonRoleAssignments
            .Where(a => a.PersonId == personId)
            .ActiveAsOf(asOf)
            .Join(dbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Code)
            .AnyAsync(code => code == roleCode, cancellationToken);
    }

    public async Task<bool> HasAnyRoleAsync(Guid personId, IReadOnlyCollection<string> roleCodes, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.PersonRoleAssignments
            .Where(a => a.PersonId == personId)
            .ActiveAsOf(asOf)
            .Join(dbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Code)
            .AnyAsync(code => roleCodes.Contains(code), cancellationToken);
    }

    public async Task<bool> AnyPersonHasRoleAsync(string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.PersonRoleAssignments
            .ActiveAsOf(asOf)
            .Join(dbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Code)
            .AnyAsync(code => code == roleCode, cancellationToken);
    }
}

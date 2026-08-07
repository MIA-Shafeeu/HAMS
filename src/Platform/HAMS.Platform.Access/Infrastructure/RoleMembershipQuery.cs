using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Infrastructure;

internal sealed class RoleMembershipQuery(AccessDbContext dbContext) : IRoleMembershipQuery
{
    public Task<bool> HasRoleAsync(Guid personId, string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return dbContext.PersonRoleAssignments
            .Where(a => a.PersonId == personId)
            .ActiveAsOf(asOf)
            .Join(dbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Code)
            .AnyAsync(code => code == roleCode, cancellationToken);
    }

    public Task<bool> HasAnyRoleAsync(Guid personId, IReadOnlyCollection<string> roleCodes, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return dbContext.PersonRoleAssignments
            .Where(a => a.PersonId == personId)
            .ActiveAsOf(asOf)
            .Join(dbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Code)
            .AnyAsync(code => roleCodes.Contains(code), cancellationToken);
    }

    public Task<bool> AnyPersonHasRoleAsync(string roleCode, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return dbContext.PersonRoleAssignments
            .ActiveAsOf(asOf)
            .Join(dbContext.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Code)
            .AnyAsync(code => code == roleCode, cancellationToken);
    }
}

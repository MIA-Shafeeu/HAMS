using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Infrastructure;

internal sealed class PersonAccessScopeQuery(AccessDbContext dbContext) : IPersonAccessScopeQuery
{
    public async Task<IReadOnlyList<AccessGrantSummary>> GetActiveGrantsAsync(Guid personId, DateOnly asOf, CancellationToken cancellationToken = default)
        => await (
            from grant in dbContext.AccessGrants.Where(g => g.PersonId == personId).ActiveAsOf(asOf)
            join role in dbContext.Roles on grant.RoleId equals role.Id
            select new AccessGrantSummary(grant.SchoolId, grant.GradeId, grant.ClassId, grant.SubjectId, role.Code))
            .ToListAsync(cancellationToken);
}

using HAMS.Platform.Access.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Infrastructure;

internal sealed class AccessGrantProjectionService(AccessDbContext dbContext) : IAccessGrantProjectionService
{
    public async Task UpsertRoleGrantAsync(
        Guid personId, Guid roleId, Guid? schoolId, DateOnly effectiveFrom, DateOnly? effectiveTo, Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.AccessGrants.SingleOrDefaultAsync(
            g => g.SourceType == AccessGrantSourceTypes.PersonRoleAssignment && g.SourceId == sourceId,
            cancellationToken);

        if (existing is not null)
        {
            existing.EffectiveTo = effectiveTo;
            return;
        }

        dbContext.AccessGrants.Add(new AccessGrant
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            RoleId = roleId,
            SchoolId = schoolId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            SourceType = AccessGrantSourceTypes.PersonRoleAssignment,
            SourceId = sourceId,
        });
    }

    public async Task CloseAsync(string sourceType, Guid sourceId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var grants = await dbContext.AccessGrants
            .Where(g => g.SourceType == sourceType && g.SourceId == sourceId)
            .ToListAsync(cancellationToken);

        foreach (var grant in grants)
        {
            grant.EffectiveTo = effectiveTo;
        }
    }
}

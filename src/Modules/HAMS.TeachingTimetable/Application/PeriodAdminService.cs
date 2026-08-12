using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Application;

internal sealed class PeriodAdminService(TeachingTimetableDbContext dbContext) : IPeriodAdminService
{
    public async Task<IReadOnlyList<Period>> GetPeriodsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Periods.Where(p => p.SchoolId == schoolId).OrderBy(p => p.DisplayOrder).ToListAsync(cancellationToken);
}

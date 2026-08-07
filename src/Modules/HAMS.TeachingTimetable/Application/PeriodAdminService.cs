using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Application;

internal sealed class PeriodAdminService(TeachingTimetableDbContext dbContext) : IPeriodAdminService
{
    public async Task<Guid> CreatePeriodAsync(Guid schoolId, string code, string name, TimeOnly startTime, TimeOnly endTime, int displayOrder, CancellationToken cancellationToken = default)
    {
        var period = new Period { Id = Guid.NewGuid(), SchoolId = schoolId, Code = code, Name = name, StartTime = startTime, EndTime = endTime, DisplayOrder = displayOrder };
        dbContext.Periods.Add(period);
        await dbContext.SaveChangesAsync(cancellationToken);
        return period.Id;
    }

    public async Task<IReadOnlyList<Period>> GetPeriodsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Periods.Where(p => p.SchoolId == schoolId).OrderBy(p => p.DisplayOrder).ToListAsync(cancellationToken);
}

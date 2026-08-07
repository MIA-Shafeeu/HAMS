using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class SchoolCalendarService(OrgDbContext dbContext) : ISchoolCalendarService
{
    public Task<bool> IsWorkingDayOfWeekAsync(Guid schoolId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        => dbContext.WorkingDays.AnyAsync(w => w.SchoolId == schoolId && w.DayOfWeek == dayOfWeek, cancellationToken);

    public Task<bool> IsHolidayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
        => dbContext.Holidays.AnyAsync(h => h.SchoolId == schoolId && h.Date == date, cancellationToken);

    public async Task<bool> IsSchoolDayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
    {
        if (!await IsWorkingDayOfWeekAsync(schoolId, date.DayOfWeek, cancellationToken))
        {
            return false;
        }

        return !await IsHolidayAsync(schoolId, date, cancellationToken);
    }
}

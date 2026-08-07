using HAMS.OrgCurriculum.Application;

namespace HAMS.Attendance.Tests;

/// <summary>Defaults every day to a school day — pass specific non-school dates to test the retrofit rejecting them.</summary>
internal sealed class FakeSchoolCalendarService(params DateOnly[] nonSchoolDates) : ISchoolCalendarService
{
    private readonly HashSet<DateOnly> _nonSchoolDates = [.. nonSchoolDates];

    public Task<bool> IsWorkingDayOfWeekAsync(Guid schoolId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> IsHolidayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> IsSchoolDayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
        => Task.FromResult(!_nonSchoolDates.Contains(date));
}

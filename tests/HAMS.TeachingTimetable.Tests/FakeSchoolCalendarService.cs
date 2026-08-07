using HAMS.OrgCurriculum.Application;

namespace HAMS.TeachingTimetable.Tests;

/// <summary>Defaults every day to a working day (so existing conflict-check tests are unaffected) — pass specific non-working days to test the retrofit rejecting them.</summary>
internal sealed class FakeSchoolCalendarService(params DayOfWeek[] nonWorkingDays) : ISchoolCalendarService
{
    private readonly HashSet<DayOfWeek> _nonWorkingDays = [.. nonWorkingDays];

    public Task<bool> IsWorkingDayOfWeekAsync(Guid schoolId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        => Task.FromResult(!_nonWorkingDays.Contains(dayOfWeek));

    public Task<bool> IsHolidayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public async Task<bool> IsSchoolDayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
        => await IsWorkingDayOfWeekAsync(schoolId, date.DayOfWeek, cancellationToken) && !await IsHolidayAsync(schoolId, date, cancellationToken);
}

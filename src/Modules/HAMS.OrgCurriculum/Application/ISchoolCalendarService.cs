namespace HAMS.OrgCurriculum.Application;

/// <summary>
/// The one place "is this date a school day for this school?" gets resolved — consulted by
/// <c>ITimetableService</c> (TeachingTimetable, weekly recurring slots) and attendance marking
/// (HAMS.Attendance, specific dates), so the working-week/holiday rules only ever live here.
/// </summary>
public interface ISchoolCalendarService
{
    /// <summary>True if <paramref name="dayOfWeek"/> is configured as a working day for the school — the weekly recurring pattern, ignoring specific-date holidays.</summary>
    Task<bool> IsWorkingDayOfWeekAsync(Guid schoolId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    /// <summary>True if <paramref name="date"/> is a declared holiday for the school (public, religious, or school-declared).</summary>
    Task<bool> IsHolidayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>True if this specific calendar date is actually a school day: its day-of-week is a working day AND it isn't a declared holiday.</summary>
    Task<bool> IsSchoolDayAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default);
}

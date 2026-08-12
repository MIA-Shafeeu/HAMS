using HAMS.TeachingTimetable.Domain;

namespace HAMS.TeachingTimetable.Application;

/// <summary>
/// Read-only access to a school's <c>Period</c> rows — kept for inspection/debugging now that
/// Periods are an internal detail <c>ITimetableService.ScheduleAsync</c> finds-or-creates itself
/// from a raw start/end time (the whole-school timetable calendar replaced the standalone admin
/// Periods tab that used to author these directly).
/// </summary>
public interface IPeriodAdminService
{
    Task<IReadOnlyList<Period>> GetPeriodsAsync(Guid schoolId, CancellationToken cancellationToken = default);
}

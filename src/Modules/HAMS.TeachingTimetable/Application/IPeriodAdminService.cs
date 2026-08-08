using HAMS.TeachingTimetable.Domain;

namespace HAMS.TeachingTimetable.Application;

/// <summary>
/// A school's own configurable timetable-period structure (build plan Phase 4 scope) — extracted
/// from what had been purely inline <c>TeachingTimetableDbContext</c> queries directly inside
/// <c>TimetableEndpoints</c>' minimal-API lambdas, the same extraction already done for the
/// OrgCurriculum/PeopleEnrollment admin surfaces this session.
/// </summary>
public interface IPeriodAdminService
{
    Task<Guid> CreatePeriodAsync(Guid schoolId, string code, string name, TimeOnly startTime, TimeOnly endTime, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Period>> GetPeriodsAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>Renames/reschedules/reorders a <c>Period</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdatePeriodAsync(Guid id, string name, TimeOnly startTime, TimeOnly endTime, int displayOrder, CancellationToken cancellationToken = default);
}

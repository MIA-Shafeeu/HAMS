using HAMS.TeachingTimetable.Domain;

namespace HAMS.TeachingTimetable.Application;

/// <summary>
/// A mobile/staff-facing "my schedule" row (build plan Phase 14) — pre-resolves Subject/Class/Period
/// display names server-side so a real HTTP client (unlike a Blazor page's own DI-injected lookups)
/// doesn't need N additional round trips per entry to render a readable timetable.
/// </summary>
public sealed record StaffTimetableEntry(
    Guid Id, Guid ClassId, string SubjectName, string ClassName, DayOfWeek DayOfWeek, string PeriodName,
    TimeOnly PeriodStartTime, TimeOnly PeriodEndTime);

/// <summary>
/// One weekly recurring slot, resolved for the whole-school timetable calendar admin page — every
/// class at once, unlike <see cref="StaffTimetableEntry"/> (one staff member) or
/// <c>GetEntriesForClassAsync</c> (one class). <see cref="StaffPersonId"/> is deliberately left for
/// the caller (a Razor Page, which already has a school-wide staff list loaded) to resolve into a
/// display name — resolving person names is a PeopleEnrollment concern this module doesn't
/// reference, matching how <c>TeachingAssignmentsTimetableModel.StaffName</c> already works today.
/// </summary>
public sealed record SchoolTimetableEntry(
    Guid Id, Guid ClassId, string ClassName, string ColorHex, Guid SubjectId, string SubjectName, Guid StaffPersonId,
    DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>
/// Places/removes weekly recurring <c>TimetableEntry</c> slots, enforcing the two conflicts build
/// plan §4 calls out: a class can't have two subjects in the same day/period, and a staff member
/// can't be teaching two classes in the same day/period (resolved by joining through
/// <c>TeachingAssignmentId</c>, since <c>TimetableEntry</c> doesn't store the staff member directly).
/// Also enforces the school's configured working week (<c>ISchoolCalendarService</c>,
/// OrgCurriculum) — a day can only be scheduled if it's one of the school's configured working
/// days (Sunday-Thursday by default for a Maldivian school, but never hardcoded as such).
/// </summary>
public interface ITimetableService
{
    /// <summary>
    /// Schedules a slot at a raw start/end time — <c>Period</c> rows are an internal implementation
    /// detail found-or-created here (matched by exact <c>(SchoolId, StartTime, EndTime)</c>), not a
    /// concept the caller manages directly. Both conflict checks are real time-interval overlap
    /// (not exact-Period-equality), so two different auto-created Periods that merely overlap are
    /// still correctly rejected.
    /// </summary>
    /// <exception cref="InvalidOperationException">The day isn't a configured working day for the school, or the class/staff member already has something overlapping scheduled in that slot.</exception>
    Task<Guid> ScheduleAsync(
        Guid schoolId, Guid classId, Guid subjectId, Guid teachingAssignmentId, Guid academicYearId, DayOfWeek dayOfWeek,
        TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid timetableEntryId, CancellationToken cancellationToken = default);

    /// <summary>Every weekly recurring slot for this class/year, ordered for display — the first read of its kind (the endpoint layer previously queried <c>TeachingTimetableDbContext</c> directly).</summary>
    Task<IReadOnlyList<TimetableEntry>> GetEntriesForClassAsync(Guid classId, Guid academicYearId, CancellationToken cancellationToken = default);

    /// <summary>
    /// "What am I teaching this week" (build plan Phase 14 — mobile's own daily-workflow entry
    /// point). Resolves the staff member's slots by joining through <c>SubjectTeachingAssignment</c>
    /// the same way the conflict checks in <see cref="ScheduleAsync"/> already do, filtered to
    /// whichever assignment is effective-dated as of <paramref name="asOf"/> (a substitution that's
    /// expired doesn't show up; one starting today does).
    /// </summary>
    Task<IReadOnlyList<StaffTimetableEntry>> GetEntriesForStaffAsync(
        Guid staffPersonId, Guid schoolId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>Every weekly recurring slot for the WHOLE school/year, across all classes — backs the whole-school timetable calendar's event feed.</summary>
    Task<IReadOnlyList<SchoolTimetableEntry>> GetEntriesForSchoolAsync(Guid schoolId, Guid academicYearId, CancellationToken cancellationToken = default);
}

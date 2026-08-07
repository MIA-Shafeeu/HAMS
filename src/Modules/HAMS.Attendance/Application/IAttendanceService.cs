namespace HAMS.Attendance.Application;

/// <summary>
/// Marks daily/lesson attendance (build plan Phase 5 scope: "daily+lesson attendance"). Daily
/// marking is rejected outright for any date that isn't a real school day for the school
/// (its configured working days AND-ed with declared holidays, via OrgCurriculum's
/// <c>ISchoolCalendarService</c>) — per the user's explicit instruction that the working week and
/// holiday calendar must be real, checked configuration, not an assumption baked into attendance
/// logic. Both marking operations upsert by their natural key, so correcting an earlier mistake
/// the same day doesn't require a separate "revise" flow the way <c>GuardianStudentRelationship</c>
/// needs one — attendance marks aren't legally sensitive history in the same way.
/// </summary>
public interface IAttendanceService
{
    /// <exception cref="InvalidOperationException"><paramref name="date"/> is not a school day for this school.</exception>
    Task<Guid> MarkDailyAttendanceAsync(
        Guid schoolId, Guid studentPersonId, DateOnly date, Guid academicYearId, string attendanceStatusCode,
        Guid recordedByPersonId, string? notes, CancellationToken cancellationToken = default);

    Task<Guid> MarkLessonAttendanceAsync(
        Guid studentPersonId, Guid lessonSessionId, string attendanceStatusCode, Guid recordedByPersonId, string? notes,
        CancellationToken cancellationToken = default);
}

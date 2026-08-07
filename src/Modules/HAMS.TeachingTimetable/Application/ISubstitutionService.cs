namespace HAMS.TeachingTimetable.Application;

/// <summary>
/// Covers a single day of an existing <c>SubjectTeachingAssignment</c> with a substitute teacher
/// (build plan §3, TIM-FR-008/009, AC-022) — generates a linked, single-day
/// <c>SubjectTeachingAssignment</c> (Role=Substitute) via the exact same assign path ordinary
/// assignments use, so the substitute gets the exact same Subject Teacher permission code, scoped
/// to the exact same subject+class, auto-expiring the day after the substitution date with no
/// scheduled job required.
/// </summary>
public interface ISubstitutionService
{
    Task<Guid> CreateSubstitutionAsync(
        Guid originalAssignmentId, Guid substituteStaffPersonId, DateOnly substitutionDate, Guid? schoolId,
        string? reason, CancellationToken cancellationToken = default);
}

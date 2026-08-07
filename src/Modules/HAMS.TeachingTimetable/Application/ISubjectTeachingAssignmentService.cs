using HAMS.TeachingTimetable.Domain;

namespace HAMS.TeachingTimetable.Application;

/// <summary>
/// Assigns/ends a <c>SubjectTeachingAssignment</c>, projecting the Subject Teacher
/// <c>AccessGrant</c> (scoped to the subject+class) atomically with it via
/// <c>IScopedAccessGrantProjector</c> — build plan §4.
/// </summary>
public interface ISubjectTeachingAssignmentService
{
    Task<Guid> AssignAsync(
        Guid staffPersonId, Guid subjectId, Guid classId, Guid academicYearId, Guid? schoolId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default);

    Task EndAsync(Guid assignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default);

    /// <summary>Every subject-teaching assignment for this class/year — includes substitute-role rows, since a substitution is just an ordinary row with <see cref="AssignmentRoleCodes.Substitute"/> (build plan §3). The first read of its kind — every prior caller only ever wrote/ended one already-known assignment.</summary>
    Task<IReadOnlyList<SubjectTeachingAssignment>> GetAssignmentsForClassAsync(Guid classId, Guid academicYearId, CancellationToken cancellationToken = default);
}

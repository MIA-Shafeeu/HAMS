using HAMS.TeachingTimetable.Domain;

namespace HAMS.TeachingTimetable.Application;

/// <summary>Assigns/ends a <c>LeadingTeacherAssignment</c>, projecting the Leading Teacher <c>AccessGrant</c> (scoped to the subject) atomically with it.</summary>
public interface ILeadingTeacherAssignmentService
{
    Task<Guid> AssignAsync(
        Guid staffPersonId, Guid subjectId, Guid academicYearId, Guid? schoolId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default);

    Task EndAsync(Guid assignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeadingTeacherAssignment>> GetAssignmentsForSubjectAsync(Guid subjectId, Guid academicYearId, CancellationToken cancellationToken = default);
}

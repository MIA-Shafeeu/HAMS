using HAMS.TeachingTimetable.Domain;

namespace HAMS.TeachingTimetable.Application;

/// <summary>Assigns/ends a <c>ClassTeacherAssignment</c>, projecting the Class Teacher <c>AccessGrant</c> (scoped to the class) atomically with it.</summary>
public interface IClassTeacherAssignmentService
{
    Task<Guid> AssignAsync(
        Guid staffPersonId, Guid classId, Guid academicYearId, Guid? schoolId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default);

    Task EndAsync(Guid assignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassTeacherAssignment>> GetAssignmentsForClassAsync(Guid classId, Guid academicYearId, CancellationToken cancellationToken = default);
}

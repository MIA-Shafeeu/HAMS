using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Application;

/// <summary>
/// Concrete type exposes <see cref="AssignWithRoleAsync"/> internally so <c>SubstitutionService</c>
/// can reuse the exact same assignment+grant-projection path with
/// <see cref="AssignmentRoleCodes.Substitute"/> instead of duplicating it — both are registered
/// against this same instance in DI (see <c>TeachingTimetableModule</c>).
/// </summary>
internal sealed class SubjectTeachingAssignmentService(
    TeachingTimetableDbContext dbContext, AccessDbContext accessDbContext, IScopedAccessGrantProjector projector)
    : ISubjectTeachingAssignmentService
{
    public Task<Guid> AssignAsync(
        Guid staffPersonId, Guid subjectId, Guid classId, Guid academicYearId, Guid? schoolId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
        => AssignWithRoleAsync(staffPersonId, subjectId, classId, academicYearId, schoolId, AssignmentRoleCodes.Ordinary, effectiveFrom, effectiveTo, cancellationToken);

    /// <param name="assignmentId">
    /// Lets <c>SubstitutionService</c> pre-determine the generated assignment's id so it can stage
    /// its own <c>SubstitutionRecord</c> (referencing that id) on the same <c>dbContext</c>
    /// *before* calling this — both then land in the one transaction this method opens via
    /// <see cref="IScopedAccessGrantProjector"/>, since <c>SaveChangesAsync</c> persists every
    /// staged change, not just the one added inside its own callback.
    /// </param>
    internal async Task<Guid> AssignWithRoleAsync(
        Guid staffPersonId, Guid subjectId, Guid classId, Guid academicYearId, Guid? schoolId, string assignmentRoleCode,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken, Guid? assignmentId = null)
    {
        var assignmentRoleId = await dbContext.AssignmentRoles
            .Where(r => r.Code == assignmentRoleCode).Select(r => r.Id).SingleAsync(cancellationToken);
        var subjectTeacherRoleId = await accessDbContext.Roles
            .Where(r => r.Code == RoleCodes.SubjectTeacher).Select(r => r.Id).SingleAsync(cancellationToken);

        var assignment = new SubjectTeachingAssignment
        {
            Id = assignmentId ?? Guid.NewGuid(),
            StaffPersonId = staffPersonId,
            SubjectId = subjectId,
            ClassId = classId,
            AcademicYearId = academicYearId,
            AssignmentRoleId = assignmentRoleId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
        };

        var grant = new ScopedAccessGrant(
            PersonId: staffPersonId, RoleId: subjectTeacherRoleId, SchoolId: schoolId, CampusId: null,
            AcademicYearId: academicYearId, KeyStageId: null, GradeId: null, ClassId: classId, SubjectId: subjectId, StudentId: null,
            EffectiveFrom: effectiveFrom, EffectiveTo: effectiveTo,
            SourceType: AccessGrantSourceTypes.SubjectTeachingAssignment, SourceId: assignment.Id);

        await projector.ProjectAsync(dbContext, () => dbContext.SubjectTeachingAssignments.Add(assignment), grant, cancellationToken);

        return assignment.Id;
    }

    public async Task EndAsync(Guid assignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.SubjectTeachingAssignments.FindAsync([assignmentId], cancellationToken)
            ?? throw new InvalidOperationException("Teaching assignment not found.");

        var subjectTeacherRoleId = await accessDbContext.Roles
            .Where(r => r.Code == RoleCodes.SubjectTeacher).Select(r => r.Id).SingleAsync(cancellationToken);

        var grant = new ScopedAccessGrant(
            PersonId: assignment.StaffPersonId, RoleId: subjectTeacherRoleId, SchoolId: null, CampusId: null,
            AcademicYearId: assignment.AcademicYearId, KeyStageId: null, GradeId: null, ClassId: assignment.ClassId,
            SubjectId: assignment.SubjectId, StudentId: null,
            EffectiveFrom: assignment.EffectiveFrom, EffectiveTo: effectiveTo,
            SourceType: AccessGrantSourceTypes.SubjectTeachingAssignment, SourceId: assignment.Id);

        await projector.ProjectAsync(dbContext, () => assignment.EffectiveTo = effectiveTo, grant, cancellationToken);
    }

    public async Task<IReadOnlyList<SubjectTeachingAssignment>> GetAssignmentsForClassAsync(Guid classId, Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.SubjectTeachingAssignments
            .Where(a => a.ClassId == classId && a.AcademicYearId == academicYearId)
            .OrderByDescending(a => a.EffectiveFrom)
            .ToListAsync(cancellationToken);
}

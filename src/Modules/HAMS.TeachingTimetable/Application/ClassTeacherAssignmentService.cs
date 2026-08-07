using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Application;

internal sealed class ClassTeacherAssignmentService(
    TeachingTimetableDbContext dbContext, AccessDbContext accessDbContext, IScopedAccessGrantProjector projector)
    : IClassTeacherAssignmentService
{
    public async Task<Guid> AssignAsync(
        Guid staffPersonId, Guid classId, Guid academicYearId, Guid? schoolId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
    {
        var classTeacherRoleId = await accessDbContext.Roles
            .Where(r => r.Code == RoleCodes.ClassTeacher).Select(r => r.Id).SingleAsync(cancellationToken);

        var assignment = new ClassTeacherAssignment
        {
            Id = Guid.NewGuid(), StaffPersonId = staffPersonId, ClassId = classId, AcademicYearId = academicYearId,
            EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo,
        };

        var grant = new ScopedAccessGrant(
            PersonId: staffPersonId, RoleId: classTeacherRoleId, SchoolId: schoolId, CampusId: null,
            AcademicYearId: academicYearId, KeyStageId: null, GradeId: null, ClassId: classId, SubjectId: null, StudentId: null,
            EffectiveFrom: effectiveFrom, EffectiveTo: effectiveTo,
            SourceType: AccessGrantSourceTypes.ClassTeacherAssignment, SourceId: assignment.Id);

        await projector.ProjectAsync(dbContext, () => dbContext.ClassTeacherAssignments.Add(assignment), grant, cancellationToken);

        return assignment.Id;
    }

    public async Task EndAsync(Guid assignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.ClassTeacherAssignments.FindAsync([assignmentId], cancellationToken)
            ?? throw new InvalidOperationException("Class teacher assignment not found.");

        var classTeacherRoleId = await accessDbContext.Roles
            .Where(r => r.Code == RoleCodes.ClassTeacher).Select(r => r.Id).SingleAsync(cancellationToken);

        var grant = new ScopedAccessGrant(
            PersonId: assignment.StaffPersonId, RoleId: classTeacherRoleId, SchoolId: null, CampusId: null,
            AcademicYearId: assignment.AcademicYearId, KeyStageId: null, GradeId: null, ClassId: assignment.ClassId,
            SubjectId: null, StudentId: null,
            EffectiveFrom: assignment.EffectiveFrom, EffectiveTo: effectiveTo,
            SourceType: AccessGrantSourceTypes.ClassTeacherAssignment, SourceId: assignment.Id);

        await projector.ProjectAsync(dbContext, () => assignment.EffectiveTo = effectiveTo, grant, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassTeacherAssignment>> GetAssignmentsForClassAsync(Guid classId, Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.ClassTeacherAssignments
            .Where(a => a.ClassId == classId && a.AcademicYearId == academicYearId)
            .OrderByDescending(a => a.EffectiveFrom)
            .ToListAsync(cancellationToken);
}

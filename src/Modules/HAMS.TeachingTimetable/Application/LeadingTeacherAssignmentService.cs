using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Application;

internal sealed class LeadingTeacherAssignmentService(
    TeachingTimetableDbContext dbContext, AccessDbContext accessDbContext, IScopedAccessGrantProjector projector)
    : ILeadingTeacherAssignmentService
{
    public async Task<Guid> AssignAsync(
        Guid staffPersonId, Guid subjectId, Guid academicYearId, Guid? schoolId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
    {
        var leadingTeacherRoleId = await accessDbContext.Roles
            .Where(r => r.Code == RoleCodes.LeadingTeacher).Select(r => r.Id).SingleAsync(cancellationToken);

        var assignment = new LeadingTeacherAssignment
        {
            Id = Guid.NewGuid(), StaffPersonId = staffPersonId, SubjectId = subjectId, AcademicYearId = academicYearId,
            EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo,
        };

        var grant = new ScopedAccessGrant(
            PersonId: staffPersonId, RoleId: leadingTeacherRoleId, SchoolId: schoolId, CampusId: null,
            AcademicYearId: academicYearId, KeyStageId: null, GradeId: null, ClassId: null, SubjectId: subjectId, StudentId: null,
            EffectiveFrom: effectiveFrom, EffectiveTo: effectiveTo,
            SourceType: AccessGrantSourceTypes.LeadingTeacherAssignment, SourceId: assignment.Id);

        await projector.ProjectAsync(dbContext, () => dbContext.LeadingTeacherAssignments.Add(assignment), grant, cancellationToken);

        return assignment.Id;
    }

    public async Task EndAsync(Guid assignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.LeadingTeacherAssignments.FindAsync([assignmentId], cancellationToken)
            ?? throw new InvalidOperationException("Leading teacher assignment not found.");

        var leadingTeacherRoleId = await accessDbContext.Roles
            .Where(r => r.Code == RoleCodes.LeadingTeacher).Select(r => r.Id).SingleAsync(cancellationToken);

        var grant = new ScopedAccessGrant(
            PersonId: assignment.StaffPersonId, RoleId: leadingTeacherRoleId, SchoolId: null, CampusId: null,
            AcademicYearId: assignment.AcademicYearId, KeyStageId: null, GradeId: null, ClassId: null,
            SubjectId: assignment.SubjectId, StudentId: null,
            EffectiveFrom: assignment.EffectiveFrom, EffectiveTo: effectiveTo,
            SourceType: AccessGrantSourceTypes.LeadingTeacherAssignment, SourceId: assignment.Id);

        await projector.ProjectAsync(dbContext, () => assignment.EffectiveTo = effectiveTo, grant, cancellationToken);
    }

    public async Task<IReadOnlyList<LeadingTeacherAssignment>> GetAssignmentsForSubjectAsync(Guid subjectId, Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.LeadingTeacherAssignments
            .Where(a => a.SubjectId == subjectId && a.AcademicYearId == academicYearId)
            .OrderByDescending(a => a.EffectiveFrom)
            .ToListAsync(cancellationToken);
}

using HAMS.OrgCurriculum.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Application;

internal sealed class StaffAccessScopeQuery(
    IPersonAccessScopeQuery accessGrants, TeachingTimetableDbContext dbContext, IOrgStructureLookup orgLookup)
    : IStaffAccessScopeQuery
{
    // The admin "Assign Role" form (StaffAccountsRoles.cshtml) has no Class/Subject picker at all -
    // every grant it produces has ClassId/SubjectId both null, for WHATEVER role is picked. A grant
    // shaped that way only means "whole school" (or, for SystemAdministrator, "every school") for
    // the specific roles that are actually meant to carry that breadth; for any other role code
    // (Class/Subject/Leading Teacher, or a school's own future custom role) that shape is a
    // misconfiguration relative to the role's real semantics - fail closed (contributes nothing)
    // rather than silently treating a bare role assignment as a data-access grant it was never
    // meant to be. Class/Subject Teacher's real access always arrives with a real ClassId already
    // set (ClassTeacherAssignmentService/SubjectTeachingAssignmentService never leave it null), and
    // Leading Teacher's always arrives with a real SubjectId - neither goes through this list.
    private static readonly string[] UnrestrictedRoleCodes = [RoleCodes.SystemAdministrator, RoleCodes.SchoolAdministrator];
    private static readonly string[] WholeSchoolRoleCodes = [RoleCodes.SystemAdministrator, RoleCodes.SchoolAdministrator, RoleCodes.Principal, RoleCodes.DeputyPrincipal];

    public async Task<StaffAccessScope> GetScopeAsync(
        Guid personId, DateOnly asOf, Guid? schoolId, Guid? academicYearId, CancellationToken cancellationToken = default)
    {
        var grants = await accessGrants.GetActiveGrantsAsync(personId, asOf, cancellationToken);

        var hasUnrestrictedAccess = grants.Any(g => g.SchoolId is null && UnrestrictedRoleCodes.Contains(g.RoleCode));
        var schoolIds = grants.Where(g => g.SchoolId is not null).Select(g => g.SchoolId!.Value).Distinct().ToList();

        if (hasUnrestrictedAccess || schoolId is not { } school || academicYearId is not { } academicYear)
        {
            return new StaffAccessScope(hasUnrestrictedAccess, schoolIds, [], []);
        }

        // Only this specific school's grants matter from here on - a grant scoped to a different
        // school (e.g. this person also teaches at School B) has no bearing on what they can see
        // inside School A.
        var grantsForSchool = grants.Where(g => g.SchoolId == school).ToList();

        // ClassId == null AND SubjectId == null (but SchoolId set) is the whole-school shape
        // PersonRoleAssignmentService projects for Principal/Deputy Principal/School Administrator
        // - every grade and class in the school, not just a subset of either - but ONLY for a role
        // actually in WholeSchoolRoleCodes; see that field's own remarks for why shape alone isn't enough.
        if (grantsForSchool.Any(g => g.ClassId is null && g.SubjectId is null && WholeSchoolRoleCodes.Contains(g.RoleCode)))
        {
            // GetClassesAsync(academicYearId) alone is already school-scoped in effect - an
            // AcademicYear belongs to exactly one School, so every Class it returns does too;
            // no separate SchoolId filter is needed (ClassOption doesn't even carry one).
            var allGradeIds = (await orgLookup.GetGradesAsync(school, cancellationToken)).Select(g => g.Id).ToList();
            var allClassIds = (await orgLookup.GetClassesAsync(academicYear, cancellationToken)).Select(c => c.Id).ToList();
            return new StaffAccessScope(false, [school], allGradeIds, allClassIds);
        }

        var classIds = new HashSet<Guid>(grantsForSchool.Where(g => g.ClassId is not null).Select(g => g.ClassId!.Value));

        // Leading Teacher's grant shape: SubjectId set, ClassId null - "every class currently
        // teaching this subject" (ILeadingTeacherAssignmentService's own design note: AccessGrant
        // has no Learning-Area dimension, so this is deliberately resolved via a join here rather
        // than being a flat grant dimension the generic ScopeAuthorizationHandler could read directly).
        var subjectIds = grantsForSchool.Where(g => g.ClassId is null && g.SubjectId is not null).Select(g => g.SubjectId!.Value).Distinct().ToList();
        if (subjectIds.Count > 0)
        {
            var classIdsForSubjects = await dbContext.SubjectTeachingAssignments
                .Where(a => subjectIds.Contains(a.SubjectId) && a.AcademicYearId == academicYear)
                .ActiveAsOf(asOf)
                .Select(a => a.ClassId)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var classId in classIdsForSubjects)
            {
                classIds.Add(classId);
            }
        }

        var gradeIds = new HashSet<Guid>();
        foreach (var classId in classIds)
        {
            foreach (var gradeId in await orgLookup.GetClassGradeIdsAsync(classId, cancellationToken))
            {
                gradeIds.Add(gradeId);
            }
        }

        return new StaffAccessScope(false, [school], gradeIds.ToList(), classIds.ToList());
    }
}

using HAMS.OrgCurriculum.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class StaffAccessScopeQueryTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);

    private static TeachingTimetableDbContext CreateTeachingContext() => new(
        new DbContextOptionsBuilder<TeachingTimetableDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Person_with_no_grants_at_all_can_access_nothing()
    {
        await using var db = CreateTeachingContext();
        var query = new StaffAccessScopeQuery(new FakeAccessGrantQuery(), db, new FakeOrgStructureLookup());

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, schoolId: null, academicYearId: null);

        Assert.False(scope.HasUnrestrictedAccess);
        Assert.Empty(scope.SchoolIds);
        Assert.False(scope.CanAccessSchool(Guid.NewGuid()));
    }

    [Fact]
    public async Task Wildcard_grant_grants_unrestricted_access_System_or_School_Administrator()
    {
        await using var db = CreateTeachingContext();
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(null, null, null, null, RoleCodes.SystemAdministrator)), db, new FakeOrgStructureLookup());

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, schoolId: null, academicYearId: null);

        Assert.True(scope.HasUnrestrictedAccess);
        Assert.True(scope.CanAccessSchool(Guid.NewGuid()));
        Assert.True(scope.CanAccessGrade(Guid.NewGuid()));
        Assert.True(scope.CanAccessClass(Guid.NewGuid()));
    }

    [Fact]
    public async Task Class_scoped_grant_Class_Teacher_can_only_access_its_own_class_and_grade()
    {
        var school = Guid.NewGuid();
        var academicYear = Guid.NewGuid();
        var myClass = Guid.NewGuid();
        var otherClass = Guid.NewGuid();
        var myGrade = Guid.NewGuid();

        await using var db = CreateTeachingContext();
        var orgLookup = new FakeOrgStructureLookup().WithClassGrades(myClass, myGrade);
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(school, null, myClass, null, RoleCodes.ClassTeacher)), db, orgLookup);

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, school, academicYear);

        Assert.False(scope.HasUnrestrictedAccess);
        Assert.True(scope.CanAccessClass(myClass));
        Assert.False(scope.CanAccessClass(otherClass));
        Assert.True(scope.CanAccessGrade(myGrade));
        Assert.False(scope.CanAccessGrade(Guid.NewGuid()));
    }

    [Fact]
    public async Task Whole_school_grant_Principal_can_access_every_grade_and_class_in_that_school()
    {
        var school = Guid.NewGuid();
        var academicYear = Guid.NewGuid();
        var gradeA = Guid.NewGuid();
        var gradeB = Guid.NewGuid();
        var classA = Guid.NewGuid();
        var classB = Guid.NewGuid();

        await using var db = CreateTeachingContext();
        var orgLookup = new FakeOrgStructureLookup()
            .WithGradesForSchool(new GradeOption(gradeA, "GA", "Grade A"), new GradeOption(gradeB, "GB", "Grade B"))
            .WithClass(classA, "Class A").WithClass(classB, "Class B");
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(school, null, null, null, RoleCodes.Principal)), db, orgLookup);

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, school, academicYear);

        Assert.False(scope.HasUnrestrictedAccess);
        Assert.True(scope.CanAccessGrade(gradeA));
        Assert.True(scope.CanAccessGrade(gradeB));
        Assert.True(scope.CanAccessClass(classA));
        Assert.True(scope.CanAccessClass(classB));
        // A different school's grant must never leak into this school's resolved scope.
        Assert.False(scope.CanAccessSchool(Guid.NewGuid()));
    }

    [Fact]
    public async Task Subject_only_grant_Leading_Teacher_can_access_every_class_currently_teaching_that_subject()
    {
        var school = Guid.NewGuid();
        var academicYear = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classTeachingSubject = Guid.NewGuid();
        var classNotTeachingSubject = Guid.NewGuid();
        var gradeForThatClass = Guid.NewGuid();

        await using var db = CreateTeachingContext();
        db.SubjectTeachingAssignments.Add(new SubjectTeachingAssignment
        {
            Id = Guid.NewGuid(), StaffPersonId = Guid.NewGuid(), SubjectId = subjectId, ClassId = classTeachingSubject,
            AcademicYearId = academicYear, AssignmentRoleId = Guid.NewGuid(), EffectiveFrom = Today.AddDays(-30),
        });
        await db.SaveChangesAsync();

        var orgLookup = new FakeOrgStructureLookup().WithClassGrades(classTeachingSubject, gradeForThatClass);
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(school, null, null, subjectId, RoleCodes.LeadingTeacher)), db, orgLookup);

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, school, academicYear);

        Assert.True(scope.CanAccessClass(classTeachingSubject));
        Assert.False(scope.CanAccessClass(classNotTeachingSubject));
        Assert.True(scope.CanAccessGrade(gradeForThatClass));
    }

    [Fact]
    public async Task Leading_Teacher_does_not_see_a_class_whose_subject_assignment_has_already_ended()
    {
        var school = Guid.NewGuid();
        var academicYear = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var expiredClass = Guid.NewGuid();

        await using var db = CreateTeachingContext();
        db.SubjectTeachingAssignments.Add(new SubjectTeachingAssignment
        {
            Id = Guid.NewGuid(), StaffPersonId = Guid.NewGuid(), SubjectId = subjectId, ClassId = expiredClass,
            AcademicYearId = academicYear, AssignmentRoleId = Guid.NewGuid(),
            EffectiveFrom = Today.AddDays(-60), EffectiveTo = Today.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(school, null, null, subjectId, RoleCodes.LeadingTeacher)), db, new FakeOrgStructureLookup());

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, school, academicYear);

        Assert.False(scope.CanAccessClass(expiredClass));
    }

    [Fact]
    public async Task Grants_for_a_different_school_never_leak_into_the_requested_schools_scope()
    {
        var mySchool = Guid.NewGuid();
        var otherSchool = Guid.NewGuid();
        var academicYear = Guid.NewGuid();
        var classAtOtherSchool = Guid.NewGuid();

        await using var db = CreateTeachingContext();
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(otherSchool, null, classAtOtherSchool, null, RoleCodes.ClassTeacher)), db, new FakeOrgStructureLookup());

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, mySchool, academicYear);

        Assert.False(scope.HasUnrestrictedAccess);
        Assert.False(scope.CanAccessClass(classAtOtherSchool));
        Assert.Empty(scope.ClassIds);
    }

    [Fact]
    public async Task School_only_query_with_no_academic_year_resolves_just_the_accessible_schools()
    {
        var school = Guid.NewGuid();
        await using var db = CreateTeachingContext();
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(school, null, Guid.NewGuid(), null, RoleCodes.ClassTeacher)), db, new FakeOrgStructureLookup());

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, schoolId: null, academicYearId: null);

        Assert.True(scope.CanAccessSchool(school));
        Assert.Empty(scope.GradeIds);
        Assert.Empty(scope.ClassIds);
    }

    [Fact]
    public async Task Class_Teacher_role_assigned_generically_without_a_class_grants_nothing()
    {
        // The generic admin "Assign Role" form has no Class/Subject picker - it can only ever
        // produce a grant shaped like this (School set, Class/Subject both null) for ANY role. That
        // shape must NOT be silently treated as "whole school" for a role whose real semantics are
        // "one specific class" - a Class Teacher's genuine access always arrives with a real ClassId
        // already set, via ClassTeacherAssignmentService, never through this path.
        var school = Guid.NewGuid();
        var academicYear = Guid.NewGuid();

        await using var db = CreateTeachingContext();
        var orgLookup = new FakeOrgStructureLookup().WithGradesForSchool(new GradeOption(Guid.NewGuid(), "GA", "Grade A"));
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(school, null, null, null, RoleCodes.ClassTeacher)), db, orgLookup);

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, school, academicYear);

        Assert.False(scope.HasUnrestrictedAccess);
        Assert.Empty(scope.GradeIds);
        Assert.Empty(scope.ClassIds);
        Assert.False(scope.CanAccessGrade(Guid.NewGuid()));
    }

    [Fact]
    public async Task Wildcard_school_grant_for_a_non_admin_role_does_not_grant_unrestricted_access()
    {
        // Same misuse-of-the-generic-form scenario as above, but with "All Schools" (SchoolId
        // null) picked for a Class Teacher - must fail closed, not silently escalate to global access.
        await using var db = CreateTeachingContext();
        var query = new StaffAccessScopeQuery(
            new FakeAccessGrantQuery(new AccessGrantSummary(null, null, null, null, RoleCodes.ClassTeacher)), db, new FakeOrgStructureLookup());

        var scope = await query.GetScopeAsync(Guid.NewGuid(), Today, schoolId: null, academicYearId: null);

        Assert.False(scope.HasUnrestrictedAccess);
        Assert.False(scope.CanAccessSchool(Guid.NewGuid()));
    }
}

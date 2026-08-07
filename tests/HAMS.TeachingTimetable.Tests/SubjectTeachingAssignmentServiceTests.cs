using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class SubjectTeachingAssignmentServiceTests
{
    private static readonly Guid SubjectTeacherRoleId = Guid.NewGuid();

    private static TeachingTimetableDbContext CreateTeachingContext()
    {
        var db = new TeachingTimetableDbContext(
            new DbContextOptionsBuilder<TeachingTimetableDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.AssignmentRoles.Add(new AssignmentRole { Id = Guid.NewGuid(), Code = AssignmentRoleCodes.Ordinary, Name = "Ordinary" });
        db.AssignmentRoles.Add(new AssignmentRole { Id = Guid.NewGuid(), Code = AssignmentRoleCodes.Substitute, Name = "Substitute" });
        db.SaveChanges();
        return db;
    }

    private static AccessDbContext CreateAccessContext()
    {
        var db = new AccessDbContext(new DbContextOptionsBuilder<AccessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.Add(new Role { Id = SubjectTeacherRoleId, Code = RoleCodes.SubjectTeacher, Name = "Subject Teacher" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task AssignAsync_creates_the_assignment_and_a_matching_scoped_grant()
    {
        await using var teachingDb = CreateTeachingContext();
        await using var accessDb = CreateAccessContext();
        var projector = new FakeScopedAccessGrantProjector();
        var service = new SubjectTeachingAssignmentService(teachingDb, accessDb, projector);

        var staffPersonId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var assignmentId = await service.AssignAsync(
            staffPersonId, subjectId, classId, academicYearId, schoolId, new DateOnly(2026, 1, 1), null);

        var assignment = await teachingDb.SubjectTeachingAssignments.SingleAsync(a => a.Id == assignmentId);
        Assert.Equal(staffPersonId, assignment.StaffPersonId);

        var ordinaryRole = await teachingDb.AssignmentRoles.SingleAsync(r => r.Code == AssignmentRoleCodes.Ordinary);
        Assert.Equal(ordinaryRole.Id, assignment.AssignmentRoleId);

        Assert.NotNull(projector.LastGrant);
        var grant = projector.LastGrant!;
        Assert.Equal(staffPersonId, grant.PersonId);
        Assert.Equal(SubjectTeacherRoleId, grant.RoleId);
        Assert.Equal(schoolId, grant.SchoolId);
        Assert.Equal(subjectId, grant.SubjectId);
        Assert.Equal(classId, grant.ClassId);
        Assert.Null(grant.GradeId); // wildcard — not restricted by grade
        Assert.Null(grant.StudentId); // wildcard — a subject teacher isn't scoped to one student
    }

    [Fact]
    public async Task EndAsync_closes_the_assignment_and_updates_the_grants_effective_to()
    {
        await using var teachingDb = CreateTeachingContext();
        await using var accessDb = CreateAccessContext();
        var projector = new FakeScopedAccessGrantProjector();
        var service = new SubjectTeachingAssignmentService(teachingDb, accessDb, projector);

        var assignmentId = await service.AssignAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), null);

        await service.EndAsync(assignmentId, new DateOnly(2026, 6, 30));

        var assignment = await teachingDb.SubjectTeachingAssignments.SingleAsync(a => a.Id == assignmentId);
        Assert.Equal(new DateOnly(2026, 6, 30), assignment.EffectiveTo);
        Assert.Equal(new DateOnly(2026, 6, 30), projector.LastGrant!.EffectiveTo);
    }

    [Fact]
    public async Task GetAssignmentsForClassAsync_returns_only_that_class_and_years_assignments()
    {
        await using var teachingDb = CreateTeachingContext();
        await using var accessDb = CreateAccessContext();
        var service = new SubjectTeachingAssignmentService(teachingDb, accessDb, new FakeScopedAccessGrantProjector());
        var classId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        var assignmentId = await service.AssignAsync(Guid.NewGuid(), Guid.NewGuid(), classId, academicYearId, Guid.NewGuid(), new DateOnly(2026, 1, 1), null);
        await service.AssignAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), academicYearId, Guid.NewGuid(), new DateOnly(2026, 1, 1), null); // different class
        await service.AssignAsync(Guid.NewGuid(), Guid.NewGuid(), classId, Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), null); // different year

        var assignments = await service.GetAssignmentsForClassAsync(classId, academicYearId);

        Assert.Single(assignments, a => a.Id == assignmentId);
    }
}

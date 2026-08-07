using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class SubstitutionServiceTests
{
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
        db.Roles.Add(new Role { Id = Guid.NewGuid(), Code = RoleCodes.SubjectTeacher, Name = "Subject Teacher" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task CreateSubstitutionAsync_generates_a_single_day_substitute_assignment_linked_by_the_record()
    {
        await using var teachingDb = CreateTeachingContext();
        await using var accessDb = CreateAccessContext();
        var projector = new FakeScopedAccessGrantProjector();
        var assignmentService = new SubjectTeachingAssignmentService(teachingDb, accessDb, projector);
        var substitutionService = new SubstitutionService(teachingDb, assignmentService);

        var originalTeacherId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var originalAssignmentId = await assignmentService.AssignAsync(
            originalTeacherId, subjectId, classId, academicYearId, Guid.NewGuid(), new DateOnly(2026, 1, 1), null);

        var substituteId = Guid.NewGuid();
        var substitutionDate = new DateOnly(2026, 3, 10);

        var substitutionRecordId = await substitutionService.CreateSubstitutionAsync(
            originalAssignmentId, substituteId, substitutionDate, Guid.NewGuid(), "Original teacher on medical leave");

        var record = await teachingDb.SubstitutionRecords.SingleAsync(r => r.Id == substitutionRecordId);
        Assert.Equal(originalAssignmentId, record.OriginalAssignmentId);
        Assert.Equal(substituteId, record.SubstituteStaffPersonId);

        var generated = await teachingDb.SubjectTeachingAssignments.SingleAsync(a => a.Id == record.GeneratedAssignmentId);
        Assert.Equal(substituteId, generated.StaffPersonId);
        Assert.Equal(subjectId, generated.SubjectId);
        Assert.Equal(classId, generated.ClassId);

        // Single-day window — auto-expires the next day with no scheduled job.
        Assert.Equal(substitutionDate, generated.EffectiveFrom);
        Assert.Equal(substitutionDate, generated.EffectiveTo);

        var substituteRole = await teachingDb.AssignmentRoles.SingleAsync(r => r.Code == AssignmentRoleCodes.Substitute);
        Assert.Equal(substituteRole.Id, generated.AssignmentRoleId);

        // The substitute gets their own scoped grant — same subject+class, same Subject Teacher permission code.
        Assert.Equal(substituteId, projector.LastGrant!.PersonId);
        Assert.Equal(subjectId, projector.LastGrant.SubjectId);
        Assert.Equal(classId, projector.LastGrant.ClassId);
    }

    [Fact]
    public async Task CreateSubstitutionAsync_throws_when_the_original_assignment_does_not_exist()
    {
        await using var teachingDb = CreateTeachingContext();
        await using var accessDb = CreateAccessContext();
        var assignmentService = new SubjectTeachingAssignmentService(teachingDb, accessDb, new FakeScopedAccessGrantProjector());
        var substitutionService = new SubstitutionService(teachingDb, assignmentService);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            substitutionService.CreateSubstitutionAsync(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 10), null, null));
    }
}

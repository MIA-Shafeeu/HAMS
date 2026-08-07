using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class ClassTeacherAssignmentServiceTests
{
    private static TeachingTimetableDbContext CreateTeachingContext() => new(
        new DbContextOptionsBuilder<TeachingTimetableDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AccessDbContext CreateAccessContext()
    {
        var db = new AccessDbContext(new DbContextOptionsBuilder<AccessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.Add(new Role { Id = Guid.NewGuid(), Code = RoleCodes.ClassTeacher, Name = "Class Teacher" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task AssignAsync_is_retrievable_via_GetAssignmentsForClassAsync()
    {
        await using var teachingDb = CreateTeachingContext();
        await using var accessDb = CreateAccessContext();
        var service = new ClassTeacherAssignmentService(teachingDb, accessDb, new FakeScopedAccessGrantProjector());
        var classId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        var assignmentId = await service.AssignAsync(Guid.NewGuid(), classId, academicYearId, Guid.NewGuid(), new DateOnly(2026, 1, 1), null);

        var assignments = await service.GetAssignmentsForClassAsync(classId, academicYearId);
        Assert.Single(assignments, a => a.Id == assignmentId);
    }

    [Fact]
    public async Task GetAssignmentsForClassAsync_does_not_return_a_different_classs_assignment()
    {
        await using var teachingDb = CreateTeachingContext();
        await using var accessDb = CreateAccessContext();
        var service = new ClassTeacherAssignmentService(teachingDb, accessDb, new FakeScopedAccessGrantProjector());
        var academicYearId = Guid.NewGuid();
        await service.AssignAsync(Guid.NewGuid(), Guid.NewGuid(), academicYearId, Guid.NewGuid(), new DateOnly(2026, 1, 1), null);

        var assignments = await service.GetAssignmentsForClassAsync(Guid.NewGuid(), academicYearId);

        Assert.Empty(assignments);
    }
}

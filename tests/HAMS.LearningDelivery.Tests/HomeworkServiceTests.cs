using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class HomeworkServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static HomeworkService CreateService(LearningDeliveryDbContext db, DateOnly? today = null) =>
        new(db, new FakeClock(today ?? new DateOnly(2026, 8, 5)));

    [Fact]
    public async Task CreateAsync_creates_homework_for_a_class()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var classId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var id = await service.CreateAsync(
            classId, subjectId, null, "Fractions worksheet", "ބައި ސުވާލު", "Complete questions 1-10", "ސުވާލު 1-10 ފުރިހަމަ ކުރޭ",
            new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 10), 20, teacherId);

        var homework = await db.Homeworks.SingleAsync(h => h.Id == id);
        Assert.Equal(classId, homework.ClassId);
        Assert.Equal(20, homework.MaxScore);
        Assert.Equal(teacherId, homework.AssignedByPersonId);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_due_date_before_the_assigned_date()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, "Title", "Title", "Instructions", "Instructions",
            new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 5), null, Guid.NewGuid()));
    }

    [Fact]
    public async Task ListForClassAsync_returns_only_that_classs_homework_ordered_by_due_date_descending()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var classId = Guid.NewGuid();
        var otherClassId = Guid.NewGuid();

        var earlyId = await service.CreateAsync(classId, Guid.NewGuid(), null, "Early", "Early", "x", "x", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), null, Guid.NewGuid());
        var lateId = await service.CreateAsync(classId, Guid.NewGuid(), null, "Late", "Late", "x", "x", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 20), null, Guid.NewGuid());
        await service.CreateAsync(otherClassId, Guid.NewGuid(), null, "Other class", "Other class", "x", "x", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5), null, Guid.NewGuid());

        var result = await service.ListForClassAsync(classId);

        Assert.Equal([lateId, earlyId], result.Select(h => h.Id));
    }
}

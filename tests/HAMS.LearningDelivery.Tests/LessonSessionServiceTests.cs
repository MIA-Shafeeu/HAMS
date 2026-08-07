using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class LessonSessionServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Guid> SeedPlannedSessionAsync(LearningDeliveryDbContext db)
    {
        var lessonPlan = new LessonPlan
        {
            Id = Guid.NewGuid(), TeachingTopicId = Guid.NewGuid(), StaffPersonId = Guid.NewGuid(),
            PlannedDate = new DateOnly(2026, 1, 4), Objectives = "Introduce fractions",
        };
        db.LessonPlans.Add(lessonPlan);
        await db.SaveChangesAsync();

        var service = new LessonSessionService(db);
        var sessionId = await service.ScheduleAsync(lessonPlan.Id, Guid.NewGuid(), new DateOnly(2026, 1, 4), Guid.NewGuid());
        return sessionId;
    }

    [Fact]
    public async Task ScheduleAsync_creates_a_session_with_Planned_status()
    {
        await using var db = CreateContext();

        var sessionId = await SeedPlannedSessionAsync(db);

        var session = await db.LessonSessions.SingleAsync(s => s.Id == sessionId);
        Assert.Equal(LessonSessionStatus.Planned, session.Status);
    }

    [Fact]
    public async Task CompleteAsync_marks_the_session_Completed_and_records_covered_outcomes()
    {
        await using var db = CreateContext();
        var sessionId = await SeedPlannedSessionAsync(db);
        var service = new LessonSessionService(db);
        var outcomeIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await service.CompleteAsync(sessionId, outcomeIds);

        var session = await db.LessonSessions.SingleAsync(s => s.Id == sessionId);
        Assert.Equal(LessonSessionStatus.Completed, session.Status);
        var coveredIds = await db.LessonSessionOutcomeCoverages
            .Where(c => c.LessonSessionId == sessionId).Select(c => c.LearningOutcomeId).ToListAsync();
        Assert.Equal(outcomeIds.OrderBy(id => id), coveredIds.OrderBy(id => id));
    }

    [Fact]
    public async Task CompleteAsync_rejects_a_session_that_is_already_Completed()
    {
        await using var db = CreateContext();
        var sessionId = await SeedPlannedSessionAsync(db);
        var service = new LessonSessionService(db);
        await service.CompleteAsync(sessionId, [Guid.NewGuid()]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(sessionId, [Guid.NewGuid()]));
    }

    [Fact]
    public async Task CancelAsync_marks_a_Planned_session_Cancelled()
    {
        await using var db = CreateContext();
        var sessionId = await SeedPlannedSessionAsync(db);
        var service = new LessonSessionService(db);

        await service.CancelAsync(sessionId);

        var session = await db.LessonSessions.SingleAsync(s => s.Id == sessionId);
        Assert.Equal(LessonSessionStatus.Cancelled, session.Status);
    }

    [Fact]
    public async Task CancelAsync_rejects_a_session_that_is_already_Completed()
    {
        await using var db = CreateContext();
        var sessionId = await SeedPlannedSessionAsync(db);
        var service = new LessonSessionService(db);
        await service.CompleteAsync(sessionId, []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelAsync(sessionId));
    }
}

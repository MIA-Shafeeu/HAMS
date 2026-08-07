using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class LessonSessionService(LearningDeliveryDbContext dbContext) : ILessonSessionService
{
    public async Task<Guid> ScheduleAsync(Guid lessonPlanId, Guid classId, DateOnly actualDate, Guid periodId, CancellationToken cancellationToken = default)
    {
        var session = new LessonSession
        {
            Id = Guid.NewGuid(), LessonPlanId = lessonPlanId, ClassId = classId, ActualDate = actualDate, PeriodId = periodId,
        };
        dbContext.LessonSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return session.Id;
    }

    public async Task CompleteAsync(Guid lessonSessionId, IReadOnlyList<Guid> coveredOutcomeIds, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.LessonSessions.FindAsync([lessonSessionId], cancellationToken)
            ?? throw new InvalidOperationException("Lesson session not found.");

        if (session.Status != LessonSessionStatus.Planned)
        {
            throw new InvalidOperationException("Only a Planned session can be completed.");
        }

        session.Status = LessonSessionStatus.Completed;

        foreach (var outcomeId in coveredOutcomeIds)
        {
            dbContext.LessonSessionOutcomeCoverages.Add(new LessonSessionOutcomeCoverage
            {
                Id = Guid.NewGuid(), LessonSessionId = lessonSessionId, LearningOutcomeId = outcomeId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid lessonSessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.LessonSessions.FindAsync([lessonSessionId], cancellationToken)
            ?? throw new InvalidOperationException("Lesson session not found.");

        if (session.Status != LessonSessionStatus.Planned)
        {
            throw new InvalidOperationException("Only a Planned session can be cancelled.");
        }

        session.Status = LessonSessionStatus.Cancelled;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

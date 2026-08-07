using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class CoverageComparisonService(LearningDeliveryDbContext dbContext) : ICoverageComparisonService
{
    public async Task<CoverageComparisonResult> CompareAsync(Guid schemeOfWorkId, CancellationToken cancellationToken = default)
    {
        var plannedOutcomeIds = await dbContext.SchemeOfWorkItems
            .Where(i => i.SchemeOfWorkId == schemeOfWorkId)
            .Select(i => i.LearningOutcomeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Only outcomes actually delivered through *this* scheme of work's own
        // topic -> plan -> completed session -> coverage chain count as covered.
        var coveredOutcomeIds = await (
            from item in dbContext.SchemeOfWorkItems
            where item.SchemeOfWorkId == schemeOfWorkId
            join topic in dbContext.TeachingTopics on item.Id equals topic.SchemeOfWorkItemId
            join plan in dbContext.LessonPlans on topic.Id equals plan.TeachingTopicId
            join session in dbContext.LessonSessions.Where(s => s.Status == LessonSessionStatus.Completed)
                on plan.Id equals session.LessonPlanId
            join coverage in dbContext.LessonSessionOutcomeCoverages on session.Id equals coverage.LessonSessionId
            select coverage.LearningOutcomeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var coveredSet = coveredOutcomeIds.ToHashSet();
        var uncoveredOutcomeIds = plannedOutcomeIds.Where(id => !coveredSet.Contains(id)).ToList();

        return new CoverageComparisonResult(plannedOutcomeIds.Count, plannedOutcomeIds.Count - uncoveredOutcomeIds.Count, uncoveredOutcomeIds);
    }
}

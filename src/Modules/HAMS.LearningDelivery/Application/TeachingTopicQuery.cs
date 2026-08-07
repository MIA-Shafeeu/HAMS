using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class TeachingTopicQuery(LearningDeliveryDbContext dbContext) : ITeachingTopicQuery
{
    public async Task<Guid?> GetLearningOutcomeIdAsync(Guid teachingTopicId, CancellationToken cancellationToken = default)
        => await (
            from topic in dbContext.TeachingTopics
            where topic.Id == teachingTopicId
            join item in dbContext.SchemeOfWorkItems on topic.SchemeOfWorkItemId equals item.Id
            select (Guid?)item.LearningOutcomeId)
            .SingleOrDefaultAsync(cancellationToken);
}

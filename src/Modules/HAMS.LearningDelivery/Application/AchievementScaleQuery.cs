using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class AchievementScaleQuery(LearningDeliveryDbContext dbContext) : IAchievementScaleQuery
{
    public async Task<IReadOnlyDictionary<Guid, int>> GetLevelRanksAsync(Guid achievementScaleId, CancellationToken cancellationToken = default)
        => await dbContext.AchievementLevels
            .Where(l => l.AchievementScaleId == achievementScaleId && l.IsActive)
            .ToDictionaryAsync(l => l.Id, l => l.Rank, cancellationToken);
}

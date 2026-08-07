using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class RecommendedLevelEngine(LearningDeliveryDbContext dbContext) : IRecommendedLevelEngine
{
    public async Task<RecommendedLevelResult> RecommendAsync(
        Guid studentPersonId, Guid learningOutcomeId, Guid achievementScaleId, CancellationToken cancellationToken = default)
    {
        var scale = await dbContext.AchievementScales.FindAsync([achievementScaleId], cancellationToken)
            ?? throw new InvalidOperationException("Achievement scale not found.");

        var evidenceLevelIds = await dbContext.LearningEvidences
            .Where(e => e.StudentPersonId == studentPersonId && e.LearningOutcomeId == learningOutcomeId)
            .Select(e => e.AchievementLevelId)
            .ToListAsync(cancellationToken);

        if (evidenceLevelIds.Count < scale.MinimumEvidenceCount)
        {
            return new RecommendedLevelResult(false, null, evidenceLevelIds.Count);
        }

        var levelRanks = await dbContext.AchievementLevels
            .Where(l => l.AchievementScaleId == achievementScaleId)
            .ToDictionaryAsync(l => l.Id, l => l.Rank, cancellationToken);

        // Mode across all recorded evidence (consistency, not just the latest data point) — ties
        // broken toward the lower-ranked level as the conservative default.
        var recommendedLevelId = evidenceLevelIds
            .GroupBy(levelId => levelId)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => levelRanks.GetValueOrDefault(group.Key))
            .First()
            .Key;

        return new RecommendedLevelResult(true, recommendedLevelId, evidenceLevelIds.Count);
    }
}

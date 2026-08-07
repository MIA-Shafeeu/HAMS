using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class MasteryEvaluationService(
    LearningDeliveryDbContext dbContext, IRecommendedLevelEngine recommendedLevelEngine, IClock clock) : IMasteryEvaluationService
{
    public async Task<Guid> RecordEvaluationAsync(
        Guid studentPersonId, Guid learningOutcomeId, Guid keyStagePolicyId, Guid achievementScaleId,
        Guid recordedByPersonId, Guid? manualAchievementLevelId, CancellationToken cancellationToken = default)
    {
        Guid achievementLevelId;
        int evidenceCount;
        bool wasManuallyOverridden;

        if (manualAchievementLevelId is { } overrideLevelId)
        {
            achievementLevelId = overrideLevelId;
            evidenceCount = await dbContext.LearningEvidences.CountAsync(
                e => e.StudentPersonId == studentPersonId && e.LearningOutcomeId == learningOutcomeId, cancellationToken);
            wasManuallyOverridden = true;
        }
        else
        {
            var recommendation = await recommendedLevelEngine.RecommendAsync(
                studentPersonId, learningOutcomeId, achievementScaleId, cancellationToken);

            if (!recommendation.IsSufficient)
            {
                throw new InvalidOperationException(
                    $"Insufficient evidence to recommend a mastery level ({recommendation.EvidenceCount} recorded so far).");
            }

            achievementLevelId = recommendation.RecommendedAchievementLevelId!.Value;
            evidenceCount = recommendation.EvidenceCount;
            wasManuallyOverridden = false;
        }

        var evaluation = new MasteryEvaluation
        {
            Id = Guid.NewGuid(),
            StudentPersonId = studentPersonId,
            LearningOutcomeId = learningOutcomeId,
            KeyStagePolicyId = keyStagePolicyId,
            AchievementScaleId = achievementScaleId,
            AchievementLevelId = achievementLevelId,
            WasManuallyOverridden = wasManuallyOverridden,
            EvidenceCountAtEvaluation = evidenceCount,
            RecordedByPersonId = recordedByPersonId,
            RecordedAtUtc = clock.UtcNow,
        };
        dbContext.MasteryEvaluations.Add(evaluation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return evaluation.Id;
    }

    public async Task<MasteryEvaluation?> GetCurrentAsync(
        Guid studentPersonId, Guid learningOutcomeId, CancellationToken cancellationToken = default)
        => await dbContext.MasteryEvaluations
            .Where(e => e.StudentPersonId == studentPersonId && e.LearningOutcomeId == learningOutcomeId)
            .OrderByDescending(e => e.RecordedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, MasteryEvaluation>> GetCurrentForOutcomesAsync(
        Guid studentPersonId, IReadOnlyList<Guid> learningOutcomeIds, CancellationToken cancellationToken = default)
    {
        var evaluations = await dbContext.MasteryEvaluations
            .Where(e => e.StudentPersonId == studentPersonId && learningOutcomeIds.Contains(e.LearningOutcomeId))
            .ToListAsync(cancellationToken);

        return evaluations
            .GroupBy(e => e.LearningOutcomeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.RecordedAtUtc).First());
    }
}

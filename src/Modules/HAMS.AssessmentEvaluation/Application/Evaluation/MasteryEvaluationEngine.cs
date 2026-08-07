using HAMS.AssessmentEvaluation.Domain;
using HAMS.LearningDelivery.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;

namespace HAMS.AssessmentEvaluation.Application.Evaluation;

/// <summary>
/// The Mastery model: aggregates every <c>LearningOutcome</c> in the subject's current syllabus
/// into one overall achievement level — the mode (most frequently demonstrated level) across
/// outcomes evaluated within the period, ties broken toward the lower-ranked level, the same
/// conservative rule <c>IRecommendedLevelEngine</c> (LearningDelivery, Phase 6) already applies
/// one level down (evidence -&gt; outcome level). Returns <see cref="EvaluationOutcome.Empty"/>
/// (not an exception) when there's no published syllabus or no outcomes evaluated yet within the
/// period — an evaluation genuinely can't be produced yet, which is a legitimate, non-exceptional
/// state, not a configuration error.
/// </summary>
internal sealed class MasteryEvaluationEngine(
    ISyllabusResolver syllabusResolver, IMasteryEvaluationService masteryEvaluationService, IAchievementScaleQuery achievementScaleQuery)
    : IEvaluationEngine
{
    public string ModelCode => EvaluationModelCodes.Mastery;

    public async Task<EvaluationOutcome> EvaluateAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (context.Policy.AchievementScaleId is not { } achievementScaleId)
        {
            throw new InvalidOperationException("This key-stage policy uses the Mastery evaluation model but has no AchievementScaleId configured.");
        }

        var syllabus = await syllabusResolver.ResolveAsync(context.SubjectId, context.GradeId, cancellationToken);
        if (syllabus is null)
        {
            return EvaluationOutcome.Empty;
        }

        var outcomeIds = await syllabusResolver.GetLearningOutcomeIdsAsync(syllabus.Id, cancellationToken);
        if (outcomeIds.Count == 0)
        {
            return EvaluationOutcome.Empty;
        }

        var currentEvaluations = await masteryEvaluationService.GetCurrentForOutcomesAsync(context.StudentPersonId, outcomeIds, cancellationToken);

        var levelIdsInPeriod = currentEvaluations.Values
            .Where(e => IsWithinPeriod(e.RecordedAtUtc, context.Period))
            .Select(e => e.AchievementLevelId)
            .ToList();

        if (levelIdsInPeriod.Count == 0)
        {
            return EvaluationOutcome.Empty;
        }

        var levelRanks = await achievementScaleQuery.GetLevelRanksAsync(achievementScaleId, cancellationToken);

        var overallLevelId = levelIdsInPeriod
            .GroupBy(levelId => levelId)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => levelRanks.GetValueOrDefault(group.Key))
            .First()
            .Key;

        return new EvaluationOutcome(overallLevelId, null, null);
    }

    private static bool IsWithinPeriod(DateTimeOffset recordedAtUtc, EvaluationPeriod period)
    {
        var recordedDate = DateOnly.FromDateTime(recordedAtUtc.UtcDateTime);
        return recordedDate >= period.StartDate && recordedDate <= period.EndDate;
    }
}

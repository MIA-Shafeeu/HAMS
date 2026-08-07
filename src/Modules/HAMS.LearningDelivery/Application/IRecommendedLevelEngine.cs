namespace HAMS.LearningDelivery.Application;

public sealed record RecommendedLevelResult(bool IsSufficient, Guid? RecommendedAchievementLevelId, int EvidenceCount);

/// <summary>
/// The "sufficiency rule" + "recommended-level engine" the build plan's Phase 6 scope calls for.
/// Deliberately narrow: given evidence already recorded and an achievement scale's configured
/// rule, decide whether there's enough to recommend a level, and if so, which — it does not
/// resolve which scale applies to a student (that's the caller's job, typically via
/// OrgCurriculum's <c>IKeyStagePolicyResolver</c> one level up), keeping this engine's own
/// responsibility focused and independently testable.
/// </summary>
public interface IRecommendedLevelEngine
{
    /// <summary>
    /// Recommends an achievement level from all <c>LearningEvidence</c> recorded for
    /// <paramref name="studentPersonId"/>/<paramref name="learningOutcomeId"/>: the most
    /// frequently demonstrated level (consistency over a single most-recent data point), ties
    /// broken toward the lower-ranked level as the conservative default. Returns
    /// <c>IsSufficient = false</c> if fewer than the scale's configured
    /// <c>MinimumEvidenceCount</c> pieces of evidence exist yet.
    /// </summary>
    Task<RecommendedLevelResult> RecommendAsync(
        Guid studentPersonId, Guid learningOutcomeId, Guid achievementScaleId, CancellationToken cancellationToken = default);
}

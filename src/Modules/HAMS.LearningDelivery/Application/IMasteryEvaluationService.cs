using HAMS.LearningDelivery.Domain;

namespace HAMS.LearningDelivery.Application;

/// <summary>
/// Records mastery-history entries (build plan §3: append-only, stores <c>KeyStagePolicyId</c>).
/// <paramref name="keyStagePolicyId"/>/<paramref name="achievementScaleId"/> in
/// <see cref="RecordEvaluationAsync"/> are supplied by the caller (typically already resolved via
/// OrgCurriculum's <c>IKeyStagePolicyResolver</c> one level up) rather than re-resolved here —
/// this service only needs to know what to stamp on the row and which scale's rule to apply, not
/// how a grade maps to a policy.
/// </summary>
public interface IMasteryEvaluationService
{
    /// <summary>
    /// Records a new evaluation. If <paramref name="manualAchievementLevelId"/> is supplied, it is
    /// used as-is (a teacher's professional judgement can assert a level even against
    /// insufficient evidence — the record is simply flagged <c>WasManuallyOverridden</c>);
    /// otherwise <see cref="IRecommendedLevelEngine"/> decides, and throws
    /// <see cref="InvalidOperationException"/> if evidence is insufficient.
    /// </summary>
    Task<Guid> RecordEvaluationAsync(
        Guid studentPersonId, Guid learningOutcomeId, Guid keyStagePolicyId, Guid achievementScaleId,
        Guid recordedByPersonId, Guid? manualAchievementLevelId, CancellationToken cancellationToken = default);

    /// <summary>The most recently recorded evaluation for this student+outcome, or null if none exists yet.</summary>
    Task<MasteryEvaluation?> GetCurrentAsync(Guid studentPersonId, Guid learningOutcomeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recently recorded evaluation per outcome, for every outcome in
    /// <paramref name="learningOutcomeIds"/> that has at least one — the batch read Phase 8's
    /// Mastery evaluation engine (AssessmentEvaluation) needs to aggregate a whole subject's
    /// outcomes in one call rather than one <see cref="GetCurrentAsync"/> round-trip per outcome.
    /// An outcome with no evaluation yet is simply absent from the result, not a null entry.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, MasteryEvaluation>> GetCurrentForOutcomesAsync(
        Guid studentPersonId, IReadOnlyList<Guid> learningOutcomeIds, CancellationToken cancellationToken = default);
}

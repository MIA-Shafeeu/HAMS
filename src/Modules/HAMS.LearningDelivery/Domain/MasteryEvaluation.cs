namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// One append-only mastery-history entry (build plan §3: "MasteryEvaluation (append-only, stores
/// KeyStagePolicyId)") — the recorded outcome of either <c>IRecommendedLevelEngine</c>'s
/// evidence-based recommendation or a teacher's manual override. Never updated or deleted; the
/// "current" evaluation for a student+outcome is simply the most recently recorded row (ordered by
/// <see cref="RecordedAtUtc"/>) — unlike <c>KeyStagePolicy</c>/<c>Syllabus</c>, there is no
/// Draft/Published lineage to manage here, since each row is an independent, final snapshot of a
/// judgement made at a point in time, not a config value that gets revised in place.
/// <see cref="KeyStagePolicyId"/> is stamped at evaluation time so a later change to the school's
/// evaluation configuration never silently rewrites what this evaluation meant when it was made
/// (build plan §3's evaluation-model cascade rule).
/// </summary>
public sealed class MasteryEvaluation
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid LearningOutcomeId { get; init; }

    public Guid KeyStagePolicyId { get; init; }

    public Guid AchievementScaleId { get; init; }

    public Guid AchievementLevelId { get; init; }

    /// <summary>True when a teacher's professional judgement overrode <c>IRecommendedLevelEngine</c>'s recommendation (or was recorded despite insufficient evidence) — false when the recommendation was accepted as-is.</summary>
    public bool WasManuallyOverridden { get; init; }

    /// <summary>How many <see cref="LearningEvidence"/> rows existed for this student+outcome at the moment of evaluation — a historical fact, not a live count, so it survives new evidence being added later.</summary>
    public int EvidenceCountAtEvaluation { get; init; }

    public Guid RecordedByPersonId { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; }
}

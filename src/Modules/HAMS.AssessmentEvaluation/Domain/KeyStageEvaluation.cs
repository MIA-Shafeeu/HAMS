namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// The "official" per-student-per-subject-per-period evaluation (build plan §3/Phase 8: "policy
/// versioning on every result" — <see cref="KeyStagePolicyId"/> is stamped so a later change to
/// the school's evaluation configuration never silently rewrites what this evaluation meant when
/// it was produced, the same discipline as <c>MasteryEvaluation</c>/<c>AssessmentResult</c>).
/// Append-only, like <c>MasteryEvaluation</c> — no <c>IVersionedRecord</c> lineage: this row is a
/// computed snapshot produced by <c>IKeyStageEvaluationService</c> reading whatever
/// <c>MasteryEvaluation</c>/<c>AssessmentResult</c> rows exist at the moment it runs, not something
/// a person edits directly. If the inputs change, re-run the evaluation and get a new row; "current"
/// is simply the most recent by <see cref="RecordedAtUtc"/>.
///
/// Exactly which of <see cref="OverallAchievementLevelId"/> / (<see cref="OverallPercentage"/>,
/// <see cref="OverallGradeBandId"/>) are populated depends on <see cref="EvaluationModelId"/>'s
/// resolved code: Mastery populates only the level, Assessment populates only the percentage/band,
/// Hybrid populates all three — the Ministry's real Key Stage 3 policy tracks and reports both
/// facets side by side for a Hybrid subject, it does not mathematically blend them into one number
/// (there is no described formula for that, so this deliberately does not invent one).
/// </summary>
public sealed class KeyStageEvaluation
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid SubjectId { get; init; }

    public Guid EvaluationPeriodId { get; init; }

    public Guid KeyStagePolicyId { get; init; }

    public Guid EvaluationModelId { get; init; }

    public Guid? OverallAchievementLevelId { get; init; }

    public decimal? OverallPercentage { get; init; }

    public Guid? OverallGradeBandId { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; }
}

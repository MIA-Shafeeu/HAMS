using HAMS.Platform.Common.Contracts;

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// The single most important config row in the system (build plan §3): configured exactly once
/// per <c>(KeyStageId, AcademicYearId)</c>, selecting the evaluation model (Mastery/Assessment/
/// Hybrid) and the achievement scale/assessment scheme/grade scale/promotion policy that go with
/// it. Every grade assigned to a key stage (via <see cref="GradeKeyStageAssignment"/>) inherits
/// this automatically — a grade never stores its own evaluation configuration.
///
/// Append-only and versioned like every judgement/config row that must never silently rewrite
/// history (§3's versioning strategy): changing this only ever affects future evaluations, since
/// every historical evaluation/result row captures the exact <see cref="Id"/> that was active
/// when it was produced, never a live lookup.
/// </summary>
/// <remarks>
/// <see cref="AchievementScaleId"/> (LearningDelivery, wired Phase 6) and
/// <see cref="AssessmentSchemeId"/>/<see cref="GradeScaleId"/> (AssessmentEvaluation, wired
/// Phase 7) are now genuine loose forward references, settable via <c>CreateKeyStagePolicyRequest</c>
/// — none of the three are validated at the DB level (no FK constraint), matching every other
/// cross-module reference in this codebase. <see cref="PromotionPolicyId"/> remains unresolved
/// until Phase 11. This row's own scope (evaluation model selection + versioning discipline) has
/// been fully functional since Phase 1 regardless of which of these four are populated.
/// </remarks>
public sealed class KeyStagePolicy : IVersionedRecord<Guid>
{
    public Guid Id { get; init; }

    public Guid KeyStageId { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid EvaluationModelId { get; set; }

    public Guid? AchievementScaleId { get; set; }
    public Guid? AssessmentSchemeId { get; set; }
    public Guid? GradeScaleId { get; set; }
    public Guid? PromotionPolicyId { get; set; }

    public int Version { get; init; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesId { get; init; }
    public Guid? SupersededById { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Draft;

    public bool IsImmutable => Status is RecordStatus.Published or RecordStatus.Locked;
}

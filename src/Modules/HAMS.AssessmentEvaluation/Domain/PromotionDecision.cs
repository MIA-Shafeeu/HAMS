namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// The human-made, recorded outcome of a promotion review (build plan Phase 11) — always a real
/// person's decision, never automatically derived from <c>IPromotionService.EvaluateEligibilityAsync</c>'s
/// recommendation (a student can be promoted despite unmet subjects for a valid pastoral reason, or
/// held back despite clearing every bar). Append-only like <c>KeyStageEvaluation</c>/
/// <c>MasteryEvaluation</c> — deliberately NOT <see cref="Platform.Common.Contracts.IVersionedRecord{TKey}"/>
/// despite that interface's own doc-comment listing promotion decisions as an anticipated consumer:
/// a decision here is one discrete, dated administrative act with no Draft/review lifecycle of its
/// own (unlike <c>ReportCard</c>, which genuinely needs multi-stage sign-off) — correcting one means
/// recording a new decision, "current" is simply the latest by <see cref="RecordedAtUtc"/>, the same
/// reasoning Phase 6 applied to evidence rows.
/// </summary>
public sealed class PromotionDecision
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid CurrentGradeId { get; init; }

    public bool Promoted { get; init; }

    /// <summary>The intended next grade — informational at decision time; actually re-enrolling the student is a separate, later <c>IStudentEnrollmentService.EnrollAsync</c> call, not performed here.</summary>
    public Guid? NextGradeId { get; init; }

    public Guid DecidedByPersonId { get; init; }

    public DateOnly DecisionDate { get; init; }

    public string? Notes { get; set; }

    public DateTimeOffset RecordedAtUtc { get; init; }
}

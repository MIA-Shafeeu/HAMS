namespace HAMS.Intervention.Domain;

/// <summary>
/// One reassessment attempt against an <see cref="InterventionCase"/> (build plan Phase 9 scope:
/// "reassessment rules") — links a fresh <c>KeyStageEvaluation</c> (AssessmentEvaluation, re-run
/// via the existing Phase 8 engine after the intervention plan's support period) back to the case
/// it's reassessing. Append-only, a simple audit trail.
///
/// <b>Deliberate scope-down, flagged rather than silently done</b>: this does NOT automatically
/// decide pass/fail by comparing the new evaluation's rank against the triggering one — that would
/// require a "minimum passing rank" configuration concept that doesn't exist on
/// <c>AchievementScale</c>/<c>GradeScale</c> today, and inventing one here would be speculative
/// configuration surface nobody asked for. A human (the staff member reviewing the reassessment)
/// decides whether to call <c>IInterventionCaseService.CloseCaseAsync</c> — this record just
/// supplies the evidence they base that judgement on.
/// </summary>
public sealed class ReassessmentAttempt
{
    public Guid Id { get; init; }

    public Guid InterventionCaseId { get; init; }

    public Guid KeyStageEvaluationId { get; init; }

    public Guid RecordedByPersonId { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; }
}

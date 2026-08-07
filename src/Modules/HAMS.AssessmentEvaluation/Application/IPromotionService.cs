using HAMS.AssessmentEvaluation.Domain;
using HAMS.PeopleEnrollment.Application;

namespace HAMS.AssessmentEvaluation.Application;

/// <summary>
/// A read-only recommendation, never a decision — <see cref="MeetsThreshold"/> is what
/// <see cref="PromotionPolicy"/> computes from the student's own evaluations; the actual outcome
/// only ever comes from a human calling <see cref="IPromotionService.RecordDecisionAsync"/>, which
/// can agree or disagree with this recommendation for a reason the policy can't see.
/// </summary>
public sealed record PromotionEligibilityResult(
    bool MeetsThreshold, int SubjectsCleared, int TotalSubjectsEvaluated, IReadOnlyList<Guid> SubjectIdsNotCleared);

/// <summary>
/// Promotion/Progression (build plan §2/Phase 11) — resolves <c>KeyStagePolicy.PromotionPolicyId</c>
/// (the last of that entity's four reserved forward references, unpopulated since Phase 1) the same
/// way Phases 6-8 wired up <c>AchievementScaleId</c>/<c>AssessmentSchemeId</c>/<c>GradeScaleId</c> in turn.
/// </summary>
public interface IPromotionService
{
    /// <exception cref="InvalidOperationException">
    /// The student has no active enrolment for that academic year as of that date, no published
    /// <c>KeyStagePolicy</c> exists for their grade/year, or that policy has no
    /// <see cref="PromotionPolicy"/> configured.
    /// </exception>
    Task<PromotionEligibilityResult> EvaluateEligibilityAsync(
        Guid studentPersonId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the human decision and ends the student's current-year enrolment (build plan §3's
    /// "never delete, close+reopen" rule) — re-enrolling into <paramref name="nextGradeId"/> for the
    /// next academic year is a deliberately separate, later <c>IStudentEnrollmentService.EnrollAsync</c>
    /// call, not performed here.
    /// </summary>
    /// <exception cref="InvalidOperationException">The student has no active enrolment for that academic year as of the decision date.</exception>
    Task<Guid> RecordDecisionAsync(
        Guid studentPersonId, Guid academicYearId, bool promoted, Guid? nextGradeId, Guid decidedByPersonId, DateOnly decisionDate,
        string? notes, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionDecision>> GetDecisionsForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every student in this grade/year with an active enrolment but no <see cref="PromotionDecision"/>
    /// recorded yet for that academic year — a promotion-decisions authoring UI's worklist (the first
    /// "who still needs a decision" read anywhere in this codebase; every prior caller already knew
    /// the specific student it wanted). Cross-references <see cref="IStudentEnrollmentService.GetActiveRosterForGradeAsync"/>
    /// against recorded decisions rather than duplicating roster-resolution logic.
    /// </summary>
    Task<IReadOnlyList<ClassRosterEntry>> GetStudentsNeedingDecisionAsync(
        Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default);
}

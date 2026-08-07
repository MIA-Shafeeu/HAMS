using HAMS.AssessmentEvaluation.Domain;

namespace HAMS.AssessmentEvaluation.Application.Evaluation;

/// <summary>
/// The evaluation-engine dispatcher (build plan §13/Phase 8 — "the single riskiest component in
/// the system"): resolves the student's own enrolment-derived grade, that grade's published
/// <c>KeyStagePolicy</c>, and dispatches to whichever <see cref="IEvaluationEngine"/> matches the
/// policy's configured evaluation model. This is the one place <c>StudentEnrollment.GradeId</c> is
/// resolved and handed down — no other code in this evaluation chain touches enrolment or grade
/// resolution, so the combined-class rule (build plan §12) is enforced in exactly one spot.
/// </summary>
public interface IKeyStageEvaluationService
{
    /// <exception cref="InvalidOperationException">
    /// The student has no active enrolment for that academic year as of <paramref name="asOf"/>,
    /// no published <c>KeyStagePolicy</c> exists for their grade/year, or the resolved evaluation
    /// model has no matching engine or missing required scale/scheme configuration.
    /// </exception>
    Task<Guid> EvaluateAsync(
        Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf,
        CancellationToken cancellationToken = default);

    /// <summary>The most recently recorded evaluation for this student+subject+period, or null if none exists yet.</summary>
    Task<KeyStageEvaluation?> GetCurrentAsync(
        Guid studentPersonId, Guid subjectId, Guid evaluationPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every subject+period this student has a current evaluation for (Phase 10's portal "my
    /// results" view — the portal doesn't know which subjects/periods to ask
    /// <see cref="GetCurrentAsync"/> about ahead of time). Portal-safe by construction, not by an
    /// added filter: a <c>KeyStageEvaluation</c> row only ever exists once a school has actually
    /// triggered <see cref="EvaluateAsync"/>, Mastery-model evaluations have no draft state at all,
    /// and the Assessment/Hybrid engines already exclude non-Published <c>AssessmentResult</c> rows
    /// from their aggregation (Phase 8) — so nothing in-progress can ever surface here.
    /// </summary>
    Task<IReadOnlyList<KeyStageEvaluation>> GetAllCurrentForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default);
}

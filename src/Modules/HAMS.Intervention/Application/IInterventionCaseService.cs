using HAMS.Intervention.Domain;

namespace HAMS.Intervention.Application;

public sealed record InterventionTypeOption(Guid Id, string Code, string Name);

/// <summary>
/// The Phase 9 intervention-case lifecycle (build plan §2/Phase 9: "Intervention/Remediation/
/// Reassessment"): a case is opened against a student+subject (usually because a
/// <c>KeyStageEvaluation</c> showed a gap), a support plan is recorded, zero or more reassessment
/// attempts are made by re-running <c>IKeyStageEvaluationService</c>, and a human decides when to
/// close it — there is deliberately no automated pass/fail comparison between a reassessment's
/// evaluation and the original one (build plan §12's spirit: don't invent unrequested judgment
/// logic), a case worker reviews the fresh evaluation and calls <see cref="CloseCaseAsync"/> themselves.
/// </summary>
public interface IInterventionCaseService
{
    Task<Guid> OpenCaseAsync(
        Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid interventionTypeId, string confidentialityTierCode,
        Guid? learningOutcomeId, Guid? triggeringKeyStageEvaluationId, Guid? carriedForwardGapId,
        Guid openedByPersonId, DateOnly openedDate, CancellationToken cancellationToken = default);

    Task<Guid> CreatePlanAsync(
        Guid interventionCaseId, string description, Guid assignedStaffPersonId, DateOnly startDate, DateOnly targetDate,
        string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-runs <c>IKeyStageEvaluationService.EvaluateAsync</c> for the case's own student/subject and
    /// links the resulting evaluation — this is the mechanism, not a verdict; nothing here decides
    /// whether the reassessment "passed."
    /// </summary>
    Task<Guid> RecordReassessmentAttemptAsync(
        Guid interventionCaseId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, Guid recordedByPersonId,
        CancellationToken cancellationToken = default);

    Task CloseCaseAsync(Guid interventionCaseId, DateOnly closedDate, CancellationToken cancellationToken = default);

    Task<InterventionCase?> GetAsync(Guid interventionCaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every case for this student, most recent first (Phase 10's guardian portal "intervention
    /// updates" surface). Deliberately returns the full <see cref="InterventionCase"/> row — the
    /// portal layer (CommunicationPortals), not this service, decides how much of it is safe to
    /// show a guardian; nothing here filters by confidentiality tier, since that kernel governs
    /// staff access, not a guardian's own relationship-based visibility.
    /// </summary>
    Task<IReadOnlyList<InterventionCase>> GetCasesForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterventionPlan>> GetPlansAsync(Guid interventionCaseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReassessmentAttempt>> GetReassessmentAttemptsAsync(Guid interventionCaseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterventionTypeOption>> GetActiveInterventionTypesAsync(CancellationToken cancellationToken = default);
}

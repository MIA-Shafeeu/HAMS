using HAMS.AssessmentEvaluation.Domain;
using HAMS.OrgCurriculum.Domain;

namespace HAMS.AssessmentEvaluation.Application.Evaluation;

/// <summary>
/// Everything one evaluation-model implementation needs, resolved once by
/// <see cref="IKeyStageEvaluationService"/> and handed down rather than re-fetched per engine —
/// <see cref="GradeId"/> is the student's own <c>StudentEnrollment.GradeId</c>, resolved by the
/// dispatcher, never a <c>Class</c>'s grade (build plan §3/§12's combined-class rule).
/// </summary>
public sealed record EvaluationContext(
    Guid StudentPersonId, Guid SubjectId, Guid GradeId, Guid AcademicYearId, KeyStagePolicy Policy, EvaluationPeriod Period);

/// <summary>
/// One evaluation-model's result. Exactly which fields are populated depends on which
/// <see cref="IEvaluationEngine"/> produced it — see <see cref="KeyStageEvaluation"/>'s remarks.
/// </summary>
public sealed record EvaluationOutcome(Guid? AchievementLevelId, decimal? OverallPercentage, Guid? GradeBandId)
{
    public static readonly EvaluationOutcome Empty = new(null, null, null);
}

/// <summary>
/// One per-key-stage evaluation-model strategy (build plan §13: "IEvaluationEngine.cs +
/// MasteryEvaluationEngine.cs/AssessmentEvaluationEngine.cs/HybridEvaluationEngine.cs") —
/// <see cref="IKeyStageEvaluationService"/> dispatches to whichever implementation's
/// <see cref="ModelCode"/> matches the resolved <c>KeyStagePolicy</c>'s <c>EvaluationModel.Code</c>.
/// </summary>
public interface IEvaluationEngine
{
    /// <summary>One of <c>EvaluationModelCodes.Mastery</c>/<c>Assessment</c>/<c>Hybrid</c>.</summary>
    string ModelCode { get; }

    Task<EvaluationOutcome> EvaluateAsync(EvaluationContext context, CancellationToken cancellationToken = default);
}

using HAMS.OrgCurriculum.Domain;

namespace HAMS.OrgCurriculum.Application;

/// <summary>
/// Resolves an <see cref="EvaluationModel"/> row by id — the small public read surface Phase 8's
/// evaluation-engine dispatcher (AssessmentEvaluation) needs to turn the
/// <c>KeyStagePolicy.EvaluationModelId</c> it already has into the <c>Code</c> string
/// (Mastery/Assessment/Hybrid) it dispatches on, without reaching into OrgCurriculum's internals.
/// </summary>
public interface IEvaluationModelLookup
{
    Task<EvaluationModel?> GetByIdAsync(Guid evaluationModelId, CancellationToken cancellationToken = default);
}

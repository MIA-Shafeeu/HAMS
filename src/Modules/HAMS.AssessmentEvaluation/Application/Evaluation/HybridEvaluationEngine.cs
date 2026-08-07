using HAMS.OrgCurriculum.Domain;

namespace HAMS.AssessmentEvaluation.Application.Evaluation;

/// <summary>
/// The Hybrid model: the real Key Stage 3 policy blends continuous assessment for every subject
/// with an internal term exam for six named subjects, but the Ministry's own reporting tracks and
/// shows both facets side by side — it never describes a formula for blending an achievement level
/// with an exam percentage into one number. This engine deliberately does the same: it runs
/// <see cref="MasteryEvaluationEngine"/> and <see cref="AssessmentEvaluationEngine"/> independently
/// (both require their respective scale/scheme to be configured on the policy) and merges their
/// non-overlapping fields into one <see cref="EvaluationOutcome"/>, rather than inventing a
/// blending formula the source policy doesn't specify.
/// </summary>
internal sealed class HybridEvaluationEngine(MasteryEvaluationEngine masteryEngine, AssessmentEvaluationEngine assessmentEngine) : IEvaluationEngine
{
    public string ModelCode => EvaluationModelCodes.Hybrid;

    public async Task<EvaluationOutcome> EvaluateAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        var masteryOutcome = await masteryEngine.EvaluateAsync(context, cancellationToken);
        var assessmentOutcome = await assessmentEngine.EvaluateAsync(context, cancellationToken);

        return new EvaluationOutcome(masteryOutcome.AchievementLevelId, assessmentOutcome.OverallPercentage, assessmentOutcome.GradeBandId);
    }
}

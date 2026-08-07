using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.OrgCurriculum.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Application.Evaluation;

/// <summary>
/// The Assessment model: for each of the <c>AssessmentScheme</c>'s weighted components (e.g. "Term
/// Exam 60%"), combines every <c>Assessment</c> in that category scheduled within the evaluation
/// period into one component percentage (via the component's configured
/// <see cref="ResultAggregationRule"/>), then weight-sums the components into an overall
/// percentage and maps it to a <see cref="GradeBand"/>. Only <c>Approved</c> (Published)
/// <c>AssessmentResult</c>s with a <c>FinalMark</c> contribute — a Medical-Certificate-excused
/// result (no <c>FinalMark</c>) is simply skipped, not treated as a zero (build plan §3: an
/// excused absence isn't a failed attempt). Components with zero contributing assessments are
/// excluded from both the weighted numerator and the weight denominator, so a subject missing one
/// category's results still produces a sensible partial evaluation rather than an artificially low
/// one from a phantom "0% for the missing category."
/// </summary>
internal sealed class AssessmentEvaluationEngine(AssessmentEvaluationDbContext dbContext) : IEvaluationEngine
{
    public string ModelCode => EvaluationModelCodes.Assessment;

    public async Task<EvaluationOutcome> EvaluateAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (context.Policy.AssessmentSchemeId is not { } schemeId || context.Policy.GradeScaleId is not { } gradeScaleId)
        {
            throw new InvalidOperationException("This key-stage policy uses the Assessment evaluation model but has no AssessmentScheme/GradeScale configured.");
        }

        var components = await dbContext.AssessmentSchemeComponents
            .Where(c => c.AssessmentSchemeId == schemeId)
            .ToListAsync(cancellationToken);
        if (components.Count == 0)
        {
            return EvaluationOutcome.Empty;
        }

        decimal weightedSum = 0;
        decimal totalWeight = 0;

        foreach (var component in components)
        {
            var componentPercentage = await ComputeComponentPercentageAsync(context, component, cancellationToken);
            if (componentPercentage is not { } percentage)
            {
                continue;
            }

            weightedSum += percentage * component.WeightPercentage;
            totalWeight += component.WeightPercentage;
        }

        if (totalWeight == 0)
        {
            return EvaluationOutcome.Empty;
        }

        var overallPercentage = weightedSum / totalWeight;

        var gradeBand = await dbContext.GradeBands
            .Where(b => b.GradeScaleId == gradeScaleId && b.IsActive)
            .Where(b => overallPercentage >= b.MinPercentage && overallPercentage <= b.MaxPercentage)
            .SingleOrDefaultAsync(cancellationToken);

        return new EvaluationOutcome(null, overallPercentage, gradeBand?.Id);
    }

    private async Task<decimal?> ComputeComponentPercentageAsync(
        EvaluationContext context, AssessmentSchemeComponent component, CancellationToken cancellationToken)
    {
        var assessmentsInCategory = await dbContext.Assessments
            .Where(a => a.SubjectId == context.SubjectId && a.GradeId == context.GradeId && a.AcademicYearId == context.AcademicYearId)
            .Where(a => a.AssessmentCategoryId == component.AssessmentCategoryId)
            .Where(a => a.ScheduledDate >= context.Period.StartDate && a.ScheduledDate <= context.Period.EndDate)
            .ToListAsync(cancellationToken);

        if (assessmentsInCategory.Count == 0)
        {
            return null;
        }

        var attempts = new List<(DateOnly ScheduledDate, decimal Percentage)>();
        foreach (var assessment in assessmentsInCategory)
        {
            var result = await dbContext.AssessmentResults.SingleOrDefaultAsync(
                r => r.AssessmentId == assessment.Id && r.StudentPersonId == context.StudentPersonId
                     && r.IsCurrent && r.Status == RecordStatus.Published,
                cancellationToken);

            if (result?.FinalMark is { } finalMark && assessment.MaxMarks > 0)
            {
                attempts.Add((assessment.ScheduledDate, finalMark / assessment.MaxMarks * 100m));
            }
        }

        if (attempts.Count == 0)
        {
            return null;
        }

        var ordered = attempts.OrderBy(a => a.ScheduledDate).ToList();
        var aggregationRule = await dbContext.ResultAggregationRules.FindAsync([component.ResultAggregationRuleId], cancellationToken);

        return aggregationRule?.Code switch
        {
            ResultAggregationRuleCodes.Latest => ordered[^1].Percentage,
            ResultAggregationRuleCodes.Highest => ordered.Max(a => a.Percentage),
            _ => ordered.Average(a => a.Percentage),
        };
    }
}

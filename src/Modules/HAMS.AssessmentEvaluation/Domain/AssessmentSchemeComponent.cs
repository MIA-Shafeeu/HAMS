namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// One weighted component of an <see cref="AssessmentScheme"/> — e.g. "Term Exam: 60%".
/// <see cref="ResultAggregationRuleId"/> is Phase 8's addition: how multiple <see cref="Assessment"/>
/// instances within this one category (e.g. several continuous-assessment quizzes in a term)
/// combine into this component's single contributing percentage — see
/// <see cref="ResultAggregationRule"/>'s remarks. The actual cross-assessment weighted aggregation
/// into a student's overall subject result is <c>AssessmentEvaluationEngine</c>'s job (Phase 8),
/// not built here; this table only holds the configuration that engine reads.
/// </summary>
public sealed class AssessmentSchemeComponent
{
    public Guid Id { get; init; }

    public Guid AssessmentSchemeId { get; init; }

    public Guid AssessmentCategoryId { get; init; }

    public Guid ResultAggregationRuleId { get; init; }

    public decimal WeightPercentage { get; set; }

    public int DisplayOrder { get; set; }
}

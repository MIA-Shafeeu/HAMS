using HAMS.AssessmentEvaluation.Domain;

namespace HAMS.AssessmentEvaluation.Application;

/// <summary>
/// Assessment scheme/grade-scale/evaluation-period/promotion-policy/assessment-instance setup
/// (build plan Phases 7/8/11 scope) — extracted from what had been purely inline
/// <c>AssessmentEvaluationDbContext</c> queries directly inside <c>AssessmentConfigEndpoints</c>/
/// <c>EvaluationEndpoints</c>/<c>PromotionEndpoints</c>' minimal-API lambdas, the same extraction
/// already done for the OrgCurriculum/PeopleEnrollment/TeachingTimetable admin surfaces this
/// session. The four pure lookups (<see cref="AssessmentCategory"/>/<see cref="ExternalExaminationBoard"/>/
/// <see cref="SpecialResultState"/>/<see cref="ResultAggregationRule"/>) are read-only here —
/// full CRUD over them is Phase A7's reusable lookup-manager scope, not this one.
/// </summary>
public interface IAssessmentConfigAdminService
{
    Task<IReadOnlyList<AssessmentCategory>> GetAssessmentCategoriesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateAssessmentCategoryAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetAssessmentCategoryActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task UpdateAssessmentCategoryAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalExaminationBoard>> GetExternalExaminationBoardsAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateExternalExaminationBoardAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetExternalExaminationBoardActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task UpdateExternalExaminationBoardAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpecialResultState>> GetSpecialResultStatesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateSpecialResultStateAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetSpecialResultStateActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task UpdateSpecialResultStateAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResultAggregationRule>> GetResultAggregationRulesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateResultAggregationRuleAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetResultAggregationRuleActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task UpdateResultAggregationRuleAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreateAssessmentSchemeAsync(string code, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssessmentScheme>> GetAssessmentSchemesAsync(CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders an <c>AssessmentScheme</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateAssessmentSchemeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active assessment category or result aggregation rule with that code exists.</exception>
    Task<Guid> AddAssessmentSchemeComponentAsync(
        Guid schemeId, string assessmentCategoryCode, string resultAggregationRuleCode, decimal weightPercentage, int displayOrder,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssessmentSchemeComponent>> GetAssessmentSchemeComponentsAsync(Guid schemeId, CancellationToken cancellationToken = default);

    /// <summary>Reweights/reorders an <c>AssessmentSchemeComponent</c>. Its category and aggregation rule are <c>init</c>-only by design (they define what the component fundamentally IS) and cannot be changed - remove and re-add the component instead. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateAssessmentSchemeComponentAsync(Guid id, decimal weightPercentage, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreateGradeScaleAsync(string code, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeScale>> GetGradeScalesAsync(CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders a <c>GradeScale</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateGradeScaleAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> AddGradeBandAsync(
        Guid scaleId, string code, string name, decimal minPercentage, decimal maxPercentage, int rank, int displayOrder,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeBand>> GetGradeBandsAsync(Guid scaleId, CancellationToken cancellationToken = default);

    /// <summary>Renames/rebounds/reranks/reorders a <c>GradeBand</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateGradeBandAsync(Guid id, string name, decimal minPercentage, decimal maxPercentage, int rank, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active assessment category with that code exists, or (when supplied) no active external examination board with that code exists.</exception>
    Task<Guid> CreateAssessmentAsync(
        Guid subjectId, Guid gradeId, Guid termId, Guid academicYearId, string assessmentCategoryCode, string title,
        decimal maxMarks, int? durationMinutes, string? externalExaminationBoardCode, string? externalSyllabusCode, DateOnly scheduledDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Assessment>> GetAssessmentsAsync(Guid subjectId, Guid gradeId, Guid termId, CancellationToken cancellationToken = default);

    /// <summary>Reschedules/reconfigures an <c>Assessment</c>. Its category is <c>init</c>-only by design and cannot be changed. Throws <see cref="InvalidOperationException"/> if not found, or if (when supplied) no active external examination board with that code exists.</summary>
    Task UpdateAssessmentAsync(
        Guid id, string title, decimal maxMarks, int? durationMinutes,
        string? externalExaminationBoardCode, string? externalSyllabusCode, DateOnly scheduledDate,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateEvaluationPeriodAsync(Guid academicYearId, string code, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationPeriod>> GetEvaluationPeriodsAsync(Guid academicYearId, CancellationToken cancellationToken = default);

    /// <summary>Renames/reschedules/reorders an <c>EvaluationPeriod</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateEvaluationPeriodAsync(Guid id, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreatePromotionPolicyAsync(string code, string name, int minimumRank, int minimumSubjectsRequiredToClear, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionPolicy>> GetPromotionPoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>Renames/reconfigures a <c>PromotionPolicy</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdatePromotionPolicyAsync(Guid id, string name, int minimumRank, int minimumSubjectsRequiredToClear, CancellationToken cancellationToken = default);
}

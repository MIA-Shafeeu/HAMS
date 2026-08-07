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

    Task<IReadOnlyList<ExternalExaminationBoard>> GetExternalExaminationBoardsAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateExternalExaminationBoardAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetExternalExaminationBoardActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpecialResultState>> GetSpecialResultStatesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateSpecialResultStateAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetSpecialResultStateActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResultAggregationRule>> GetResultAggregationRulesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateResultAggregationRuleAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetResultAggregationRuleActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<Guid> CreateAssessmentSchemeAsync(string code, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssessmentScheme>> GetAssessmentSchemesAsync(CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active assessment category or result aggregation rule with that code exists.</exception>
    Task<Guid> AddAssessmentSchemeComponentAsync(
        Guid schemeId, string assessmentCategoryCode, string resultAggregationRuleCode, decimal weightPercentage, int displayOrder,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssessmentSchemeComponent>> GetAssessmentSchemeComponentsAsync(Guid schemeId, CancellationToken cancellationToken = default);

    Task<Guid> CreateGradeScaleAsync(string code, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeScale>> GetGradeScalesAsync(CancellationToken cancellationToken = default);

    Task<Guid> AddGradeBandAsync(
        Guid scaleId, string code, string name, decimal minPercentage, decimal maxPercentage, int rank, int displayOrder,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GradeBand>> GetGradeBandsAsync(Guid scaleId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active assessment category with that code exists, or (when supplied) no active external examination board with that code exists.</exception>
    Task<Guid> CreateAssessmentAsync(
        Guid subjectId, Guid gradeId, Guid termId, Guid academicYearId, string assessmentCategoryCode, string title,
        decimal maxMarks, int? durationMinutes, string? externalExaminationBoardCode, string? externalSyllabusCode, DateOnly scheduledDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Assessment>> GetAssessmentsAsync(Guid subjectId, Guid gradeId, Guid termId, CancellationToken cancellationToken = default);

    Task<Guid> CreateEvaluationPeriodAsync(Guid academicYearId, string code, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationPeriod>> GetEvaluationPeriodsAsync(Guid academicYearId, CancellationToken cancellationToken = default);

    Task<Guid> CreatePromotionPolicyAsync(string code, string name, int minimumRank, int minimumSubjectsRequiredToClear, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionPolicy>> GetPromotionPoliciesAsync(CancellationToken cancellationToken = default);
}

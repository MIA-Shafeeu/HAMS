using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Application;

internal sealed class AssessmentConfigAdminService(AssessmentEvaluationDbContext dbContext) : IAssessmentConfigAdminService
{
    public async Task<IReadOnlyList<AssessmentCategory>> GetAssessmentCategoriesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AssessmentCategories.OrderBy(c => c.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateAssessmentCategoryAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var category = new AssessmentCategory { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.AssessmentCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    public async Task SetAssessmentCategoryActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.AssessmentCategories.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Assessment category not found.");

        category.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalExaminationBoard>> GetExternalExaminationBoardsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ExternalExaminationBoards.OrderBy(b => b.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateExternalExaminationBoardAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var board = new ExternalExaminationBoard { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.ExternalExaminationBoards.Add(board);
        await dbContext.SaveChangesAsync(cancellationToken);
        return board.Id;
    }

    public async Task SetExternalExaminationBoardActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var board = await dbContext.ExternalExaminationBoards.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("External examination board not found.");

        board.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpecialResultState>> GetSpecialResultStatesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SpecialResultStates.OrderBy(s => s.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateSpecialResultStateAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var state = new SpecialResultState { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.SpecialResultStates.Add(state);
        await dbContext.SaveChangesAsync(cancellationToken);
        return state.Id;
    }

    public async Task SetSpecialResultStateActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var state = await dbContext.SpecialResultStates.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Special result state not found.");

        state.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResultAggregationRule>> GetResultAggregationRulesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ResultAggregationRules.OrderBy(r => r.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateResultAggregationRuleAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var rule = new ResultAggregationRule { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.ResultAggregationRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return rule.Id;
    }

    public async Task SetResultAggregationRuleActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.ResultAggregationRules.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Result aggregation rule not found.");

        rule.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateAssessmentSchemeAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        var scheme = new AssessmentScheme { Id = Guid.NewGuid(), Code = code, Name = name };
        dbContext.AssessmentSchemes.Add(scheme);
        await dbContext.SaveChangesAsync(cancellationToken);
        return scheme.Id;
    }

    public async Task<IReadOnlyList<AssessmentScheme>> GetAssessmentSchemesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AssessmentSchemes.OrderBy(s => s.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> AddAssessmentSchemeComponentAsync(
        Guid schemeId, string assessmentCategoryCode, string resultAggregationRuleCode, decimal weightPercentage, int displayOrder,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.AssessmentCategories.SingleOrDefaultAsync(c => c.Code == assessmentCategoryCode && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active assessment category with code '{assessmentCategoryCode}'.");

        var aggregationRule = await dbContext.ResultAggregationRules.SingleOrDefaultAsync(r => r.Code == resultAggregationRuleCode && r.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active result aggregation rule with code '{resultAggregationRuleCode}'.");

        var component = new AssessmentSchemeComponent
        {
            Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = category.Id,
            ResultAggregationRuleId = aggregationRule.Id, WeightPercentage = weightPercentage, DisplayOrder = displayOrder,
        };
        dbContext.AssessmentSchemeComponents.Add(component);
        await dbContext.SaveChangesAsync(cancellationToken);
        return component.Id;
    }

    public async Task<IReadOnlyList<AssessmentSchemeComponent>> GetAssessmentSchemeComponentsAsync(Guid schemeId, CancellationToken cancellationToken = default) =>
        await dbContext.AssessmentSchemeComponents.Where(c => c.AssessmentSchemeId == schemeId).OrderBy(c => c.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateGradeScaleAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        var scale = new GradeScale { Id = Guid.NewGuid(), Code = code, Name = name };
        dbContext.GradeScales.Add(scale);
        await dbContext.SaveChangesAsync(cancellationToken);
        return scale.Id;
    }

    public async Task<IReadOnlyList<GradeScale>> GetGradeScalesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.GradeScales.OrderBy(s => s.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> AddGradeBandAsync(
        Guid scaleId, string code, string name, decimal minPercentage, decimal maxPercentage, int rank, int displayOrder,
        CancellationToken cancellationToken = default)
    {
        var band = new GradeBand
        {
            Id = Guid.NewGuid(), GradeScaleId = scaleId, Code = code, Name = name,
            MinPercentage = minPercentage, MaxPercentage = maxPercentage, Rank = rank, DisplayOrder = displayOrder,
        };
        dbContext.GradeBands.Add(band);
        await dbContext.SaveChangesAsync(cancellationToken);
        return band.Id;
    }

    public async Task<IReadOnlyList<GradeBand>> GetGradeBandsAsync(Guid scaleId, CancellationToken cancellationToken = default) =>
        await dbContext.GradeBands.Where(b => b.GradeScaleId == scaleId).OrderBy(b => b.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateAssessmentAsync(
        Guid subjectId, Guid gradeId, Guid termId, Guid academicYearId, string assessmentCategoryCode, string title,
        decimal maxMarks, int? durationMinutes, string? externalExaminationBoardCode, string? externalSyllabusCode, DateOnly scheduledDate,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.AssessmentCategories.SingleOrDefaultAsync(c => c.Code == assessmentCategoryCode && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active assessment category with code '{assessmentCategoryCode}'.");

        Guid? examBoardId = null;
        if (externalExaminationBoardCode is not null)
        {
            var board = await dbContext.ExternalExaminationBoards.SingleOrDefaultAsync(b => b.Code == externalExaminationBoardCode && b.IsActive, cancellationToken)
                ?? throw new InvalidOperationException($"No active external examination board with code '{externalExaminationBoardCode}'.");
            examBoardId = board.Id;
        }

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(), SubjectId = subjectId, GradeId = gradeId, TermId = termId,
            AcademicYearId = academicYearId, AssessmentCategoryId = category.Id, Title = title,
            MaxMarks = maxMarks, DurationMinutes = durationMinutes, ExternalExaminationBoardId = examBoardId,
            ExternalSyllabusCode = externalSyllabusCode, ScheduledDate = scheduledDate,
        };
        dbContext.Assessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return assessment.Id;
    }

    public async Task<IReadOnlyList<Assessment>> GetAssessmentsAsync(Guid subjectId, Guid gradeId, Guid termId, CancellationToken cancellationToken = default) =>
        await dbContext.Assessments
            .Where(a => a.SubjectId == subjectId && a.GradeId == gradeId && a.TermId == termId)
            .OrderBy(a => a.ScheduledDate)
            .ToListAsync(cancellationToken);

    public async Task<Guid> CreateEvaluationPeriodAsync(Guid academicYearId, string code, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default)
    {
        var period = new EvaluationPeriod
        {
            Id = Guid.NewGuid(), AcademicYearId = academicYearId, Code = code, Name = name,
            StartDate = startDate, EndDate = endDate, DisplayOrder = displayOrder,
        };
        dbContext.EvaluationPeriods.Add(period);
        await dbContext.SaveChangesAsync(cancellationToken);
        return period.Id;
    }

    public async Task<IReadOnlyList<EvaluationPeriod>> GetEvaluationPeriodsAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.EvaluationPeriods.Where(p => p.AcademicYearId == academicYearId).OrderBy(p => p.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreatePromotionPolicyAsync(string code, string name, int minimumRank, int minimumSubjectsRequiredToClear, CancellationToken cancellationToken = default)
    {
        var policy = new PromotionPolicy
        {
            Id = Guid.NewGuid(), Code = code, Name = name,
            MinimumRank = minimumRank, MinimumSubjectsRequiredToClear = minimumSubjectsRequiredToClear,
        };
        dbContext.PromotionPolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return policy.Id;
    }

    public async Task<IReadOnlyList<PromotionPolicy>> GetPromotionPoliciesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PromotionPolicies.OrderBy(p => p.Code).ToListAsync(cancellationToken);
}

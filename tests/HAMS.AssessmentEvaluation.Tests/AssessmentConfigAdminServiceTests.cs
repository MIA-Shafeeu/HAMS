using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Tests;

public class AssessmentConfigAdminServiceTests
{
    private static AssessmentEvaluationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AssessmentEvaluationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateAssessmentSchemeAsync_is_retrievable_via_GetAssessmentSchemesAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        var schemeId = await service.CreateAssessmentSchemeAsync("KS3_STANDARD", "Key Stage 3 Standard");

        var schemes = await service.GetAssessmentSchemesAsync();
        Assert.Single(schemes, s => s.Id == schemeId);
    }

    [Fact]
    public async Task AddAssessmentSchemeComponentAsync_resolves_category_and_aggregation_rule_by_code()
    {
        await using var db = CreateContext();
        var categoryId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        db.AssessmentCategories.Add(new AssessmentCategory { Id = categoryId, Code = AssessmentCategoryCodes.TermExam, Name = "Term Exam", IsActive = true });
        db.ResultAggregationRules.Add(new ResultAggregationRule { Id = ruleId, Code = ResultAggregationRuleCodes.Latest, Name = "Latest", IsActive = true });
        await db.SaveChangesAsync();
        var service = new AssessmentConfigAdminService(db);
        var schemeId = await service.CreateAssessmentSchemeAsync("KS3", "Key Stage 3");

        var componentId = await service.AddAssessmentSchemeComponentAsync(schemeId, AssessmentCategoryCodes.TermExam, ResultAggregationRuleCodes.Latest, 60m, 1);

        var component = Assert.Single(await service.GetAssessmentSchemeComponentsAsync(schemeId));
        Assert.Equal(componentId, component.Id);
        Assert.Equal(categoryId, component.AssessmentCategoryId);
        Assert.Equal(ruleId, component.ResultAggregationRuleId);
        Assert.Equal(60m, component.WeightPercentage);
    }

    [Fact]
    public async Task AddAssessmentSchemeComponentAsync_throws_for_an_unknown_category_code()
    {
        await using var db = CreateContext();
        db.ResultAggregationRules.Add(new ResultAggregationRule { Id = Guid.NewGuid(), Code = ResultAggregationRuleCodes.Latest, Name = "Latest", IsActive = true });
        await db.SaveChangesAsync();
        var service = new AssessmentConfigAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAssessmentSchemeComponentAsync(Guid.NewGuid(), "NONEXISTENT", ResultAggregationRuleCodes.Latest, 60m, 1));
    }

    [Fact]
    public async Task AddAssessmentSchemeComponentAsync_throws_for_an_unknown_aggregation_rule_code()
    {
        await using var db = CreateContext();
        db.AssessmentCategories.Add(new AssessmentCategory { Id = Guid.NewGuid(), Code = AssessmentCategoryCodes.TermExam, Name = "Term Exam", IsActive = true });
        await db.SaveChangesAsync();
        var service = new AssessmentConfigAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAssessmentSchemeComponentAsync(Guid.NewGuid(), AssessmentCategoryCodes.TermExam, "NONEXISTENT", 60m, 1));
    }

    [Fact]
    public async Task AddGradeBandAsync_is_retrievable_via_GetGradeBandsAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);
        var scaleId = await service.CreateGradeScaleAsync("STD", "Standard Grade Scale");

        var bandId = await service.AddGradeBandAsync(scaleId, "A", "A Grade", 80m, 100m, 5, 1);

        var band = Assert.Single(await service.GetGradeBandsAsync(scaleId));
        Assert.Equal(bandId, band.Id);
        Assert.Equal(5, band.Rank);
    }

    [Fact]
    public async Task CreateAssessmentAsync_resolves_category_by_code_and_leaves_exam_board_null_when_not_supplied()
    {
        await using var db = CreateContext();
        db.AssessmentCategories.Add(new AssessmentCategory { Id = Guid.NewGuid(), Code = AssessmentCategoryCodes.TermExam, Name = "Term Exam", IsActive = true });
        await db.SaveChangesAsync();
        var service = new AssessmentConfigAdminService(db);
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var termId = Guid.NewGuid();

        var assessmentId = await service.CreateAssessmentAsync(
            subjectId, gradeId, termId, Guid.NewGuid(), AssessmentCategoryCodes.TermExam, "Term 1 Exam",
            60m, 120, null, null, new DateOnly(2026, 4, 1));

        var assessment = Assert.Single(await service.GetAssessmentsAsync(subjectId, gradeId, termId));
        Assert.Equal(assessmentId, assessment.Id);
        Assert.Null(assessment.ExternalExaminationBoardId);
    }

    [Fact]
    public async Task CreateAssessmentAsync_resolves_external_examination_board_when_supplied()
    {
        await using var db = CreateContext();
        var categoryId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        db.AssessmentCategories.Add(new AssessmentCategory { Id = categoryId, Code = AssessmentCategoryCodes.TermExam, Name = "Term Exam", IsActive = true });
        db.ExternalExaminationBoards.Add(new ExternalExaminationBoard { Id = boardId, Code = ExternalExaminationBoardCodes.Cambridge, Name = "Cambridge", IsActive = true });
        await db.SaveChangesAsync();
        var service = new AssessmentConfigAdminService(db);
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var termId = Guid.NewGuid();

        await service.CreateAssessmentAsync(
            subjectId, gradeId, termId, Guid.NewGuid(), AssessmentCategoryCodes.TermExam, "IGCSE Maths",
            100m, null, ExternalExaminationBoardCodes.Cambridge, "IGCSE", new DateOnly(2026, 5, 1));

        var assessment = Assert.Single(await service.GetAssessmentsAsync(subjectId, gradeId, termId));
        Assert.Equal(boardId, assessment.ExternalExaminationBoardId);
    }

    [Fact]
    public async Task CreateAssessmentAsync_throws_for_an_unknown_external_examination_board_code()
    {
        await using var db = CreateContext();
        db.AssessmentCategories.Add(new AssessmentCategory { Id = Guid.NewGuid(), Code = AssessmentCategoryCodes.TermExam, Name = "Term Exam", IsActive = true });
        await db.SaveChangesAsync();
        var service = new AssessmentConfigAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAssessmentAsync(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AssessmentCategoryCodes.TermExam, "X",
                100m, null, "NONEXISTENT", null, new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public async Task CreateEvaluationPeriodAsync_is_retrievable_via_GetEvaluationPeriodsAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);
        var academicYearId = Guid.NewGuid();

        var periodId = await service.CreateEvaluationPeriodAsync(academicYearId, "T1", "Term 1", new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30), 1);

        var period = Assert.Single(await service.GetEvaluationPeriodsAsync(academicYearId));
        Assert.Equal(periodId, period.Id);
    }

    [Fact]
    public async Task CreatePromotionPolicyAsync_is_retrievable_via_GetPromotionPoliciesAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        var policyId = await service.CreatePromotionPolicyAsync("STANDARD", "Standard Policy", 3, 5);

        var policy = Assert.Single(await service.GetPromotionPoliciesAsync());
        Assert.Equal(policyId, policy.Id);
        Assert.Equal(3, policy.MinimumRank);
        Assert.Equal(5, policy.MinimumSubjectsRequiredToClear);
    }

    [Fact]
    public async Task GetAssessmentCategoriesAsync_orders_by_display_order()
    {
        await using var db = CreateContext();
        db.AssessmentCategories.Add(new AssessmentCategory { Id = Guid.NewGuid(), Code = "B", Name = "B", DisplayOrder = 2 });
        db.AssessmentCategories.Add(new AssessmentCategory { Id = Guid.NewGuid(), Code = "A", Name = "A", DisplayOrder = 1 });
        await db.SaveChangesAsync();
        var service = new AssessmentConfigAdminService(db);

        var categories = await service.GetAssessmentCategoriesAsync();

        Assert.Equal(["A", "B"], categories.Select(c => c.Code));
    }

    [Fact]
    public async Task CreateAssessmentCategoryAsync_is_retrievable_via_GetAssessmentCategoriesAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        var id = await service.CreateAssessmentCategoryAsync("EXTRA_CREDIT", "Extra Credit", 9);

        var categories = await service.GetAssessmentCategoriesAsync();
        Assert.Single(categories, c => c.Id == id && c.Code == "EXTRA_CREDIT" && c.DisplayOrder == 9);
    }

    [Fact]
    public async Task SetAssessmentCategoryActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);
        var id = await service.CreateAssessmentCategoryAsync("TEMP", "Temp", 1);

        await service.SetAssessmentCategoryActiveAsync(id, false);

        var category = Assert.Single(await service.GetAssessmentCategoriesAsync());
        Assert.False(category.IsActive);
    }

    [Fact]
    public async Task SetAssessmentCategoryActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetAssessmentCategoryActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task CreateExternalExaminationBoardAsync_is_retrievable_via_GetExternalExaminationBoardsAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        var id = await service.CreateExternalExaminationBoardAsync("PEARSON", "Pearson", 5);

        var boards = await service.GetExternalExaminationBoardsAsync();
        Assert.Single(boards, b => b.Id == id && b.Code == "PEARSON" && b.DisplayOrder == 5);
    }

    [Fact]
    public async Task SetExternalExaminationBoardActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);
        var id = await service.CreateExternalExaminationBoardAsync("TEMP", "Temp", 1);

        await service.SetExternalExaminationBoardActiveAsync(id, false);

        var board = Assert.Single(await service.GetExternalExaminationBoardsAsync());
        Assert.False(board.IsActive);
    }

    [Fact]
    public async Task SetExternalExaminationBoardActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetExternalExaminationBoardActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task CreateSpecialResultStateAsync_is_retrievable_via_GetSpecialResultStatesAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        var id = await service.CreateSpecialResultStateAsync("BEREAVEMENT_EXCUSED", "Bereavement Excused", 4);

        var states = await service.GetSpecialResultStatesAsync();
        Assert.Single(states, s => s.Id == id && s.Code == "BEREAVEMENT_EXCUSED" && s.DisplayOrder == 4);
    }

    [Fact]
    public async Task SetSpecialResultStateActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);
        var id = await service.CreateSpecialResultStateAsync("TEMP", "Temp", 1);

        await service.SetSpecialResultStateActiveAsync(id, false);

        var state = Assert.Single(await service.GetSpecialResultStatesAsync());
        Assert.False(state.IsActive);
    }

    [Fact]
    public async Task SetSpecialResultStateActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetSpecialResultStateActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task CreateResultAggregationRuleAsync_is_retrievable_via_GetResultAggregationRulesAsync()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        var id = await service.CreateResultAggregationRuleAsync("CAP", "Cap", 4);

        var rules = await service.GetResultAggregationRulesAsync();
        Assert.Single(rules, r => r.Id == id && r.Code == "CAP" && r.DisplayOrder == 4);
    }

    [Fact]
    public async Task SetResultAggregationRuleActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);
        var id = await service.CreateResultAggregationRuleAsync("TEMP", "Temp", 1);

        await service.SetResultAggregationRuleActiveAsync(id, false);

        var rule = Assert.Single(await service.GetResultAggregationRulesAsync());
        Assert.False(rule.IsActive);
    }

    [Fact]
    public async Task SetResultAggregationRuleActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new AssessmentConfigAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetResultAggregationRuleActiveAsync(Guid.NewGuid(), false));
    }
}

using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.OrgCurriculum.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Tests.Evaluation;

public class AssessmentEvaluationEngineTests
{
    private static AssessmentEvaluationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AssessmentEvaluationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static KeyStagePolicy CreatePolicy(Guid? schemeId, Guid? gradeScaleId) => new()
    {
        Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = Guid.NewGuid(),
        EvaluationModelId = Guid.NewGuid(), AssessmentSchemeId = schemeId, GradeScaleId = gradeScaleId, Status = RecordStatus.Published,
    };

    private static EvaluationPeriod CreatePeriod(Guid academicYearId, DateOnly start, DateOnly end) => new()
    {
        Id = Guid.NewGuid(), AcademicYearId = academicYearId, Code = "T1", Name = "Term 1", StartDate = start, EndDate = end,
    };

    private static async Task<Guid> SeedAggregationRuleAsync(AssessmentEvaluationDbContext db, string code)
    {
        var rule = new ResultAggregationRule { Id = Guid.NewGuid(), Code = code, Name = code };
        db.ResultAggregationRules.Add(rule);
        await db.SaveChangesAsync();
        return rule.Id;
    }

    private static async Task<Guid> SeedCategoryAsync(AssessmentEvaluationDbContext db)
    {
        var category = new AssessmentCategory { Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString("N"), Name = "Category" };
        db.AssessmentCategories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private static async Task<Guid> SeedAssessmentWithResultAsync(
        AssessmentEvaluationDbContext db, Guid subjectId, Guid gradeId, Guid academicYearId, Guid categoryId,
        Guid studentId, DateOnly scheduledDate, decimal maxMarks, decimal? finalMark, bool isCurrent = true, RecordStatus status = RecordStatus.Published)
    {
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(), SubjectId = subjectId, GradeId = gradeId, TermId = Guid.NewGuid(), AcademicYearId = academicYearId,
            AssessmentCategoryId = categoryId, Title = "Test", MaxMarks = maxMarks, ScheduledDate = scheduledDate,
        };
        db.Assessments.Add(assessment);

        if (finalMark is not null || status != RecordStatus.Published)
        {
            db.AssessmentResults.Add(new AssessmentResult
            {
                Id = Guid.NewGuid(), AssessmentId = assessment.Id, StudentPersonId = studentId, KeyStagePolicyId = Guid.NewGuid(),
                FinalMark = finalMark, RecordedByPersonId = Guid.NewGuid(), ModerationStatus = WorkflowStatus.Approved,
                IsCurrent = isCurrent, Status = status,
            });
        }

        await db.SaveChangesAsync();
        return assessment.Id;
    }

    [Fact]
    public async Task EvaluateAsync_throws_when_the_policy_has_no_scheme_or_grade_scale_configured()
    {
        await using var db = CreateContext();
        var engine = new AssessmentEvaluationEngine(db);
        var academicYearId = Guid.NewGuid();
        var context = new EvaluationContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), academicYearId, CreatePolicy(null, null),
            CreatePeriod(academicYearId, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.EvaluateAsync(context));
    }

    [Fact]
    public async Task EvaluateAsync_returns_empty_when_the_scheme_has_no_components()
    {
        await using var db = CreateContext();
        var schemeId = Guid.NewGuid();
        var gradeScaleId = Guid.NewGuid();
        var engine = new AssessmentEvaluationEngine(db);
        var academicYearId = Guid.NewGuid();
        var context = new EvaluationContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), academicYearId, CreatePolicy(schemeId, gradeScaleId),
            CreatePeriod(academicYearId, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(EvaluationOutcome.Empty, result);
    }

    [Fact]
    public async Task EvaluateAsync_computes_a_weighted_average_across_components_and_resolves_the_grade_band()
    {
        await using var db = CreateContext();
        var schemeId = Guid.NewGuid();
        var gradeScaleId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var averageRuleId = await SeedAggregationRuleAsync(db, "AVERAGE");
        var examCategoryId = await SeedCategoryAsync(db);
        var caCategoryId = await SeedCategoryAsync(db);

        db.AssessmentSchemeComponents.AddRange(
            new AssessmentSchemeComponent { Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = examCategoryId, ResultAggregationRuleId = averageRuleId, WeightPercentage = 60 },
            new AssessmentSchemeComponent { Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = caCategoryId, ResultAggregationRuleId = averageRuleId, WeightPercentage = 40 });

        db.GradeBands.AddRange(
            new GradeBand { Id = Guid.NewGuid(), GradeScaleId = gradeScaleId, Code = "A", Name = "A", MinPercentage = 80, MaxPercentage = 100, Rank = 2 },
            new GradeBand { Id = Guid.NewGuid(), GradeScaleId = gradeScaleId, Code = "B", Name = "B", MinPercentage = 60, MaxPercentage = 79.99m, Rank = 1 });
        await db.SaveChangesAsync();

        var period = new DateOnly(2026, 1, 1);
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, examCategoryId, studentId, period, 60, 51); // 85%
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, caCategoryId, studentId, period, 20, 16); // 80%

        var engine = new AssessmentEvaluationEngine(db);
        var context = new EvaluationContext(
            studentId, subjectId, gradeId, academicYearId, CreatePolicy(schemeId, gradeScaleId),
            CreatePeriod(academicYearId, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        // 85*0.6 + 80*0.4 = 51 + 32 = 83
        Assert.Equal(83m, result.OverallPercentage);
        var bandA = await db.GradeBands.SingleAsync(b => b.Code == "A");
        Assert.Equal(bandA.Id, result.GradeBandId);
    }

    [Fact]
    public async Task EvaluateAsync_excludes_a_component_with_no_assessments_from_both_numerator_and_denominator()
    {
        await using var db = CreateContext();
        var schemeId = Guid.NewGuid();
        var gradeScaleId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var averageRuleId = await SeedAggregationRuleAsync(db, "AVERAGE");
        var examCategoryId = await SeedCategoryAsync(db);
        var caCategoryId = await SeedCategoryAsync(db); // never gets any assessments

        db.AssessmentSchemeComponents.AddRange(
            new AssessmentSchemeComponent { Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = examCategoryId, ResultAggregationRuleId = averageRuleId, WeightPercentage = 60 },
            new AssessmentSchemeComponent { Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = caCategoryId, ResultAggregationRuleId = averageRuleId, WeightPercentage = 40 });
        await db.SaveChangesAsync();

        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, examCategoryId, studentId, new DateOnly(2026, 1, 1), 60, 45); // 75%

        var engine = new AssessmentEvaluationEngine(db);
        var context = new EvaluationContext(
            studentId, subjectId, gradeId, academicYearId, CreatePolicy(schemeId, gradeScaleId),
            CreatePeriod(academicYearId, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        // Only the exam component has data -> overall percentage is just that component's, not diluted by the empty CA component.
        Assert.Equal(75m, result.OverallPercentage);
    }

    [Fact]
    public async Task EvaluateAsync_skips_a_medical_certificate_excused_result_with_no_final_mark_rather_than_treating_it_as_zero()
    {
        await using var db = CreateContext();
        var schemeId = Guid.NewGuid();
        var gradeScaleId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var averageRuleId = await SeedAggregationRuleAsync(db, "AVERAGE");
        var examCategoryId = await SeedCategoryAsync(db);

        db.AssessmentSchemeComponents.Add(
            new AssessmentSchemeComponent { Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = examCategoryId, ResultAggregationRuleId = averageRuleId, WeightPercentage = 100 });
        await db.SaveChangesAsync();

        // One normal result and one excused (no FinalMark) result in the same category.
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, examCategoryId, studentId, new DateOnly(2026, 1, 1), 60, 48); // 80%
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, examCategoryId, studentId, new DateOnly(2026, 2, 1), 60, null);

        var engine = new AssessmentEvaluationEngine(db);
        var context = new EvaluationContext(
            studentId, subjectId, gradeId, academicYearId, CreatePolicy(schemeId, gradeScaleId),
            CreatePeriod(academicYearId, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(80m, result.OverallPercentage);
    }

    [Fact]
    public async Task EvaluateAsync_ignores_a_result_that_is_not_current_or_not_yet_Published()
    {
        await using var db = CreateContext();
        var schemeId = Guid.NewGuid();
        var gradeScaleId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var averageRuleId = await SeedAggregationRuleAsync(db, "AVERAGE");
        var examCategoryId = await SeedCategoryAsync(db);

        db.AssessmentSchemeComponents.Add(
            new AssessmentSchemeComponent { Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = examCategoryId, ResultAggregationRuleId = averageRuleId, WeightPercentage = 100 });
        await db.SaveChangesAsync();

        // A Draft result (not yet moderated/Published) must not contribute.
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, examCategoryId, studentId, new DateOnly(2026, 1, 1), 60, 30, status: RecordStatus.Draft);

        var engine = new AssessmentEvaluationEngine(db);
        var context = new EvaluationContext(
            studentId, subjectId, gradeId, academicYearId, CreatePolicy(schemeId, gradeScaleId),
            CreatePeriod(academicYearId, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(EvaluationOutcome.Empty, result);
    }

    [Theory]
    [InlineData("LATEST", 65)]
    [InlineData("HIGHEST", 100)]
    [InlineData("AVERAGE", 75)]
    public async Task EvaluateAsync_applies_the_components_configured_aggregation_rule_across_multiple_attempts(string ruleCode, decimal expectedPercentage)
    {
        await using var db = CreateContext();
        var schemeId = Guid.NewGuid();
        var gradeScaleId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var ruleId = await SeedAggregationRuleAsync(db, ruleCode);
        var caCategoryId = await SeedCategoryAsync(db);

        db.AssessmentSchemeComponents.Add(
            new AssessmentSchemeComponent { Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = caCategoryId, ResultAggregationRuleId = ruleId, WeightPercentage = 100 });
        await db.SaveChangesAsync();

        // Three CA attempts, chronologically: 60%, 100%, 65% (in that scheduled order) — chosen so
        // Latest (65) / Highest (100) / Average (75) are all distinct, a stronger test than values
        // that happen to coincide across rules.
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, caCategoryId, studentId, new DateOnly(2026, 1, 1), 100, 60);
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, caCategoryId, studentId, new DateOnly(2026, 2, 1), 100, 100);
        await SeedAssessmentWithResultAsync(db, subjectId, gradeId, academicYearId, caCategoryId, studentId, new DateOnly(2026, 3, 1), 100, 65);

        var engine = new AssessmentEvaluationEngine(db);
        var context = new EvaluationContext(
            studentId, subjectId, gradeId, academicYearId, CreatePolicy(schemeId, gradeScaleId),
            CreatePeriod(academicYearId, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(expectedPercentage, result.OverallPercentage);
    }
}

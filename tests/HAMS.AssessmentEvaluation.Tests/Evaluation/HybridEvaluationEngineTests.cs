using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.LearningDelivery.Domain;
using HAMS.OrgCurriculum.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Tests.Evaluation;

public class HybridEvaluationEngineTests
{
    private static AssessmentEvaluationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AssessmentEvaluationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task EvaluateAsync_merges_the_mastery_level_and_the_assessment_percentage_and_band_from_both_engines()
    {
        await using var db = CreateContext();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var scaleId = Guid.NewGuid();
        var schemeId = Guid.NewGuid();
        var gradeScaleId = Guid.NewGuid();

        var syllabus = new Syllabus { Id = Guid.NewGuid(), SubjectId = subjectId, Status = RecordStatus.Published };
        var outcomeId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var masteryEvaluations = new Dictionary<Guid, MasteryEvaluation>
        {
            [outcomeId] = new()
            {
                Id = Guid.NewGuid(), StudentPersonId = studentId, LearningOutcomeId = outcomeId, KeyStagePolicyId = Guid.NewGuid(),
                AchievementScaleId = scaleId, AchievementLevelId = levelId, RecordedByPersonId = Guid.NewGuid(),
                RecordedAtUtc = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            },
        };
        var masteryEngine = new MasteryEvaluationEngine(
            new FakeSyllabusResolver(syllabus, [outcomeId]), new FakeMasteryEvaluationService(masteryEvaluations),
            new FakeAchievementScaleQuery(new Dictionary<Guid, int> { [levelId] = 1 }));

        var ruleId = Guid.NewGuid();
        db.ResultAggregationRules.Add(new ResultAggregationRule { Id = ruleId, Code = "AVERAGE", Name = "Average" });
        var categoryId = Guid.NewGuid();
        db.AssessmentCategories.Add(new AssessmentCategory { Id = categoryId, Code = "CAT", Name = "Category" });
        db.AssessmentSchemeComponents.Add(new AssessmentSchemeComponent
        {
            Id = Guid.NewGuid(), AssessmentSchemeId = schemeId, AssessmentCategoryId = categoryId, ResultAggregationRuleId = ruleId, WeightPercentage = 100,
        });
        var bandId = Guid.NewGuid();
        db.GradeBands.Add(new GradeBand { Id = bandId, GradeScaleId = gradeScaleId, Code = "A", Name = "A", MinPercentage = 0, MaxPercentage = 100, Rank = 1 });
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(), SubjectId = subjectId, GradeId = gradeId, TermId = Guid.NewGuid(), AcademicYearId = academicYearId,
            AssessmentCategoryId = categoryId, Title = "Exam", MaxMarks = 100, ScheduledDate = new DateOnly(2026, 1, 15),
        };
        db.Assessments.Add(assessment);
        db.AssessmentResults.Add(new AssessmentResult
        {
            Id = Guid.NewGuid(), AssessmentId = assessment.Id, StudentPersonId = studentId, KeyStagePolicyId = Guid.NewGuid(),
            FinalMark = 70, RecordedByPersonId = Guid.NewGuid(), IsCurrent = true, Status = RecordStatus.Published,
        });
        await db.SaveChangesAsync();
        var assessmentEngine = new AssessmentEvaluationEngine(db);

        var hybridEngine = new HybridEvaluationEngine(masteryEngine, assessmentEngine);
        var policy = new KeyStagePolicy
        {
            Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = Guid.NewGuid(),
            AchievementScaleId = scaleId, AssessmentSchemeId = schemeId, GradeScaleId = gradeScaleId, Status = RecordStatus.Published,
        };
        var period = new EvaluationPeriod { Id = Guid.NewGuid(), AcademicYearId = academicYearId, Code = "T1", Name = "Term 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 4, 30) };
        var context = new EvaluationContext(studentId, subjectId, gradeId, academicYearId, policy, period);

        var result = await hybridEngine.EvaluateAsync(context);

        Assert.Equal(levelId, result.AchievementLevelId);
        Assert.Equal(70m, result.OverallPercentage);
        Assert.Equal(bandId, result.GradeBandId);
    }
}

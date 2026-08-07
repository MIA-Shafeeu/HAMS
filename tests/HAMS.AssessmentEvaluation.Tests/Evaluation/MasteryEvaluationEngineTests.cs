using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.LearningDelivery.Domain;
using HAMS.OrgCurriculum.Domain;
using HAMS.Platform.Common.Contracts;

namespace HAMS.AssessmentEvaluation.Tests.Evaluation;

public class MasteryEvaluationEngineTests
{
    private static KeyStagePolicy CreatePolicy(Guid? achievementScaleId) => new()
    {
        Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = Guid.NewGuid(),
        EvaluationModelId = Guid.NewGuid(), AchievementScaleId = achievementScaleId, Status = RecordStatus.Published,
    };

    private static EvaluationPeriod CreatePeriod(DateOnly start, DateOnly end) => new()
    {
        Id = Guid.NewGuid(), AcademicYearId = Guid.NewGuid(), Code = "T1", Name = "Term 1", StartDate = start, EndDate = end,
    };

    private static MasteryEvaluation CreateEvaluation(Guid outcomeId, Guid levelId, DateTimeOffset recordedAtUtc) => new()
    {
        Id = Guid.NewGuid(), StudentPersonId = Guid.NewGuid(), LearningOutcomeId = outcomeId, KeyStagePolicyId = Guid.NewGuid(),
        AchievementScaleId = Guid.NewGuid(), AchievementLevelId = levelId, RecordedByPersonId = Guid.NewGuid(), RecordedAtUtc = recordedAtUtc,
    };

    [Fact]
    public async Task EvaluateAsync_throws_when_the_policy_has_no_achievement_scale_configured()
    {
        var engine = new MasteryEvaluationEngine(
            new FakeSyllabusResolver(null), new FakeMasteryEvaluationService(new Dictionary<Guid, MasteryEvaluation>()),
            new FakeAchievementScaleQuery(new Dictionary<Guid, int>()));
        var context = new EvaluationContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CreatePolicy(null), CreatePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.EvaluateAsync(context));
    }

    [Fact]
    public async Task EvaluateAsync_returns_empty_when_the_subject_has_no_published_syllabus()
    {
        var engine = new MasteryEvaluationEngine(
            new FakeSyllabusResolver(null), new FakeMasteryEvaluationService(new Dictionary<Guid, MasteryEvaluation>()),
            new FakeAchievementScaleQuery(new Dictionary<Guid, int>()));
        var context = new EvaluationContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CreatePolicy(Guid.NewGuid()), CreatePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(EvaluationOutcome.Empty, result);
    }

    [Fact]
    public async Task EvaluateAsync_returns_empty_when_no_outcome_has_been_evaluated_within_the_period()
    {
        var syllabus = new Syllabus { Id = Guid.NewGuid(), SubjectId = Guid.NewGuid(), Status = RecordStatus.Published };
        var outcomeId = Guid.NewGuid();
        var evaluations = new Dictionary<Guid, MasteryEvaluation>
        {
            [outcomeId] = CreateEvaluation(outcomeId, Guid.NewGuid(), new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero)), // before the period
        };
        var engine = new MasteryEvaluationEngine(
            new FakeSyllabusResolver(syllabus, [outcomeId]), new FakeMasteryEvaluationService(evaluations),
            new FakeAchievementScaleQuery(new Dictionary<Guid, int>()));
        var context = new EvaluationContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CreatePolicy(Guid.NewGuid()), CreatePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(EvaluationOutcome.Empty, result);
    }

    [Fact]
    public async Task EvaluateAsync_aggregates_the_mode_across_outcomes_with_ties_broken_toward_the_lower_rank()
    {
        var syllabus = new Syllabus { Id = Guid.NewGuid(), SubjectId = Guid.NewGuid(), Status = RecordStatus.Published };
        var outcome1 = Guid.NewGuid();
        var outcome2 = Guid.NewGuid();
        var outcome3 = Guid.NewGuid();
        var low = Guid.NewGuid();
        var high = Guid.NewGuid();
        var recordedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var evaluations = new Dictionary<Guid, MasteryEvaluation>
        {
            [outcome1] = CreateEvaluation(outcome1, low, recordedAt),
            [outcome2] = CreateEvaluation(outcome2, high, recordedAt),
            [outcome3] = CreateEvaluation(outcome3, low, recordedAt),
        };
        var ranks = new Dictionary<Guid, int> { [low] = 1, [high] = 3 };
        var engine = new MasteryEvaluationEngine(
            new FakeSyllabusResolver(syllabus, [outcome1, outcome2, outcome3]), new FakeMasteryEvaluationService(evaluations),
            new FakeAchievementScaleQuery(ranks));
        var context = new EvaluationContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CreatePolicy(Guid.NewGuid()), CreatePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(low, result.AchievementLevelId); // 2 of 3 outcomes at "low"
        Assert.Null(result.OverallPercentage);
        Assert.Null(result.GradeBandId);
    }

    [Fact]
    public async Task EvaluateAsync_breaks_an_exact_tie_toward_the_lower_ranked_level()
    {
        var syllabus = new Syllabus { Id = Guid.NewGuid(), SubjectId = Guid.NewGuid(), Status = RecordStatus.Published };
        var outcome1 = Guid.NewGuid();
        var outcome2 = Guid.NewGuid();
        var low = Guid.NewGuid();
        var high = Guid.NewGuid();
        var recordedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var evaluations = new Dictionary<Guid, MasteryEvaluation>
        {
            [outcome1] = CreateEvaluation(outcome1, low, recordedAt),
            [outcome2] = CreateEvaluation(outcome2, high, recordedAt),
        };
        var ranks = new Dictionary<Guid, int> { [low] = 1, [high] = 3 };
        var engine = new MasteryEvaluationEngine(
            new FakeSyllabusResolver(syllabus, [outcome1, outcome2]), new FakeMasteryEvaluationService(evaluations),
            new FakeAchievementScaleQuery(ranks));
        var context = new EvaluationContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CreatePolicy(Guid.NewGuid()), CreatePeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var result = await engine.EvaluateAsync(context);

        Assert.Equal(low, result.AchievementLevelId);
    }
}

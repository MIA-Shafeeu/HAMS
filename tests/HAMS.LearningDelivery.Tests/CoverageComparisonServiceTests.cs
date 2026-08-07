using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class CoverageComparisonServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed record DeliveryChain(Guid ItemId, Guid TopicId, Guid PlanId, Guid SessionId);

    private static async Task<DeliveryChain> SeedDeliveryChainAsync(
        LearningDeliveryDbContext db, Guid schemeOfWorkId, Guid outcomeId, LessonSessionStatus sessionStatus)
    {
        var item = new SchemeOfWorkItem { Id = Guid.NewGuid(), SchemeOfWorkId = schemeOfWorkId, LearningOutcomeId = outcomeId };
        db.SchemeOfWorkItems.Add(item);

        var topic = new TeachingTopic { Id = Guid.NewGuid(), SchemeOfWorkItemId = item.Id, NameEn = "Topic", NameDv = "Topic (Dv)" };
        db.TeachingTopics.Add(topic);

        var plan = new LessonPlan
        {
            Id = Guid.NewGuid(), TeachingTopicId = topic.Id, StaffPersonId = Guid.NewGuid(),
            PlannedDate = new DateOnly(2026, 1, 4), Objectives = "Objectives",
        };
        db.LessonPlans.Add(plan);

        var session = new LessonSession
        {
            Id = Guid.NewGuid(), LessonPlanId = plan.Id, ClassId = Guid.NewGuid(),
            ActualDate = new DateOnly(2026, 1, 4), PeriodId = Guid.NewGuid(), Status = sessionStatus,
        };
        db.LessonSessions.Add(session);

        await db.SaveChangesAsync();
        return new DeliveryChain(item.Id, topic.Id, plan.Id, session.Id);
    }

    [Fact]
    public async Task CompareAsync_reports_full_coverage_when_every_planned_outcome_was_covered_by_a_completed_session()
    {
        await using var db = CreateContext();
        var schemeOfWorkId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        var chain = await SeedDeliveryChainAsync(db, schemeOfWorkId, outcomeId, LessonSessionStatus.Completed);
        db.LessonSessionOutcomeCoverages.Add(new LessonSessionOutcomeCoverage { Id = Guid.NewGuid(), LessonSessionId = chain.SessionId, LearningOutcomeId = outcomeId });
        await db.SaveChangesAsync();
        var service = new CoverageComparisonService(db);

        var result = await service.CompareAsync(schemeOfWorkId);

        Assert.Equal(1, result.PlannedOutcomeCount);
        Assert.Equal(1, result.CoveredOutcomeCount);
        Assert.Empty(result.UncoveredOutcomeIds);
    }

    [Fact]
    public async Task CompareAsync_reports_an_outcome_as_uncovered_when_its_session_was_never_completed()
    {
        // Coverage rows only get written by LessonSessionService.CompleteAsync, but this test seeds
        // one directly on a still-Planned session to prove the comparison itself enforces LES-FR-012
        // ("only Completed sessions count"), not just that the write path happens to obey it.
        await using var db = CreateContext();
        var schemeOfWorkId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        var chain = await SeedDeliveryChainAsync(db, schemeOfWorkId, outcomeId, LessonSessionStatus.Planned);
        db.LessonSessionOutcomeCoverages.Add(new LessonSessionOutcomeCoverage { Id = Guid.NewGuid(), LessonSessionId = chain.SessionId, LearningOutcomeId = outcomeId });
        await db.SaveChangesAsync();
        var service = new CoverageComparisonService(db);

        var result = await service.CompareAsync(schemeOfWorkId);

        Assert.Equal(1, result.PlannedOutcomeCount);
        Assert.Equal(0, result.CoveredOutcomeCount);
        Assert.Equal([outcomeId], result.UncoveredOutcomeIds);
    }

    [Fact]
    public async Task CompareAsync_only_counts_coverage_delivered_through_this_scheme_of_works_own_chain()
    {
        // An outcome covered via a *different* scheme of work's delivery chain must not count toward
        // this one, even if the LearningOutcomeId happens to match.
        await using var db = CreateContext();
        var thisSchemeId = Guid.NewGuid();
        var otherSchemeId = Guid.NewGuid();
        var sharedOutcomeId = Guid.NewGuid();

        await SeedDeliveryChainAsync(db, thisSchemeId, sharedOutcomeId, LessonSessionStatus.Completed);
        var otherChain = await SeedDeliveryChainAsync(db, otherSchemeId, sharedOutcomeId, LessonSessionStatus.Completed);
        db.LessonSessionOutcomeCoverages.Add(new LessonSessionOutcomeCoverage { Id = Guid.NewGuid(), LessonSessionId = otherChain.SessionId, LearningOutcomeId = sharedOutcomeId });
        await db.SaveChangesAsync();
        var service = new CoverageComparisonService(db);

        var result = await service.CompareAsync(thisSchemeId);

        Assert.Equal(1, result.PlannedOutcomeCount);
        Assert.Equal(0, result.CoveredOutcomeCount);
        Assert.Equal([sharedOutcomeId], result.UncoveredOutcomeIds);
    }

    [Fact]
    public async Task CompareAsync_returns_zero_planned_when_the_scheme_of_work_has_no_items()
    {
        await using var db = CreateContext();
        var service = new CoverageComparisonService(db);

        var result = await service.CompareAsync(Guid.NewGuid());

        Assert.Equal(0, result.PlannedOutcomeCount);
        Assert.Equal(0, result.CoveredOutcomeCount);
        Assert.Empty(result.UncoveredOutcomeIds);
    }
}

using HAMS.Intervention.Application;
using HAMS.Intervention.Infrastructure;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Tests;

public class TopicClosureServiceTests
{
    private static InterventionDbContext CreateContext() => new(
        new DbContextOptionsBuilder<InterventionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TopicClosureService CreateService(InterventionDbContext db, Guid? learningOutcomeId = null, DateOnly? today = null)
        => new(db, new WorkflowEngine(), new FakeTeachingTopicQuery(learningOutcomeId ?? Guid.NewGuid()), new FakeClock(today ?? new DateOnly(2026, 8, 5)));

    [Fact]
    public async Task RequestClosureAsync_creates_a_Draft_closure()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var topicId = Guid.NewGuid();

        var closureId = await service.RequestClosureAsync(topicId, Guid.NewGuid());

        var closure = await db.TopicClosures.SingleAsync(c => c.Id == closureId);
        Assert.Equal(topicId, closure.TeachingTopicId);
        Assert.Equal(WorkflowStatus.Draft, closure.Status);
    }

    [Fact]
    public async Task Full_pipeline_from_submit_to_approve_reaches_Approved()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var closureId = await service.RequestClosureAsync(Guid.NewGuid(), Guid.NewGuid());

        await service.SubmitAsync(closureId);
        await service.BeginReviewAsync(closureId, Guid.NewGuid());
        await service.ApproveAsync(closureId, Guid.NewGuid(), "Looks complete.", []);

        var closure = await db.TopicClosures.SingleAsync(c => c.Id == closureId);
        Assert.Equal(WorkflowStatus.Approved, closure.Status);
        Assert.Equal("Looks complete.", closure.ReviewNotes);
        Assert.NotNull(closure.DecidedAtUtc);
    }

    [Fact]
    public async Task ApproveAsync_creates_a_CarriedForwardGap_for_each_named_student()
    {
        await using var db = CreateContext();
        var outcomeId = Guid.NewGuid();
        var service = CreateService(db, outcomeId);
        var closureId = await service.RequestClosureAsync(Guid.NewGuid(), Guid.NewGuid());
        await service.SubmitAsync(closureId);
        await service.BeginReviewAsync(closureId, Guid.NewGuid());

        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        await service.ApproveAsync(closureId, Guid.NewGuid(), null, [studentA, studentB]);

        var gaps = await db.CarriedForwardGaps.Where(g => g.TopicClosureId == closureId).ToListAsync();
        Assert.Equal(2, gaps.Count);
        Assert.All(gaps, g => Assert.Equal(outcomeId, g.LearningOutcomeId));
        Assert.Contains(gaps, g => g.StudentPersonId == studentA);
        Assert.Contains(gaps, g => g.StudentPersonId == studentB);
        Assert.All(gaps, g => Assert.False(g.IsResolved));
    }

    [Fact]
    public async Task ApproveAsync_with_no_gap_students_creates_no_gap_rows()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var closureId = await service.RequestClosureAsync(Guid.NewGuid(), Guid.NewGuid());
        await service.SubmitAsync(closureId);
        await service.BeginReviewAsync(closureId, Guid.NewGuid());

        await service.ApproveAsync(closureId, Guid.NewGuid(), null, []);

        Assert.Empty(await db.CarriedForwardGaps.Where(g => g.TopicClosureId == closureId).ToListAsync());
    }

    [Fact]
    public async Task RejectAsync_and_ReturnAsync_move_to_the_expected_terminal_or_correctable_state()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var rejectedClosureId = await service.RequestClosureAsync(Guid.NewGuid(), Guid.NewGuid());
        await service.SubmitAsync(rejectedClosureId);
        await service.BeginReviewAsync(rejectedClosureId, Guid.NewGuid());
        await service.RejectAsync(rejectedClosureId, Guid.NewGuid(), "Not enough evidence.");
        Assert.Equal(WorkflowStatus.Rejected, (await db.TopicClosures.SingleAsync(c => c.Id == rejectedClosureId)).Status);

        var returnedClosureId = await service.RequestClosureAsync(Guid.NewGuid(), Guid.NewGuid());
        await service.SubmitAsync(returnedClosureId);
        await service.BeginReviewAsync(returnedClosureId, Guid.NewGuid());
        await service.ReturnAsync(returnedClosureId, Guid.NewGuid(), "Please add coverage notes.");
        var returned = await db.TopicClosures.SingleAsync(c => c.Id == returnedClosureId);
        Assert.Equal(WorkflowStatus.Returned, returned.Status);

        // Returned closures can be resubmitted, the same as assessment moderation's Returned state.
        await service.SubmitAsync(returnedClosureId);
        Assert.Equal(WorkflowStatus.Submitted, (await db.TopicClosures.SingleAsync(c => c.Id == returnedClosureId)).Status);
    }

    [Fact]
    public async Task ApproveAsync_rejects_an_illegal_transition_from_Draft()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var closureId = await service.RequestClosureAsync(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidWorkflowTransitionException>(() =>
            service.ApproveAsync(closureId, Guid.NewGuid(), null, []));
    }

    [Fact]
    public async Task GetCurrentAsync_returns_the_most_recently_requested_closure()
    {
        await using var db = CreateContext();
        var topicId = Guid.NewGuid();
        var serviceEarlier = CreateService(db, today: new DateOnly(2026, 8, 1));
        var earlierId = await serviceEarlier.RequestClosureAsync(topicId, Guid.NewGuid());
        var serviceLater = CreateService(db, today: new DateOnly(2026, 8, 5));
        var laterId = await serviceLater.RequestClosureAsync(topicId, Guid.NewGuid());

        var current = await serviceLater.GetCurrentAsync(topicId);

        Assert.NotNull(current);
        Assert.Equal(laterId, current!.Id);
        Assert.NotEqual(earlierId, current.Id);
    }

    [Fact]
    public async Task GetCurrentAsync_returns_null_when_no_closure_has_been_requested()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        Assert.Null(await service.GetCurrentAsync(Guid.NewGuid()));
    }
}

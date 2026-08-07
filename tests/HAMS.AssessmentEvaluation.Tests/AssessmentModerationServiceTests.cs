using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.AssessmentEvaluation.Tests.Evaluation;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Tests;

public class AssessmentModerationServiceTests
{
    private static AssessmentEvaluationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AssessmentEvaluationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SaveChangesGuardInterceptor())
            .Options);

    private static AssessmentModerationService CreateService(
        AssessmentEvaluationDbContext db, FakeGuardianContactResolver? guardianContacts = null, FakeNotificationOutboxWriter? outboxWriter = null)
        => new(
            db, new WorkflowEngine(), guardianContacts ?? new FakeGuardianContactResolver(), outboxWriter ?? new FakeNotificationOutboxWriter(),
            new FakeClock(new DateOnly(2026, 8, 5)));

    [Fact]
    public async Task RecordRawMarkAsync_creates_a_Draft_result()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 45m, null, Guid.NewGuid());

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(45m, result.RawMark);
        Assert.Equal(WorkflowStatus.Draft, result.ModerationStatus);
        Assert.Equal(RecordStatus.Draft, result.Status);
    }

    [Fact]
    public async Task RecordRawMarkAsync_throws_when_neither_a_mark_nor_a_special_state_is_given()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, Guid.NewGuid()));
    }

    [Fact]
    public async Task RecordRawMarkAsync_allows_a_special_result_state_with_no_mark()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var specialStateId = Guid.NewGuid();

        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, specialStateId, Guid.NewGuid());

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Null(result.RawMark);
        Assert.Equal(specialStateId, result.SpecialResultStateId);
    }

    [Fact]
    public async Task Full_pipeline_from_submit_to_approve_settles_FinalMark_and_publishes()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());

        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, adjustedMark: 42m);
        await service.ApproveAsync(resultId, moderatedMark: 44m);

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(40m, result.RawMark);
        Assert.Equal(42m, result.AdjustedMark);
        Assert.Equal(44m, result.ModeratedMark);
        Assert.Equal(44m, result.FinalMark);
        Assert.Equal(WorkflowStatus.Approved, result.ModerationStatus);
        Assert.Equal(RecordStatus.Published, result.Status);
    }

    [Fact]
    public async Task EscalateAsync_moves_UnderReview_to_Escalated_and_records_who_and_why()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, adjustedMark: null);
        var escalatedBy = Guid.NewGuid();

        await service.EscalateAsync(resultId, escalatedBy, "Disputed by the student's parent.");

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(WorkflowStatus.Escalated, result.ModerationStatus);
        Assert.Equal(escalatedBy, result.EscalatedByPersonId);
        Assert.Equal("Disputed by the student's parent.", result.EscalationReason);
    }

    [Fact]
    public async Task EscalateAsync_rejects_an_illegal_transition_from_Draft()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidWorkflowTransitionException>(() => service.EscalateAsync(resultId, Guid.NewGuid(), "x"));
    }

    [Fact]
    public async Task ApproveAsync_finishes_an_Escalated_result_exactly_like_an_ordinary_UnderReview_one()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, adjustedMark: null);
        await service.EscalateAsync(resultId, Guid.NewGuid(), "Disputed mark.");

        await service.ApproveAsync(resultId, moderatedMark: 44m);

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(WorkflowStatus.Approved, result.ModerationStatus);
        Assert.Equal(44m, result.FinalMark);
        Assert.Equal(RecordStatus.Published, result.Status);
    }

    [Fact]
    public async Task RejectAsync_finishes_an_Escalated_result_exactly_like_an_ordinary_UnderReview_one()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, adjustedMark: null);
        await service.EscalateAsync(resultId, Guid.NewGuid(), "Disputed mark.");

        await service.RejectAsync(resultId);

        Assert.Equal(WorkflowStatus.Rejected, (await db.AssessmentResults.SingleAsync(r => r.Id == resultId)).ModerationStatus);
    }

    [Fact]
    public async Task ApproveAsync_without_a_moderated_mark_falls_back_to_adjusted_then_raw()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, adjustedMark: null);

        await service.ApproveAsync(resultId, moderatedMark: null);

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(40m, result.FinalMark);
    }

    [Fact]
    public async Task BeginReviewAsync_rejects_setting_AdjustedMark_a_second_time()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, adjustedMark: 42m);

        // BeginReview a second time isn't a legal workflow move anyway (Submitted->Review only),
        // so directly assert the AdjustedMark guard by attempting the same call state manually.
        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        result.ModerationStatus = WorkflowStatus.Submitted; // simulate being eligible to review again
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BeginReviewAsync(resultId, adjustedMark: 50m));
    }

    [Fact]
    public async Task RejectAsync_leaves_the_row_Draft_and_correctable()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, null);

        await service.RejectAsync(resultId);

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(WorkflowStatus.Rejected, result.ModerationStatus);
        Assert.Equal(RecordStatus.Draft, result.Status);
    }

    [Fact]
    public async Task Returned_results_can_be_revised_and_resubmitted()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, null);
        await service.ReturnAsync(resultId);

        await service.ReviseRawMarkAsync(resultId, 41m, null);
        await service.SubmitAsync(resultId);

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(41m, result.RawMark);
        Assert.Equal(WorkflowStatus.Submitted, result.ModerationStatus);
    }

    [Fact]
    public async Task ReviseRawMarkAsync_rejects_revising_a_result_that_is_Submitted()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviseRawMarkAsync(resultId, 45m, null));
    }

    [Fact]
    public async Task ReviseApprovedResultAsync_supersedes_the_original_and_creates_a_new_Approved_version()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, null);
        await service.ApproveAsync(resultId, null);

        var revisedId = await service.ReviseApprovedResultAsync(resultId, newFinalMark: 55m);

        var original = await db.AssessmentResults.AsNoTracking().SingleAsync(r => r.Id == resultId);
        var revised = await db.AssessmentResults.AsNoTracking().SingleAsync(r => r.Id == revisedId);

        Assert.False(original.IsCurrent);
        Assert.Equal(RecordStatus.Superseded, original.Status);
        Assert.Equal(revisedId, original.SupersededById);

        Assert.True(revised.IsCurrent);
        Assert.Equal(RecordStatus.Published, revised.Status);
        Assert.Equal(resultId, revised.SupersedesId);
        Assert.Equal(55m, revised.FinalMark);
        Assert.Equal(WorkflowStatus.Approved, revised.ModerationStatus);
    }

    [Fact]
    public async Task ReviseApprovedResultAsync_rejects_a_result_that_is_still_Draft()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviseApprovedResultAsync(resultId, 55m));
    }

    [Fact]
    public async Task Directly_modifying_an_approved_result_outside_the_service_still_throws()
    {
        // Regression guard: ReviseApprovedResultAsync must go through ImmutableRecordCorrectionScope
        // internally, but nothing else gets a free pass at mutating an already-Published result.
        await using var db = CreateContext();
        var service = CreateService(db);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, null);
        await service.ApproveAsync(resultId, null);

        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        result.FinalMark = 999m;

        await Assert.ThrowsAsync<ImmutableRecordMutationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ApproveAsync_enqueues_a_guardian_notification_when_a_notifiable_guardian_exists()
    {
        await using var db = CreateContext();
        var contacts = new FakeGuardianContactResolver(new GuardianContact(Guid.NewGuid(), "+9609999999", null));
        var outbox = new FakeNotificationOutboxWriter();
        var service = CreateService(db, contacts, outbox);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, null);

        await service.ApproveAsync(resultId, null);

        Assert.Single(outbox.Enqueued);
        Assert.Equal("+9609999999", outbox.Enqueued[0].Recipient);
        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(RecordStatus.Published, result.Status);
    }

    [Fact]
    public async Task ApproveAsync_enqueues_nothing_when_no_guardian_is_notifiable()
    {
        await using var db = CreateContext();
        var outbox = new FakeNotificationOutboxWriter();
        var service = CreateService(db, outboxWriter: outbox);
        var resultId = await service.RecordRawMarkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40m, null, Guid.NewGuid());
        await service.SubmitAsync(resultId);
        await service.BeginReviewAsync(resultId, null);

        await service.ApproveAsync(resultId, null);

        Assert.Empty(outbox.Enqueued);
        var result = await db.AssessmentResults.SingleAsync(r => r.Id == resultId);
        Assert.Equal(RecordStatus.Published, result.Status);
    }
}

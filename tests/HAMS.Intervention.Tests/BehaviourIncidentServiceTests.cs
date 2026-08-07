using HAMS.Intervention.Application;
using HAMS.Intervention.Infrastructure;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Tests;

public class BehaviourIncidentServiceTests
{
    private static InterventionDbContext CreateContext() => new(
        new DbContextOptionsBuilder<InterventionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static BehaviourIncidentService CreateService(InterventionDbContext db, DateOnly? today = null)
        => new(db, new WorkflowEngine(), new FakeClock(today ?? new DateOnly(2026, 8, 5)));

    [Fact]
    public async Task RecordAsync_creates_a_Draft_incident()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        var incidentId = await service.RecordAsync(
            studentId, Guid.NewGuid(), null, Guid.NewGuid(), "Disrupted class during maths.", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 5));

        var incident = await db.BehaviourIncidents.SingleAsync(i => i.Id == incidentId);
        Assert.Equal(studentId, incident.StudentPersonId);
        Assert.Equal(WorkflowStatus.Draft, incident.Status);
        Assert.Equal("RESTRICTED", incident.ConfidentialityTierCode);
    }

    [Fact]
    public async Task Full_pipeline_from_submit_to_approve_reaches_Approved_and_records_action_taken()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var incidentId = await service.RecordAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "Disrupted class.", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 5));

        await service.SubmitAsync(incidentId);
        await service.BeginReviewAsync(incidentId, Guid.NewGuid());
        await service.ApproveAsync(incidentId, Guid.NewGuid(), "Discussed with student.", "Confirmed by two witnesses.");

        var incident = await db.BehaviourIncidents.SingleAsync(i => i.Id == incidentId);
        Assert.Equal(WorkflowStatus.Approved, incident.Status);
        Assert.Equal("Discussed with student.", incident.ActionTaken);
        Assert.Equal("Confirmed by two witnesses.", incident.ReviewNotes);
        Assert.NotNull(incident.DecidedAtUtc);
    }

    [Fact]
    public async Task RejectAsync_and_ReturnAsync_move_to_the_expected_terminal_or_correctable_state()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var rejectedId = await service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "x", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 5));
        await service.SubmitAsync(rejectedId);
        await service.BeginReviewAsync(rejectedId, Guid.NewGuid());
        await service.RejectAsync(rejectedId, Guid.NewGuid(), "Unsubstantiated.");
        Assert.Equal(WorkflowStatus.Rejected, (await db.BehaviourIncidents.SingleAsync(i => i.Id == rejectedId)).Status);

        var returnedId = await service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "x", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 5));
        await service.SubmitAsync(returnedId);
        await service.BeginReviewAsync(returnedId, Guid.NewGuid());
        await service.ReturnAsync(returnedId, Guid.NewGuid(), "Add witness names.");
        Assert.Equal(WorkflowStatus.Returned, (await db.BehaviourIncidents.SingleAsync(i => i.Id == returnedId)).Status);

        await service.SubmitAsync(returnedId);
        Assert.Equal(WorkflowStatus.Submitted, (await db.BehaviourIncidents.SingleAsync(i => i.Id == returnedId)).Status);
    }

    [Fact]
    public async Task ApproveAsync_rejects_an_illegal_transition_from_Draft()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var incidentId = await service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "x", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 5));

        await Assert.ThrowsAsync<InvalidWorkflowTransitionException>(() => service.ApproveAsync(incidentId, Guid.NewGuid(), null, null));
    }

    [Fact]
    public async Task GetForStudentAsync_returns_only_that_students_incidents_ordered_by_date_descending()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        var earlyId = await service.RecordAsync(studentId, Guid.NewGuid(), null, Guid.NewGuid(), "Early", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 1));
        var lateId = await service.RecordAsync(studentId, Guid.NewGuid(), null, Guid.NewGuid(), "Late", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 4));
        await service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "Other student", "RESTRICTED", Guid.NewGuid(), new DateOnly(2026, 8, 3));

        var result = await service.GetForStudentAsync(studentId);

        Assert.Equal([lateId, earlyId], result.Select(i => i.Id));
    }
}

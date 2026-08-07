using HAMS.Intervention.Domain;
using HAMS.Intervention.Infrastructure;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Application;

internal sealed class BehaviourIncidentService(InterventionDbContext dbContext, IWorkflowEngine workflowEngine, IClock clock)
    : IBehaviourIncidentService
{
    public async Task<Guid> RecordAsync(
        Guid studentPersonId, Guid behaviourCategoryId, Guid? subjectId, Guid academicYearId, string description,
        string confidentialityTierCode, Guid recordedByPersonId, DateOnly occurredDate, CancellationToken cancellationToken = default)
    {
        var incident = new BehaviourIncident
        {
            Id = Guid.NewGuid(), StudentPersonId = studentPersonId, BehaviourCategoryId = behaviourCategoryId, SubjectId = subjectId,
            AcademicYearId = academicYearId, Description = description, ConfidentialityTierCode = confidentialityTierCode,
            RecordedByPersonId = recordedByPersonId, OccurredDate = occurredDate, CreatedAtUtc = clock.UtcNow,
        };
        dbContext.BehaviourIncidents.Add(incident);
        await dbContext.SaveChangesAsync(cancellationToken);

        return incident.Id;
    }

    public async Task SubmitAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = await GetRequiredAsync(incidentId, cancellationToken);
        incident.Status = workflowEngine.Transition(incident.Status, WorkflowAction.Submit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginReviewAsync(Guid incidentId, Guid reviewedByPersonId, CancellationToken cancellationToken = default)
    {
        var incident = await GetRequiredAsync(incidentId, cancellationToken);
        incident.Status = workflowEngine.Transition(incident.Status, WorkflowAction.Review);
        incident.ReviewedByPersonId = reviewedByPersonId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid incidentId, Guid reviewedByPersonId, string? actionTaken, string? reviewNotes, CancellationToken cancellationToken = default)
    {
        var incident = await GetRequiredAsync(incidentId, cancellationToken);
        incident.Status = workflowEngine.Transition(incident.Status, WorkflowAction.Approve);
        incident.ReviewedByPersonId = reviewedByPersonId;
        incident.ActionTaken = actionTaken;
        incident.ReviewNotes = reviewNotes;
        incident.DecidedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid incidentId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default)
    {
        var incident = await GetRequiredAsync(incidentId, cancellationToken);
        incident.Status = workflowEngine.Transition(incident.Status, WorkflowAction.Reject);
        incident.ReviewedByPersonId = reviewedByPersonId;
        incident.ReviewNotes = reviewNotes;
        incident.DecidedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReturnAsync(Guid incidentId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default)
    {
        var incident = await GetRequiredAsync(incidentId, cancellationToken);
        incident.Status = workflowEngine.Transition(incident.Status, WorkflowAction.Return);
        incident.ReviewedByPersonId = reviewedByPersonId;
        incident.ReviewNotes = reviewNotes;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<BehaviourIncident?> GetAsync(Guid incidentId, CancellationToken cancellationToken = default)
        => await dbContext.BehaviourIncidents.FindAsync([incidentId], cancellationToken);

    public async Task<IReadOnlyList<BehaviourIncident>> GetForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => await dbContext.BehaviourIncidents
            .Where(i => i.StudentPersonId == studentPersonId)
            .OrderByDescending(i => i.OccurredDate)
            .ToListAsync(cancellationToken);

    private async Task<BehaviourIncident> GetRequiredAsync(Guid incidentId, CancellationToken cancellationToken)
        => await dbContext.BehaviourIncidents.FindAsync([incidentId], cancellationToken)
            ?? throw new InvalidOperationException("Behaviour incident not found.");
}

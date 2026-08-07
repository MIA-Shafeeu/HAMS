using HAMS.Intervention.Domain;
using HAMS.Intervention.Infrastructure;
using HAMS.LearningDelivery.Application;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Application;

internal sealed class TopicClosureService(
    InterventionDbContext dbContext, IWorkflowEngine workflowEngine, ITeachingTopicQuery teachingTopicQuery, IClock clock)
    : ITopicClosureService
{
    public async Task<Guid> RequestClosureAsync(Guid teachingTopicId, Guid requestedByPersonId, CancellationToken cancellationToken = default)
    {
        var closure = new TopicClosure
        {
            Id = Guid.NewGuid(),
            TeachingTopicId = teachingTopicId,
            RequestedByPersonId = requestedByPersonId,
            CreatedAtUtc = clock.UtcNow,
        };
        dbContext.TopicClosures.Add(closure);
        await dbContext.SaveChangesAsync(cancellationToken);

        return closure.Id;
    }

    public async Task SubmitAsync(Guid topicClosureId, CancellationToken cancellationToken = default)
    {
        var closure = await GetRequiredAsync(topicClosureId, cancellationToken);
        closure.Status = workflowEngine.Transition(closure.Status, WorkflowAction.Submit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginReviewAsync(Guid topicClosureId, Guid reviewedByPersonId, CancellationToken cancellationToken = default)
    {
        var closure = await GetRequiredAsync(topicClosureId, cancellationToken);
        closure.Status = workflowEngine.Transition(closure.Status, WorkflowAction.Review);
        closure.ReviewedByPersonId = reviewedByPersonId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(
        Guid topicClosureId, Guid reviewedByPersonId, string? reviewNotes, IReadOnlyCollection<Guid> studentPersonIdsWithGaps,
        CancellationToken cancellationToken = default)
    {
        var closure = await GetRequiredAsync(topicClosureId, cancellationToken);
        closure.Status = workflowEngine.Transition(closure.Status, WorkflowAction.Approve);
        closure.ReviewedByPersonId = reviewedByPersonId;
        closure.ReviewNotes = reviewNotes;
        closure.DecidedAtUtc = clock.UtcNow;

        if (studentPersonIdsWithGaps.Count > 0)
        {
            var learningOutcomeId = await teachingTopicQuery.GetLearningOutcomeIdAsync(closure.TeachingTopicId, cancellationToken)
                ?? throw new InvalidOperationException("The teaching topic's learning outcome could not be resolved.");

            var identifiedDate = clock.TodayUtc;
            foreach (var studentPersonId in studentPersonIdsWithGaps)
            {
                dbContext.CarriedForwardGaps.Add(new CarriedForwardGap
                {
                    Id = Guid.NewGuid(),
                    StudentPersonId = studentPersonId,
                    LearningOutcomeId = learningOutcomeId,
                    TopicClosureId = closure.Id,
                    IdentifiedDate = identifiedDate,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid topicClosureId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default)
    {
        var closure = await GetRequiredAsync(topicClosureId, cancellationToken);
        closure.Status = workflowEngine.Transition(closure.Status, WorkflowAction.Reject);
        closure.ReviewedByPersonId = reviewedByPersonId;
        closure.ReviewNotes = reviewNotes;
        closure.DecidedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReturnAsync(Guid topicClosureId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default)
    {
        var closure = await GetRequiredAsync(topicClosureId, cancellationToken);
        closure.Status = workflowEngine.Transition(closure.Status, WorkflowAction.Return);
        closure.ReviewedByPersonId = reviewedByPersonId;
        closure.ReviewNotes = reviewNotes;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TopicClosure?> GetCurrentAsync(Guid teachingTopicId, CancellationToken cancellationToken = default)
        => await dbContext.TopicClosures
            .Where(c => c.TeachingTopicId == teachingTopicId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<TopicClosure> GetRequiredAsync(Guid topicClosureId, CancellationToken cancellationToken)
        => await dbContext.TopicClosures.FindAsync([topicClosureId], cancellationToken)
            ?? throw new InvalidOperationException("Topic closure not found.");
}

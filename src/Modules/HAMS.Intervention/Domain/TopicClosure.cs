using HAMS.Platform.Workflow.Domain;

namespace HAMS.Intervention.Domain;

/// <summary>
/// The topic closure workflow (build plan Phase 9 scope: "topic closure workflow") — the SECOND
/// real consumer of <c>Platform.Workflow</c>'s shared <c>IWorkflowEngine</c> (the first was Phase
/// 7's assessment moderation), confirming the kernel's one hardcoded transition table genuinely
/// generalizes across consumers rather than being assessment-specific. A teacher requests closure
/// of a <c>TeachingTopic</c> (LearningDelivery) once they consider it fully taught; a reviewer
/// (e.g. a subject/phase coordinator) approves, rejects, or returns it for correction — the exact
/// same Draft→Submitted→UnderReview→Approved/Rejected/Returned pipeline as assessment moderation,
/// reused as-is with zero changes to the shared kernel.
/// </summary>
public sealed class TopicClosure
{
    public Guid Id { get; init; }

    public Guid TeachingTopicId { get; init; }

    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    public Guid RequestedByPersonId { get; init; }

    public Guid? ReviewedByPersonId { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? DecidedAtUtc { get; set; }
}

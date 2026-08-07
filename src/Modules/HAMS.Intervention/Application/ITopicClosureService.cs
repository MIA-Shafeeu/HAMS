using HAMS.Intervention.Domain;

namespace HAMS.Intervention.Application;

/// <summary>
/// The "topic closure workflow" named in the build plan's Phase 9 scope — a teacher requests
/// closing out a <c>TeachingTopic</c>, a reviewer approves or sends it back, reusing
/// <c>Platform.Workflow</c>'s Draft→Submitted→UnderReview→Approved/Rejected/Returned state machine
/// exactly as built for Phase 7's assessment moderation (this is the kernel's second real consumer
/// — zero kernel changes needed). Approval is also the one point a reviewer names the specific
/// students who still have a gap in this topic's outcome, producing <see cref="CarriedForwardGap"/>
/// rows — deliberately an explicit reviewer-supplied list, not an automated scan of a class roster's
/// mastery status (build plan §12's spirit: no invented automated judgment).
/// </summary>
public interface ITopicClosureService
{
    Task<Guid> RequestClosureAsync(Guid teachingTopicId, Guid requestedByPersonId, CancellationToken cancellationToken = default);

    Task SubmitAsync(Guid topicClosureId, CancellationToken cancellationToken = default);

    Task BeginReviewAsync(Guid topicClosureId, Guid reviewedByPersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves the closure and, for each student in <paramref name="studentPersonIdsWithGaps"/>,
    /// creates a <see cref="CarriedForwardGap"/> against the topic's own learning outcome (resolved
    /// via <c>ITeachingTopicQuery</c>).
    /// </summary>
    Task ApproveAsync(
        Guid topicClosureId, Guid reviewedByPersonId, string? reviewNotes, IReadOnlyCollection<Guid> studentPersonIdsWithGaps,
        CancellationToken cancellationToken = default);

    Task RejectAsync(Guid topicClosureId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default);

    Task ReturnAsync(Guid topicClosureId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default);

    /// <summary>The most recently requested closure for this topic, or null if none has ever been requested.</summary>
    Task<TopicClosure?> GetCurrentAsync(Guid teachingTopicId, CancellationToken cancellationToken = default);
}

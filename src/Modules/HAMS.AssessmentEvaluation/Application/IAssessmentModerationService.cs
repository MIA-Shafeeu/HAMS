using HAMS.AssessmentEvaluation.Domain;

namespace HAMS.AssessmentEvaluation.Application;

/// <summary>
/// The assessment-moderation pipeline named in the build plan as a <c>Platform.Workflow</c>
/// consumer: Submit → Review → Approve/Reject/Return, plugging <c>AssessmentResult.ModerationStatus</c>
/// into <c>IWorkflowEngine</c>. Reaching <c>Approve</c> is the one point that also flips the row's
/// structural <c>Status</c> to Published (see <see cref="AssessmentResult"/>'s remarks) —
/// afterwards, correcting the result requires <see cref="ReviseApprovedResultAsync"/>, not a
/// direct mutation.
/// </summary>
public interface IAssessmentModerationService
{
    /// <summary>Creates the first (Draft) attempt row. Exactly one of <paramref name="rawMark"/>/<paramref name="specialResultStateId"/> must be non-null.</summary>
    Task<Guid> RecordRawMarkAsync(
        Guid assessmentId, Guid studentPersonId, Guid keyStagePolicyId, decimal? rawMark, Guid? specialResultStateId,
        Guid recordedByPersonId, CancellationToken cancellationToken = default);

    /// <summary>Corrects the raw mark/special state — only while the result is still Draft or has been Returned for correction.</summary>
    Task ReviseRawMarkAsync(Guid assessmentResultId, decimal? rawMark, Guid? specialResultStateId, CancellationToken cancellationToken = default);

    Task SubmitAsync(Guid assessmentResultId, CancellationToken cancellationToken = default);

    /// <summary>Moves Submitted → UnderReview, optionally setting <c>AdjustedMark</c> (settable only once).</summary>
    Task BeginReviewAsync(Guid assessmentResultId, decimal? adjustedMark, CancellationToken cancellationToken = default);

    /// <summary>Moves UnderReview → Approved, optionally setting <c>ModeratedMark</c> (settable only once), then computes and stores <c>FinalMark</c> and flips <c>Status</c> to Published.</summary>
    Task ApproveAsync(Guid assessmentResultId, decimal? moderatedMark, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid assessmentResultId, CancellationToken cancellationToken = default);

    Task ReturnAsync(Guid assessmentResultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves UnderReview → Escalated (build plan Phase 13: "advanced moderation") — a reviewer sends
    /// a disputed or borderline result to a senior reviewer/administrator for a final decision. That
    /// final decision is <see cref="ApproveAsync"/>/<see cref="RejectAsync"/> themselves: both accept
    /// an Escalated result too (the shared <c>IWorkflowEngine</c> transition table resolves it), so
    /// no separate "decide an escalation" method exists.
    /// </summary>
    Task EscalateAsync(Guid assessmentResultId, Guid escalatedByPersonId, string escalationReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// The one sanctioned way to correct an already-Published/Locked result (build plan §3: "a
    /// generic CorrectionService&lt;T&gt; is the only code path allowed to touch a Published/Locked
    /// row — it inserts a new version and flips pointers") — supersedes <paramref name="assessmentResultId"/>
    /// with a new, already-Approved row carrying <paramref name="newFinalMark"/>.
    /// </summary>
    Task<Guid> ReviseApprovedResultAsync(Guid assessmentResultId, decimal newFinalMark, CancellationToken cancellationToken = default);

    /// <summary>Resolves a result by its own id — a Staff page re-authorizing a moderation transition against the caller's teaching scope needs this to find the result's owning <c>AssessmentId</c> (and from there its <c>GradeId</c>) before trusting a caller-posted <c>assessmentResultId</c>.</summary>
    Task<AssessmentResult?> GetAsync(Guid assessmentResultId, CancellationToken cancellationToken = default);
}

namespace HAMS.Platform.Workflow.Domain;

/// <summary>
/// A structural workflow state, not business/reference data — same exemption as
/// <c>RecordStatus</c>/<c>LessonSessionStatus</c>: "only an UnderReview row can be Approved" is a
/// code-branching rule regardless of storage, and extending this list always needs a new code path
/// (a new transition rule) anyway. Shared by every consumer that plugs into <see cref="Application.IWorkflowEngine"/>
/// (build plan §1.4: "hardcode the ~8 known pipelines against one shared state machine" — this is
/// that one shared state machine's vocabulary, not a per-consumer copy).
/// </summary>
public enum WorkflowStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Returned = 5,

    /// <summary>
    /// Sent up to a senior reviewer/administrator for a final, binding decision (build plan Phase
    /// 13 scope: "advanced moderation") — the first real consumer of the <see cref="WorkflowAction.Escalate"/>
    /// verb, deferred since Phase 7. Only <see cref="WorkflowAction.Approve"/>/<see cref="WorkflowAction.Reject"/>
    /// lead out of it, deliberately not <see cref="WorkflowAction.Return"/> — an escalation is meant
    /// to end in one senior decision, not open a third review round.
    /// </summary>
    Escalated = 6,
}

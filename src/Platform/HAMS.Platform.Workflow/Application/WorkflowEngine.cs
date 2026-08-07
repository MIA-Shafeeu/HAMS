using HAMS.Platform.Workflow.Domain;

namespace HAMS.Platform.Workflow.Application;

internal sealed class WorkflowEngine : IWorkflowEngine
{
    // The one hardcoded transition table every consumer shares (build plan §1.6) — a Returned
    // result can be corrected and resubmitted (Submit again), matching real moderation practice;
    // Rejected and Approved are terminal for this pipeline (a correction there is a new version of
    // the underlying record, per the versioned-record pattern, not a workflow transition). Escalated
    // (Phase 13) is reachable only from UnderReview, and itself only ever resolves via Approve/Reject
    // — never Return, deliberately (see WorkflowStatus.Escalated's own remarks) — reusing the exact
    // same Approve/Reject transitions' target states an ordinary UnderReview review already uses, so
    // every consumer's existing ApproveAsync/RejectAsync method needs zero new code to also finish
    // an escalated item.
    private static readonly Dictionary<(WorkflowStatus, WorkflowAction), WorkflowStatus> Transitions = new()
    {
        [(WorkflowStatus.Draft, WorkflowAction.Submit)] = WorkflowStatus.Submitted,
        [(WorkflowStatus.Submitted, WorkflowAction.Review)] = WorkflowStatus.UnderReview,
        [(WorkflowStatus.UnderReview, WorkflowAction.Approve)] = WorkflowStatus.Approved,
        [(WorkflowStatus.UnderReview, WorkflowAction.Reject)] = WorkflowStatus.Rejected,
        [(WorkflowStatus.UnderReview, WorkflowAction.Return)] = WorkflowStatus.Returned,
        [(WorkflowStatus.UnderReview, WorkflowAction.Escalate)] = WorkflowStatus.Escalated,
        [(WorkflowStatus.Escalated, WorkflowAction.Approve)] = WorkflowStatus.Approved,
        [(WorkflowStatus.Escalated, WorkflowAction.Reject)] = WorkflowStatus.Rejected,
        [(WorkflowStatus.Returned, WorkflowAction.Submit)] = WorkflowStatus.Submitted,
    };

    public WorkflowStatus Transition(WorkflowStatus current, WorkflowAction action)
        => Transitions.TryGetValue((current, action), out var next)
            ? next
            : throw new InvalidWorkflowTransitionException(current, action);
}

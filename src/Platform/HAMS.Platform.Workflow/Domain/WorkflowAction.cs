namespace HAMS.Platform.Workflow.Domain;

/// <summary>
/// The verbs a <see cref="Application.IWorkflowEngine"/> consumer submits to move a
/// <see cref="WorkflowStatus"/> forward — build plan §1.4 names Submit/Review/Approve/Reject/
/// Return/Escalate/Delegate as the full verb set this kernel should eventually support.
/// <see cref="Escalate"/> was added in Phase 13 ("advanced moderation") once assessment moderation
/// became the first real consumer that needed it — a disputed/borderline result under review can be
/// sent to a senior reviewer for a final decision. <b>Delegate remains unimplemented</b>: still no
/// consumer needs "hand this off to someone else" specifically, and building transition semantics
/// for a verb nothing uses yet would be exactly the speculative generality the plan itself warns
/// against (§1.6: "do not build a generic admin-configurable workflow designer"). Add it, with real
/// transition rules, the day a consumer actually needs it — not before.
/// </summary>
public enum WorkflowAction
{
    Submit,
    Review,
    Approve,
    Reject,
    Return,
    Escalate,
}

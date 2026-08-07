using HAMS.Platform.Workflow.Domain;

namespace HAMS.Platform.Workflow.Application;

/// <summary>
/// The one shared, hardcoded state machine every workflow consumer plugs into (build plan §1.4/
/// §1.6) — a consumer entity stores its own <see cref="WorkflowStatus"/> field (e.g.
/// <c>AssessmentResult.ModerationStatus</c>) and calls <see cref="Transition"/> to validate and
/// compute the next state, rather than each module hand-rolling its own transition rules.
/// Deliberately stateless and side-effect-free — persisting the new status, and anything else a
/// transition should trigger (an audit entry, a notification), is the consumer's job, not this
/// kernel's; this only answers "is this move legal, and if so, what's the resulting state?"
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Throws <see cref="InvalidWorkflowTransitionException"/> if <paramref name="action"/> is not
    /// legal from <paramref name="current"/>.
    /// </summary>
    WorkflowStatus Transition(WorkflowStatus current, WorkflowAction action);
}

public sealed class InvalidWorkflowTransitionException(WorkflowStatus current, WorkflowAction action)
    : InvalidOperationException($"Cannot apply '{action}' to a workflow currently in '{current}'.")
{
    public WorkflowStatus Current { get; } = current;
    public WorkflowAction Action { get; } = action;
}

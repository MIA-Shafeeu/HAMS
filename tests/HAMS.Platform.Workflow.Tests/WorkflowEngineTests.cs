using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;

namespace HAMS.Platform.Workflow.Tests;

public class WorkflowEngineTests
{
    private readonly WorkflowEngine _engine = new();

    [Theory]
    [InlineData(WorkflowStatus.Draft, WorkflowAction.Submit, WorkflowStatus.Submitted)]
    [InlineData(WorkflowStatus.Submitted, WorkflowAction.Review, WorkflowStatus.UnderReview)]
    [InlineData(WorkflowStatus.UnderReview, WorkflowAction.Approve, WorkflowStatus.Approved)]
    [InlineData(WorkflowStatus.UnderReview, WorkflowAction.Reject, WorkflowStatus.Rejected)]
    [InlineData(WorkflowStatus.UnderReview, WorkflowAction.Return, WorkflowStatus.Returned)]
    [InlineData(WorkflowStatus.Returned, WorkflowAction.Submit, WorkflowStatus.Submitted)]
    [InlineData(WorkflowStatus.UnderReview, WorkflowAction.Escalate, WorkflowStatus.Escalated)]
    [InlineData(WorkflowStatus.Escalated, WorkflowAction.Approve, WorkflowStatus.Approved)]
    [InlineData(WorkflowStatus.Escalated, WorkflowAction.Reject, WorkflowStatus.Rejected)]
    public void Transition_applies_every_legal_move(WorkflowStatus current, WorkflowAction action, WorkflowStatus expected)
    {
        var result = _engine.Transition(current, action);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(WorkflowStatus.Draft, WorkflowAction.Review)]
    [InlineData(WorkflowStatus.Draft, WorkflowAction.Approve)]
    [InlineData(WorkflowStatus.Submitted, WorkflowAction.Approve)]
    [InlineData(WorkflowStatus.Submitted, WorkflowAction.Submit)]
    [InlineData(WorkflowStatus.Approved, WorkflowAction.Submit)]
    [InlineData(WorkflowStatus.Approved, WorkflowAction.Review)]
    [InlineData(WorkflowStatus.Rejected, WorkflowAction.Submit)]
    [InlineData(WorkflowStatus.Rejected, WorkflowAction.Review)]
    [InlineData(WorkflowStatus.Returned, WorkflowAction.Review)]
    [InlineData(WorkflowStatus.Returned, WorkflowAction.Approve)]
    [InlineData(WorkflowStatus.Draft, WorkflowAction.Escalate)]
    [InlineData(WorkflowStatus.Submitted, WorkflowAction.Escalate)]
    [InlineData(WorkflowStatus.Escalated, WorkflowAction.Return)]
    [InlineData(WorkflowStatus.Escalated, WorkflowAction.Escalate)]
    [InlineData(WorkflowStatus.Approved, WorkflowAction.Escalate)]
    public void Transition_rejects_every_illegal_move(WorkflowStatus current, WorkflowAction action)
    {
        var exception = Assert.Throws<InvalidWorkflowTransitionException>(() => _engine.Transition(current, action));

        Assert.Equal(current, exception.Current);
        Assert.Equal(action, exception.Action);
    }
}

namespace HAMS.Intervention.Domain;

/// <summary>
/// A student who had not achieved a <see cref="TopicClosure"/>'s outcome by the time the topic was
/// approved-closed (build plan Phase 9 scope: "carry-forward gaps") — tracked so the gap isn't
/// silently lost once the class moves on to the next topic. <see cref="LearningOutcomeId"/> is
/// resolved from the closed topic's own <c>SchemeOfWorkItem</c>, not supplied by the caller — a
/// <c>TeachingTopic</c> maps to exactly one outcome via that chain.
///
/// <b>Deliberate scope-down, flagged rather than silently done</b>: which students have a gap is
/// explicitly listed by the reviewer approving the closure, not auto-detected by scanning every
/// enrolled student's mastery status — matches Phase 7's precedent of manually-applied special
/// result states rather than automated bulk inference for a judgement call a human is better
/// placed to make.
/// </summary>
public sealed class CarriedForwardGap
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid LearningOutcomeId { get; init; }

    public Guid TopicClosureId { get; init; }

    public Guid? InterventionCaseId { get; set; }

    public DateOnly IdentifiedDate { get; init; }

    public bool IsResolved { get; set; }
}

namespace HAMS.LearningDelivery.Domain;

/// <summary>A teacher's plan for delivering a <see cref="TeachingTopic"/> — may be delivered across more than one <see cref="LessonSession"/>.</summary>
public sealed class LessonPlan
{
    public Guid Id { get; init; }

    public Guid TeachingTopicId { get; init; }

    public Guid StaffPersonId { get; init; }

    public DateOnly PlannedDate { get; set; }

    public required string Objectives { get; set; }
}

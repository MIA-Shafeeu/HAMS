namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// One actual delivered (or planned/cancelled) occurrence of a <see cref="LessonPlan"/>, tied to a
/// specific class/date/period. <see cref="ClassId"/>/<see cref="PeriodId"/> are loose references
/// into TeachingTimetable's "teaching" schema.
/// </summary>
public sealed class LessonSession
{
    public Guid Id { get; init; }

    public Guid LessonPlanId { get; init; }

    public Guid ClassId { get; init; }

    public DateOnly ActualDate { get; init; }

    public Guid PeriodId { get; init; }

    public LessonSessionStatus Status { get; set; } = LessonSessionStatus.Planned;
}

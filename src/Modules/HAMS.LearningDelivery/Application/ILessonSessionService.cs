namespace HAMS.LearningDelivery.Application;

/// <summary>Schedules and closes out <c>LessonSession</c> occurrences of a <c>LessonPlan</c>.</summary>
public interface ILessonSessionService
{
    Task<Guid> ScheduleAsync(Guid lessonPlanId, Guid classId, DateOnly actualDate, Guid periodId, CancellationToken cancellationToken = default);

    /// <summary>Marks the session Completed and records which outcomes it actually covered — coverage only counts once a session reaches this state (LES-FR-012).</summary>
    Task CompleteAsync(Guid lessonSessionId, IReadOnlyList<Guid> coveredOutcomeIds, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid lessonSessionId, CancellationToken cancellationToken = default);
}

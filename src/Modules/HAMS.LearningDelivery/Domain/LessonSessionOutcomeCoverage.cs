namespace HAMS.LearningDelivery.Domain;

/// <summary>Records that a specific outcome was actually covered in a <see cref="LessonSession"/> — only meaningful once that session's <c>Status</c> is <see cref="LessonSessionStatus.Completed"/> (LES-FR-012).</summary>
public sealed class LessonSessionOutcomeCoverage
{
    public Guid Id { get; init; }

    public Guid LessonSessionId { get; init; }

    public Guid LearningOutcomeId { get; init; }
}

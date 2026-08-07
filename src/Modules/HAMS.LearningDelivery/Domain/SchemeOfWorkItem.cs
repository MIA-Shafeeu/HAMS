namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// One planned outcome within a <see cref="SchemeOfWork"/> — <see cref="LearningOutcomeId"/> is a
/// loose reference to the exact <c>LearningOutcome</c> row (OrgCurriculum's "org" schema, a
/// specific <c>Syllabus</c> revision's tree) this item plans to cover, and when — the basis for
/// coverage comparison against what <see cref="LessonSessionOutcomeCoverage"/> later records as
/// actually delivered.
/// </summary>
public sealed class SchemeOfWorkItem
{
    public Guid Id { get; init; }

    public Guid SchemeOfWorkId { get; init; }

    public Guid LearningOutcomeId { get; init; }

    public int PlannedWeekNumber { get; set; }

    public int DisplayOrder { get; set; }
}

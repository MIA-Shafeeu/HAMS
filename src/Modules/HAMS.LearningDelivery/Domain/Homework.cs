namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A homework/assignment set for a class in a subject (build plan Phase 13 scope, 7.17). Deliberately
/// not tied to <see cref="IVersionedRecord{TKey}"/> lineage — correcting instructions in place is fine,
/// this isn't a judgement or result record per the build plan §3. <see cref="TeachingTopicId"/> is a
/// loose, optional link (not every assignment maps cleanly onto one curriculum topic, e.g. a
/// whole-term revision worksheet) — nullable rather than required, unlike <see cref="Resource"/>'s
/// mandatory link. <see cref="ClassId"/>/<see cref="SubjectId"/> are loose references into
/// TeachingTimetable's/OrgCurriculum's own schemas, the established cross-module reference pattern.
/// </summary>
public sealed class Homework
{
    public Guid Id { get; init; }

    public Guid ClassId { get; init; }

    public Guid SubjectId { get; init; }

    public Guid? TeachingTopicId { get; init; }

    public required string TitleEn { get; set; }

    public required string TitleDv { get; set; }

    public required string InstructionsEn { get; set; }

    public required string InstructionsDv { get; set; }

    public DateOnly AssignedDate { get; init; }

    public DateOnly DueDate { get; set; }

    /// <summary>Null when this assignment is feedback-only (no numeric mark), e.g. a reading task.</summary>
    public int? MaxScore { get; set; }

    public Guid AssignedByPersonId { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}

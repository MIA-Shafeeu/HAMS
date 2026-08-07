namespace HAMS.OrgCurriculum.Domain;

/// <summary>Bottom of the syllabus content tree — the most granular assessable criterion under a <see cref="LearningOutcome"/>.</summary>
public sealed class Indicator
{
    public Guid Id { get; init; }

    public Guid LearningOutcomeId { get; init; }

    public required string Code { get; init; }

    public required string Description { get; set; }

    public int DisplayOrder { get; set; }
}

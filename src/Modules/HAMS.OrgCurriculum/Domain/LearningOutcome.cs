namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// A gradable learning outcome under a <see cref="SubStrand"/>. Every downstream evidence/
/// mastery/evaluation record (later phases) FKs directly to a specific outcome's <see cref="Id"/>
/// — never to a "current outcome for this strand" lookup — so historical records survive a
/// syllabus revision unchanged (build plan §3).
/// </summary>
public sealed class LearningOutcome
{
    public Guid Id { get; init; }

    public Guid SubStrandId { get; init; }

    public required string Code { get; init; }

    public required string Description { get; set; }

    public int DisplayOrder { get; set; }
}

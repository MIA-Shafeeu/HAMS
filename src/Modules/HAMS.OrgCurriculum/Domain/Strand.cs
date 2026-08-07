namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// Top of the syllabus content tree (build plan §3): <c>Syllabus → Strand → SubStrand →
/// LearningOutcome → Indicator</c>. Belongs to exactly one <see cref="Syllabus"/> revision — never
/// shared across revisions — since the whole subtree is cloned wholesale on publish.
/// </summary>
public sealed class Strand
{
    public Guid Id { get; init; }

    public Guid SyllabusId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }
}

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// The top of the curriculum hierarchy (build plan §3): <c>CurriculumFramework → LearningArea →
/// Subject → Syllabus → Strand → SubStrand → LearningOutcome → Indicator</c>. One row per
/// framework revision a school operates under, e.g. "National Curriculum Framework 2015" — a
/// school realistically has one active framework at a time, but this is kept as its own entity
/// (rather than assumed implicitly) since a framework revision is a real, dated event upstream of
/// every <see cref="LearningArea"/>/<see cref="Subject"/> it introduces.
/// </summary>
public sealed class CurriculumFramework
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

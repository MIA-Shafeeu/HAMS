namespace HAMS.OrgCurriculum.Domain;

/// <summary>Many-to-many join required for combined classes (ORG-FR-018, build plan §3).</summary>
public sealed class ClassGrade
{
    public Guid Id { get; init; }

    public Guid ClassId { get; init; }

    public Guid GradeId { get; init; }
}

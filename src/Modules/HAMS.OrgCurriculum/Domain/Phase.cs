namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// Groups one or two <see cref="KeyStage"/>s (build plan §3: Foundation Phase = Foundation Key
/// Stage only; Primary = KS1+KS2; Lower Secondary = KS3+KS4; Higher Secondary = KS5 alone, per the
/// real National Curriculum Framework) — several real Ministry policies are set at this level and
/// inherited down, not configured per key stage. School-configurable, not hardcoded (ORG-FR-009).
/// </summary>
public sealed class Phase
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

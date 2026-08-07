namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// School-configurable boundaries/names/count (ORG-FR-009/010/011) — the level at which
/// <see cref="KeyStagePolicy"/> selects an evaluation model. Never carries its own evaluation
/// configuration; that only ever lives on <see cref="KeyStagePolicy"/> (build plan §3).
/// </summary>
public sealed class KeyStage
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public Guid PhaseId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

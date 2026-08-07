using HAMS.Platform.Common.Contracts;

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// The Mastery/Assessment/Hybrid selector (build plan §1.6) — deliberately a DB-backed lookup
/// entity, not an enum, even though only three rows exist today, so a future evaluation
/// philosophy never requires a code change to introduce.
/// </summary>
public sealed class EvaluationModel : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class EvaluationModelCodes
{
    public const string Mastery = "MASTERY";
    public const string Assessment = "ASSESSMENT";
    public const string Hybrid = "HYBRID";
}

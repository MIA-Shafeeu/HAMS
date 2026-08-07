using HAMS.Platform.Common.Contracts;

namespace HAMS.Intervention.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6 explicitly names "BehaviourCategory" as an example), not an
/// enum — what a school tracks as a behaviour concern or commendation changes over time and varies
/// by school. <see cref="IsPositive"/> distinguishes commendation-type categories (e.g. Merit) from
/// concern-type ones (e.g. Disruption) — "behaviour/pastoral" tracking covers both, not just discipline.
/// </summary>
public sealed class BehaviourCategory : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public bool IsPositive { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class BehaviourCategoryCodes
{
    public const string Merit = "MERIT";
    public const string Recognition = "RECOGNITION";
    public const string Disruption = "DISRUPTION";
    public const string Disrespect = "DISRESPECT";
    public const string Bullying = "BULLYING";
    public const string Other = "OTHER";
}

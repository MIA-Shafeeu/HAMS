using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Access.Domain;

/// <summary>
/// A configurable confidentiality tier (build plan §1.6/§4), e.g. Standard/Restricted/Safeguarding.
/// <see cref="Rank"/> orders tiers so a grant at a higher tier is understood to cover every lower
/// tier too (a "Safeguarding" grant also covers "Restricted" data) — comparisons are always by
/// rank, never by comparing <see cref="Code"/> strings.
/// </summary>
public sealed class ConfidentialityTier : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Higher rank = more restricted. Used for "does this grant's tier cover the required tier" comparisons.</summary>
    public int Rank { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class ConfidentialityTierCodes
{
    public const string Restricted = "RESTRICTED";
    public const string Safeguarding = "SAFEGUARDING";
}

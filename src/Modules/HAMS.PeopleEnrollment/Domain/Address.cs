namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// A Maldivian postal address — an EF Core owned type (no identity of its own; it lives entirely
/// inside its owner's row). Structured per the user's explicit requirements: island and atoll are
/// looked up from the DB-backed <see cref="Island"/>/<see cref="Atoll"/> lists (atoll is resolved
/// via <see cref="Island.AtollId"/> rather than stored redundantly here, to avoid the two ever
/// disagreeing); road and house name are bilingual, matching every other admin-facing free-text
/// field in the system; building/floor/apartment are optional, for the minority of addresses
/// (mainly Malé-style apartment blocks) that need them.
/// </summary>
public sealed class Address
{
    /// <summary>The atoll is always resolvable via <c>Island.AtollId</c> — not stored redundantly here.</summary>
    public Guid IslandId { get; set; }

    public required string RoadEn { get; set; }
    public required string RoadDv { get; set; }

    public required string HouseNameEn { get; set; }
    public required string HouseNameDv { get; set; }

    public string? BuildingEn { get; set; }
    public string? BuildingDv { get; set; }

    public string? Floor { get; set; }
    public string? Apartment { get; set; }
}

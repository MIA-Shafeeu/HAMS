namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// One inhabited island within an <see cref="Atoll"/> — configurable lookup (build plan §1.6).
/// Only a small seed set ships with the system (the school's own island, at minimum); schools add
/// the rest as needed for guardian/staff addresses rather than the system pre-populating every one
/// of the Maldives' ~200 inhabited islands upfront.
/// </summary>
public sealed class Island
{
    public Guid Id { get; init; }

    public Guid AtollId { get; init; }

    public required string Code { get; init; }

    public required string NameEn { get; set; }

    public string? NameDv { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

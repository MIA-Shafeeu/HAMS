namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// One of the Maldives' administrative atolls — a configurable lookup (build plan §1.6), not an
/// enum, per the user's explicit instruction that the atoll/island list "must be a list contained
/// inside the database." <see cref="Code"/> is the standard single/double-letter administrative
/// code (e.g. "TH" for Thaa) used on Maldivian ID cards, vehicle plates, and addresses. Seeded
/// with the real 20 administrative atolls; <see cref="NameDv"/> is deliberately left for an
/// admin fluent in Dhivehi to fill in rather than guessed here (see seed-data remarks).
/// </summary>
public sealed class Atoll
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string NameEn { get; set; }

    public string? NameDv { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

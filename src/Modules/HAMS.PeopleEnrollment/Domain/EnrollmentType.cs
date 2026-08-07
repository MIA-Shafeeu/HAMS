using HAMS.Platform.Common.Contracts;

namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6), not an enum. Only <see cref="EnrollmentTypeCodes.Ordinary"/>
/// is seeded — the one value ORG-FR-017's "one active ordinary class per grade/year" rule cares
/// about; schools add further categories (e.g. repeating, auditing) as a real need for them arises.
/// </summary>
public sealed class EnrollmentType : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class EnrollmentTypeCodes
{
    public const string Ordinary = "ORDINARY";
}

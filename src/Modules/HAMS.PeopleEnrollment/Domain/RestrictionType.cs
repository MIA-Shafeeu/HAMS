using HAMS.Platform.Common.Contracts;

namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// A configurable legal/safeguarding restriction that can apply to a
/// <see cref="GuardianStudentRelationship"/> (e.g. a court order) — a lookup, not an enum, since
/// schools encounter case-specific restriction categories that shouldn't require a code change to
/// record. Null on the relationship means no restriction; this table's rows are never used to
/// imply "no restriction" themselves.
/// </summary>
public sealed class RestrictionType : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

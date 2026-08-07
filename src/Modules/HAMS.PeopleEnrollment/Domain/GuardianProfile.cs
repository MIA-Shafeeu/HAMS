namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// Attaches guardian-specific fields to a <see cref="Person"/>. Deliberately minimal for now —
/// the relationship-specific detail (legal authority, permissions, verification, restrictions)
/// lives on <see cref="GuardianStudentRelationship"/>, not here, since one guardian can have
/// different standing with different students (e.g. legal authority over one child but not a
/// step-child).
/// </summary>
public sealed class GuardianProfile
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }
}

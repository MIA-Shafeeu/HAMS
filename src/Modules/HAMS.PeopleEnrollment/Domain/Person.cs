namespace HAMS.PeopleEnrollment.Domain;

/// <summary>
/// One row per human regardless of role (build plan §3) — the identity that
/// <c>UserAccount</c> (IdentityAccess), <see cref="StudentProfile"/>, <see cref="StaffProfile"/>
/// and <see cref="GuardianProfile"/> all attach to, and that <c>PersonRoleAssignment</c>/
/// <c>AccessGrant</c> (Platform.Access) key off. A single person may simultaneously hold more than
/// one profile (e.g. a staff member who is also a parent of an enrolled student).
///
/// Name and address are bilingual per the user's explicit requirement: Dhivehi is the official
/// national language and appears on legal/ID documents, English is needed for external exam
/// boards and international correspondence — school records need both, not one with the other as
/// an afterthought.
/// </summary>
public sealed class Person
{
    public Guid Id { get; init; }

    public required string NameEn { get; set; }

    public required string NameDv { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public Address Address { get; set; } = null!;

    /// <summary>Optional — not every <see cref="Person"/> row needs contact info immediately, but a guardian who is to <see cref="GuardianStudentRelationship.CanReceiveNotifications"/> needs at least one of these.</summary>
    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;
}

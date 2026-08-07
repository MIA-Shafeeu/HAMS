using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Access.Domain;

/// <summary>
/// One person holding one <see cref="Role"/> over a period of time. Effective-dated so temporary
/// role membership expires automatically (IAM-FR-015/BR-017) — no scheduled revocation job is
/// required for the access itself to disappear, only for UX "ending soon" notices.
/// <see cref="AccessGrantProjectionService"/> projects each active row here into an
/// <see cref="AccessGrant"/>.
/// </summary>
/// <remarks>
/// <see cref="PersonId"/> is a loose reference: <c>Person</c> lives in the PeopleEnrollment
/// module (a later phase), and modules never take a hard FK across schema boundaries (build plan
/// §2). A bootstrap System Administrator can therefore exist before a real <c>Person</c> row
/// does — Phase 3 is what makes <see cref="PersonId"/> resolve to a real profile.
/// </remarks>
public sealed class PersonRoleAssignment : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    public Guid RoleId { get; init; }

    /// <summary>Null = this role applies across every school (e.g. System Administrator).</summary>
    public Guid? SchoolId { get; init; }

    public DateOnly EffectiveFrom { get; init; }

    public DateOnly? EffectiveTo { get; set; }
}

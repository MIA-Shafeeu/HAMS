using HAMS.Platform.Access.Domain;

namespace HAMS.Platform.Access;

/// <summary>
/// The entry point business modules use to grant/revoke a <c>Role</c> to a person. Wraps the
/// <c>PersonRoleAssignment</c> write and its <see cref="IAccessGrantProjectionService"/>
/// projection in a single <c>SaveChanges</c> call so both land in one transaction.
/// </summary>
public interface IPersonRoleAssignmentService
{
    /// <returns>The new <c>PersonRoleAssignment</c>'s id.</returns>
    Task<Guid> AssignRoleAsync(
        Guid personId, string roleCode, Guid? schoolId, DateOnly effectiveFrom, DateOnly? effectiveTo,
        CancellationToken cancellationToken = default);

    Task RevokeRoleAsync(Guid personRoleAssignmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default);

    /// <summary>Every active <c>Role</c> — the role-assignment UI's picker list (build plan §1.6: a school can add roles beyond the seeded ~20, so this is never a fixed enum).</summary>
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every <c>Role</c>, active or not — the Reference Data admin screen's list, distinct from <see cref="GetRolesAsync"/>'s active-only picker list so a deactivated role stays visible (and reactivatable) there.</summary>
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every assignment (past and present) a person holds — a role-assignment UI's "current roles" list, the first read of its kind (every prior caller only ever wrote/revoked one already-known assignment).</summary>
    Task<IReadOnlyList<PersonRoleAssignment>> GetAssignmentsForPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Creates a new configurable <c>Role</c> (build plan §1.6 — an admin can add a role beyond the seeded set without a code change).</summary>
    /// <returns>The new <c>Role</c>'s id.</returns>
    Task<Guid> CreateRoleAsync(string code, string name, string? description, int displayOrder, CancellationToken cancellationToken = default);

    /// <summary>Activates or deactivates a <c>Role</c>. Throws <see cref="InvalidOperationException"/> if the role isn't found.</summary>
    Task SetRoleActiveAsync(Guid roleId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders a <c>Role</c> (build plan §1.6 — Name is admin-editable; Code stays fixed, application code branches on it). Throws <see cref="InvalidOperationException"/> if the role isn't found.</summary>
    Task UpdateRoleAsync(Guid roleId, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <summary>Creates a new configurable <c>ConfidentialityTier</c> (build plan §1.6/§4).</summary>
    /// <returns>The new <c>ConfidentialityTier</c>'s id.</returns>
    Task<Guid> CreateConfidentialityTierAsync(string code, string name, string? description, int rank, int displayOrder, CancellationToken cancellationToken = default);

    /// <summary>Every <c>ConfidentialityTier</c>, ordered by <see cref="ConfidentialityTier.DisplayOrder"/> — the confidentiality-tier admin UI's list.</summary>
    Task<IReadOnlyList<ConfidentialityTier>> GetConfidentialityTiersAsync(CancellationToken cancellationToken = default);

    /// <summary>Activates or deactivates a <c>ConfidentialityTier</c>. Throws <see cref="InvalidOperationException"/> if the tier isn't found.</summary>
    Task SetConfidentialityTierActiveAsync(Guid tierId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders/re-ranks a <c>ConfidentialityTier</c>. Throws <see cref="InvalidOperationException"/> if the tier isn't found.</summary>
    Task UpdateConfidentialityTierAsync(Guid tierId, string name, int rank, int displayOrder, CancellationToken cancellationToken = default);
}

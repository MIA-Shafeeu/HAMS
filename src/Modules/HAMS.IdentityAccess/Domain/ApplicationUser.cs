using Microsoft.AspNetCore.Identity;

namespace HAMS.IdentityAccess.Domain;

/// <summary>
/// A staff/admin login account, built on ASP.NET Core Identity (password hashing, lockout,
/// built-in TOTP token provider for MFA — build plan §5's "avoid standing up a full OIDC server"
/// choice). Deliberately uses <see cref="IdentityUser{TKey}"/> without Identity's own role system
/// — the configurable <c>Role</c> entity in Platform.Access is the one, real role model;
/// duplicating it with ASP.NET Core Identity roles would just create two competing sources of truth.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// The stable <c>Person</c> identity every <c>AccessGrant</c> row keys off. Loose reference —
    /// <c>Person</c> lives in the PeopleEnrollment module (a later phase); see
    /// <c>PersonRoleAssignment</c>'s remarks for why a bootstrap account can carry a
    /// <see cref="PersonId"/> before a real <c>Person</c> row exists.
    /// </summary>
    public required Guid PersonId { get; set; }

    public AccountStatus Status { get; set; } = AccountStatus.Active;
}

using HAMS.IdentityAccess.Domain;

namespace HAMS.IdentityAccess.Application.Auth;

/// <summary>A staff login account, joined with nothing beyond <see cref="ApplicationUser"/> itself — the UI resolves the person's name via <c>HAMS.PeopleEnrollment.IPeopleAdminService.GetStaffProfilesAsync</c> separately.</summary>
public sealed record StaffAccountSummary(Guid UserId, Guid PersonId, string UserName, string? Email, AccountStatus Status);

/// <summary>
/// Admin-driven staff account lifecycle — the one capability this codebase never had beyond the
/// dev-only <c>DevelopmentDataSeeder</c> bootstrap (which mints a brand-new <c>PersonId</c> for a
/// single hardcoded System Administrator). A real deployment needs an administrator to attach a
/// login to an <em>existing</em> <c>StaffProfile</c>'s <c>PersonId</c>, reset a forgotten password,
/// and disable an account when a staff member leaves — none of which existed anywhere before this.
/// </summary>
public interface IStaffAccountService
{
    /// <exception cref="InvalidOperationException">This person already has a login account, or account creation failed (e.g. weak password, duplicate username/email).</exception>
    Task<Guid> CreateAccountAsync(Guid personId, string userName, string? email, string initialPassword, CancellationToken cancellationToken = default);

    Task<StaffAccountSummary?> GetAccountByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffAccountSummary>> GetAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin-driven reset — deliberately not <c>UserManager.ChangePasswordAsync</c> (which requires
    /// knowing the current password): this is for the "staff member forgot their password" case,
    /// where only an administrator resetting it blind is possible.
    /// </summary>
    /// <exception cref="InvalidOperationException">No such account, or the new password fails Identity's configured policy.</exception>
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No such account.</exception>
    Task SetAccountStatusAsync(Guid userId, AccountStatus status, CancellationToken cancellationToken = default);
}

namespace HAMS.IdentityAccess.Application.Auth;

/// <summary>
/// A production-safe replacement for what <c>DevelopmentDataSeeder</c> does in Development —
/// creating the very first System Administrator account. Unlike the dev seeder (gated on
/// <c>IsDevelopment()</c>, so it can safely hardcode a known password), this is reachable in every
/// environment but permanently refuses once a System Administrator already exists anywhere,
/// checked live via <see cref="Platform.Access.IRoleMembershipQuery.AnyPersonHasRoleAsync"/> — not by
/// checking whether any <c>ApplicationUser</c> row exists at all, which a guardian/student's first
/// OTP/PIN login would already have created without ever bootstrapping a real admin.
/// </summary>
public interface ISetupService
{
    Task<bool> IsBootstrapNeededAsync(CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">A System Administrator already exists, or account creation failed (e.g. weak password).</exception>
    Task<Guid> BootstrapFirstAdminAsync(string userName, string password, CancellationToken cancellationToken = default);
}

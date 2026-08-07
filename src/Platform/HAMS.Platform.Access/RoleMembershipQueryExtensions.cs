using HAMS.Platform.Access.Domain;
using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Access;

/// <summary>
/// Shared convenience checks built on <see cref="IRoleMembershipQuery"/>, used by every module's
/// admin-only endpoints (build plan §4: always a live query, never the coarse JWT claim).
/// </summary>
public static class RoleMembershipQueryExtensions
{
    private static readonly string[] SystemOrSchoolAdminRoleCodes = [RoleCodes.SystemAdministrator, RoleCodes.SchoolAdministrator];

    public static Task<bool> IsSystemOrSchoolAdminAsync(
        this IRoleMembershipQuery query, ICurrentUser currentUser, IClock clock, CancellationToken cancellationToken = default)
        => currentUser.PersonId is { } personId
            ? query.IsSystemOrSchoolAdminAsync(personId, clock.TodayUtc, cancellationToken)
            : Task.FromResult(false);

    /// <summary>
    /// Same live check, taken directly by <see cref="Guid"/> rather than <see cref="ICurrentUser"/> —
    /// needed by Blazor Server interactive components (Phase 12), where <c>ICurrentUser</c>'s
    /// <c>IHttpContextAccessor</c> backing isn't reliably populated once a circuit is running (the
    /// standard Blazor guidance is to read the person id off the cascaded <c>AuthenticationState</c>
    /// instead). Still always a live query, never a cached/JWT-claim shortcut (build plan §4).
    /// </summary>
    public static Task<bool> IsSystemOrSchoolAdminAsync(
        this IRoleMembershipQuery query, Guid personId, DateOnly asOf, CancellationToken cancellationToken = default)
        => query.HasAnyRoleAsync(personId, SystemOrSchoolAdminRoleCodes, asOf, cancellationToken);
}

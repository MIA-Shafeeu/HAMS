namespace HAMS.Platform.Access;

/// <summary>
/// A small, read-only query surface other modules use instead of reaching into
/// <c>AccessDbContext</c> directly (build plan §2 module-boundary discipline). Currently used by
/// IdentityAccess to compute the coarse, UI-shell-only <c>IsSystemAdmin</c> JWT claim at issuance
/// time — never for an actual authorization decision (that always goes through
/// <c>ScopeAuthorizationHandler</c> against live data).
/// </summary>
public interface IRoleMembershipQuery
{
    Task<bool> HasRoleAsync(Guid personId, string roleCode, DateOnly asOf, CancellationToken cancellationToken = default);

    Task<bool> HasAnyRoleAsync(Guid personId, IReadOnlyCollection<string> roleCodes, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// "Does anyone at all currently hold this role" — not scoped to one person. The one-time
    /// production bootstrap endpoint (<c>SetupEndpoints</c>) uses this to permanently refuse once a
    /// real System Administrator exists, rather than checking a specific, not-yet-known personId.
    /// </summary>
    Task<bool> AnyPersonHasRoleAsync(string roleCode, DateOnly asOf, CancellationToken cancellationToken = default);
}

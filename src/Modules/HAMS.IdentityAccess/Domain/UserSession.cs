namespace HAMS.IdentityAccess.Domain;

/// <summary>
/// One issued refresh token / device session. Supports IAM-FR-013 ("view active sessions and
/// revoke selected or all sessions") and gives MAUI a device-bound refresh token to revoke on
/// logout/device loss (build plan §6). Only a hash of the refresh token is ever stored.
/// </summary>
public sealed class UserSession
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    /// <summary>
    /// Which login path authenticated this session — recorded once, here, at issuance, precisely
    /// because <see cref="IStaffAuthenticationService.RefreshAsync"/>/<c>LogoutAsync</c> are the one
    /// generic implementation every principal type's <c>/api/v1/auth/refresh</c> call funnels
    /// through: without this, a refreshed guardian/student token would silently come back
    /// staff-flagged (the exact bug this fixes — refresh previously hardcoded
    /// <c>isStaff: true, isGuardian: false, isStudent: false</c> unconditionally).
    /// </summary>
    public bool IsStaff { get; init; }
    public bool IsGuardian { get; init; }
    public bool IsStudent { get; init; }

    public required string RefreshTokenHash { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>User-supplied or client-reported label, e.g. "Chrome on Windows" / "Staff MAUI app".</summary>
    public string? DeviceLabel { get; init; }

    public string? IpAddress { get; init; }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;
}

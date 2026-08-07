namespace HAMS.IdentityAccess.Application.Auth;

public sealed record OtpRequestResult(bool Succeeded, Guid? ChallengeId, DateTimeOffset? ExpiresAtUtc, string? Error)
{
    public static OtpRequestResult Failed(string error) => new(false, null, null, error);

    public static OtpRequestResult Success(Guid challengeId, DateTimeOffset expiresAtUtc) => new(true, challengeId, expiresAtUtc, null);
}

/// <summary>
/// Guardian OTP login (build plan Phase 10) — proves control of a phone number already marked
/// <c>Verified</c> on at least one <c>GuardianStudentRelationship</c>, then converges on the same
/// <see cref="ITokenIssuer"/> every other principal type uses. See
/// <c>GuardianOtpChallenge</c>'s remarks for why this never itself verifies a relationship.
/// </summary>
public interface IGuardianAuthenticationService
{
    /// <summary>
    /// Sends a fresh 6-digit code, valid for 5 minutes, to <paramref name="phoneNumber"/> —
    /// synchronously, not via the notification outbox (see <c>GuardianAuthenticationService</c>'s
    /// remarks on why OTP is the one deliberate exception to that rule). Any prior unconsumed code
    /// for the same phone number is invalidated first.
    /// </summary>
    Task<OtpRequestResult> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the code, lazily provisioning the guardian's <c>ApplicationUser</c>/<c>Guardian</c>
    /// role assignment on first-ever successful login, and issues tokens.
    /// </summary>
    Task<AuthResult> VerifyOtpAsync(Guid challengeId, string code, string? deviceLabel, string? ipAddress, CancellationToken cancellationToken = default);
}

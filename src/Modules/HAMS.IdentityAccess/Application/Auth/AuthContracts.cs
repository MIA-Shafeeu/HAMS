namespace HAMS.IdentityAccess.Application.Auth;

public sealed record StaffLoginRequest(string UsernameOrEmail, string Password, string? DeviceLabel);

public sealed record StaffMfaVerifyRequest(string MfaToken, string Code, string? DeviceLabel);

public sealed record RefreshRequest(string RefreshToken);

public sealed record MfaSetupResult(string SharedKey, string AuthenticatorUri);

public sealed record MfaEnableRequest(string Code);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record SessionSummary(Guid Id, string? DeviceLabel, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, bool IsCurrent);

/// <summary>
/// Outcome of any login attempt — staff, guardian, or student, all converging on the same shape
/// (build plan §5). Exactly one of three cases: MFA required (staff only — client must call the
/// MFA-verify endpoint next), a hard failure, or a successful token issuance. <see cref="MfaRequired"/>/
/// <see cref="MfaToken"/> are always false/null for guardian/student results, since OTP/PIN login
/// has no separate MFA step of its own.
/// </summary>
public sealed class AuthResult
{
    public required bool Succeeded { get; init; }
    public bool MfaRequired { get; init; }
    public string? MfaToken { get; init; }
    public string? AccessToken { get; init; }
    public DateTimeOffset? AccessTokenExpiresAtUtc { get; init; }
    public string? RefreshToken { get; init; }
    public string? Error { get; init; }

    public static AuthResult Failed(string error) => new() { Succeeded = false, Error = error };

    public static AuthResult NeedsMfa(string mfaToken) =>
        new() { Succeeded = false, MfaRequired = true, MfaToken = mfaToken };

    public static AuthResult Success(string accessToken, DateTimeOffset expiresAtUtc, string refreshToken) =>
        new()
        {
            Succeeded = true,
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = expiresAtUtc,
            RefreshToken = refreshToken,
        };
}

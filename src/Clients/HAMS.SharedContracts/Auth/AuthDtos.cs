namespace HAMS.SharedContracts.Auth;

/// <summary>
/// Client-side mirrors of the request/response shapes exposed by
/// <c>HAMS.IdentityAccess.Endpoints.GuardianAuthEndpoints</c>/<c>StudentAuthEndpoints</c>/<c>AuthEndpoints</c>.
/// Defined independently here (rather than referenced from <c>HAMS.IdentityAccess</c>, a server-only
/// module with EF Core/ASP.NET Identity dependencies a WASM client can't take on) so both
/// <c>HAMS.WebHost.Client</c> and, later, the MAUI app can consume the same contract without depending
/// on server internals. Property names/casing match the server records exactly for JSON compatibility.
/// </summary>
public sealed record RequestGuardianOtpDto(string PhoneNumber);

public sealed record OtpRequestResultDto(bool Succeeded, Guid? ChallengeId, DateTimeOffset? ExpiresAtUtc, string? Error);

public sealed record VerifyGuardianOtpDto(Guid ChallengeId, string Code, string? DeviceLabel);

public sealed record StudentLoginDto(string AdmissionNumber, string Pin, string? DeviceLabel);

/// <summary>Mirrors <c>HAMS.IdentityAccess.Application.Auth.StaffLoginRequest</c> — the login shape <c>HAMS.Mobile</c> (Phase 14) uses, same as the existing staff web login form.</summary>
public sealed record StaffLoginDto(string UsernameOrEmail, string Password, string? DeviceLabel);

/// <summary>Mirrors <c>HAMS.IdentityAccess.Application.Auth.StaffMfaVerifyRequest</c>.</summary>
public sealed record StaffMfaVerifyDto(string MfaToken, string Code, string? DeviceLabel);

public sealed record RefreshRequestDto(string RefreshToken);

public sealed class AuthResultDto
{
    public bool Succeeded { get; init; }
    public bool MfaRequired { get; init; }
    public string? MfaToken { get; init; }
    public string? AccessToken { get; init; }
    public DateTimeOffset? AccessTokenExpiresAtUtc { get; init; }
    public string? RefreshToken { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Mirrors <c>HAMS.IdentityAccess.Application.Jwt.HamsClaimTypes</c> string-for-string — duplicated
/// here because that type lives in a server-only assembly the WASM client cannot reference. These are
/// stable claim-type string literals embedded in every access token; keep both copies in sync if they
/// ever change.
/// </summary>
public static class HamsClaimTypes
{
    public const string PersonId = "hams:person_id";
    public const string IsStaff = "hams:is_staff";
    public const string IsGuardian = "hams:is_guardian";
    public const string IsStudent = "hams:is_student";
    public const string IsSystemAdmin = "hams:is_system_admin";
}

namespace HAMS.IdentityAccess.Application.Auth;

public sealed record StudentLoginRequest(string AdmissionNumber, string Pin, string? DeviceLabel);

/// <summary>
/// Student ID+PIN login (build plan §5/Phase 10) — the admission number is the "ID," a school-set
/// numeric PIN is the "PIN." Deliberately not routed through <c>UserManager</c>'s password-set
/// APIs (<see cref="HAMS.IdentityAccess.IdentityAccessModule"/> configures a 10-character minimum
/// for staff passwords, which a PIN was never meant to satisfy) — see
/// <c>StudentAuthenticationService.SetPinAsync</c>'s remarks for exactly how a PIN is hashed
/// instead, while still verifying (and lockout-counting) it through the ordinary
/// <c>UserManager.CheckPasswordAsync</c> path.
/// </summary>
public interface IStudentAuthenticationService
{
    /// <summary>
    /// Sets (or resets) a student's PIN — a staff/admin action, gated at the endpoint, not here.
    /// Lazily provisions the student's <c>ApplicationUser</c>/<c>Student</c> role assignment on
    /// first-ever PIN set.
    /// </summary>
    Task SetPinAsync(Guid studentPersonId, string pin, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(StudentLoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
}

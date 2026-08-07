namespace HAMS.IdentityAccess.Application.Auth;

public interface IStaffAuthenticationService
{
    Task<AuthResult> LoginAsync(StaffLoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResult> VerifyMfaAsync(StaffMfaVerifyRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Revokes the session tied to the given refresh token (a no-op if already revoked/unknown).</summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionSummary>> ListSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);

    Task<MfaSetupResult> BeginMfaSetupAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <returns>False if the code didn't validate against the pending authenticator key.</returns>
    Task<bool> EnableMfaAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

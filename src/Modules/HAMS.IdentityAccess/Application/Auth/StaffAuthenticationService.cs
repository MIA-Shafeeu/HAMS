using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HAMS.IdentityAccess.Application.Auth;

internal sealed class StaffAuthenticationService(
    UserManager<ApplicationUser> userManager,
    IdentityAccessDbContext dbContext,
    IJwtTokenService jwtTokenService,
    ITokenIssuer tokenIssuer,
    IAuditLogWriter auditLogWriter,
    IClock clock)
    : IStaffAuthenticationService
{
    public async Task<AuthResult> LoginAsync(StaffLoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(request.UsernameOrEmail)
            ?? await userManager.FindByEmailAsync(request.UsernameOrEmail);

        if (user is null)
        {
            await LogFailedAttemptAsync(null, request.UsernameOrEmail, ipAddress, cancellationToken);
            return AuthResult.Failed("Invalid username or password.");
        }

        if (user.Status != AccountStatus.Active)
        {
            return AuthResult.Failed("This account is not active. Contact your school administrator.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthResult.Failed("This account is temporarily locked due to repeated failed sign-in attempts.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            await LogFailedAttemptAsync(user, request.UsernameOrEmail, ipAddress, cancellationToken);
            return AuthResult.Failed("Invalid username or password.");
        }

        // Deliberately NOT reset here when MFA is still pending: a known/guessed password would
        // otherwise let an attacker wipe the failed-attempt counter for free between TOTP guesses by
        // just calling LoginAsync again, defeating VerifyMfaAsync's own lockout entirely. Reset happens
        // once the login is actually complete — either right here (no MFA) or in VerifyMfaAsync on a
        // correct code.
        if (user.TwoFactorEnabled)
        {
            return AuthResult.NeedsMfa(jwtTokenService.IssueMfaChallengeToken(user.Id));
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await tokenIssuer.IssueAsync(user, isStaff: true, isGuardian: false, isStudent: false, request.DeviceLabel, ipAddress, cancellationToken);
    }

    public async Task<AuthResult> VerifyMfaAsync(StaffMfaVerifyRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (!jwtTokenService.TryValidateMfaChallengeToken(request.MfaToken, out var userId))
        {
            return AuthResult.Failed("MFA challenge has expired. Please sign in again.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.Status != AccountStatus.Active)
        {
            return AuthResult.Failed("This account is not active. Contact your school administrator.");
        }

        // Same lockout discipline as the password step (LoginAsync) — without this, a stolen/guessed
        // password lets an attacker brute-force the 6-digit TOTP code with unlimited attempts across
        // as many freshly-issued challenge tokens as they like.
        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthResult.Failed("This account is temporarily locked due to repeated failed sign-in attempts.");
        }

        if (!await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code))
        {
            await userManager.AccessFailedAsync(user);
            await LogFailedAttemptAsync(user, user.UserName ?? user.Id.ToString(), ipAddress, cancellationToken);
            return AuthResult.Failed("Invalid authentication code.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await tokenIssuer.IssueAsync(user, isStaff: true, isGuardian: false, isStudent: false, request.DeviceLabel, ipAddress, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        var session = await dbContext.UserSessions.SingleOrDefaultAsync(s => s.RefreshTokenHash == hash, cancellationToken);

        if (session is null || !session.IsActive(clock.UtcNow))
        {
            return AuthResult.Failed("Session has expired. Please sign in again.");
        }

        var user = await userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null || user.Status != AccountStatus.Active)
        {
            return AuthResult.Failed("This account is not active. Contact your school administrator.");
        }

        // Rotate: the presented refresh token is single-use.
        session.RevokedAtUtc = clock.UtcNow;

        // Re-issue as whichever principal type originally authenticated this session (recorded at
        // issuance) — NOT hardcoded staff, or a refreshed guardian/student token would silently
        // come back staff-flagged.
        return await tokenIssuer.IssueAsync(user, session.IsStaff, session.IsGuardian, session.IsStudent, session.DeviceLabel, ipAddress, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = jwtTokenService.HashRefreshToken(refreshToken);
        var session = await dbContext.UserSessions.SingleOrDefaultAsync(s => s.RefreshTokenHash == hash, cancellationToken);

        if (session is null || session.RevokedAtUtc is not null)
        {
            return;
        }

        session.RevokedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        // /logout is deliberately AllowAnonymous (a client should be able to sign out even with an
        // already-expired access token, needing only a still-valid refresh token) — so there's no
        // ICurrentUser here to read the actor from; the session row itself already resolves it.
        await auditLogWriter.WriteAsync(new AuditLogEntry
        {
            OccurredAtUtc = clock.UtcNow,
            Action = AuditAction.Logout,
            EntityType = nameof(UserSession),
            EntityId = session.Id.ToString(),
            ActorUserId = session.UserId,
            Summary = "Staff sign-out.",
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<SessionSummary>> ListSessionsAsync(Guid userId, string? currentRefreshToken, CancellationToken cancellationToken = default)
    {
        var currentHash = currentRefreshToken is null ? null : jwtTokenService.HashRefreshToken(currentRefreshToken);
        var now = clock.UtcNow;

        var sessions = await dbContext.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return sessions
            .Select(s => new SessionSummary(s.Id, s.DeviceLabel, s.CreatedAtUtc, s.ExpiresAtUtc, s.RefreshTokenHash == currentHash))
            .ToList();
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.UserSessions.SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Session not found.");

        session.RevokedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MfaSetupResult> BeginMfaSetupAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var label = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        var uri = $"otpauth://totp/HAMS:{label}?secret={key}&issuer=HAMS&digits=6";

        return new MfaSetupResult(key!, uri);
    }

    public async Task<bool> EnableMfaAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        if (!await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
        {
            return false;
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded;
    }

    private async Task LogFailedAttemptAsync(ApplicationUser? user, string attemptedUsername, string? ipAddress, CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogEntry
        {
            OccurredAtUtc = clock.UtcNow,
            Action = AuditAction.LoginFailed,
            EntityType = nameof(ApplicationUser),
            EntityId = user?.Id.ToString(),
            ActorPersonId = user?.PersonId,
            ActorUserId = user?.Id,
            Summary = $"Failed staff sign-in attempt: {attemptedUsername}.",
            IpAddress = ipAddress,
        }, cancellationToken);
    }
}

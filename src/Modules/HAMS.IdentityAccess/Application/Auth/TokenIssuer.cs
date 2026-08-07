using HAMS.IdentityAccess.Application.Jwt;
using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.Extensions.Options;

namespace HAMS.IdentityAccess.Application.Auth;

/// <summary>
/// The single token-issuance chokepoint every principal type (staff/guardian/student) funnels
/// through — build plan §5's "all converging on one JWT issuance service." Mints the access token,
/// generates+hashes+persists a refresh-token <see cref="UserSession"/>, and writes the audit login
/// record, exactly the same way regardless of which login path (password+MFA, OTP, ID+PIN) got the
/// caller here. Extracted from what was originally <c>StaffAuthenticationService</c>'s own private
/// method once <see cref="GuardianAuthenticationService"/>/<see cref="StudentAuthenticationService"/>
/// needed the identical sequence.
/// </summary>
internal interface ITokenIssuer
{
    Task<AuthResult> IssueAsync(
        ApplicationUser user, bool isStaff, bool isGuardian, bool isStudent, string? deviceLabel, string? ipAddress,
        CancellationToken cancellationToken = default);
}

internal sealed class TokenIssuer(
    IdentityAccessDbContext dbContext, IJwtTokenService jwtTokenService, IRoleMembershipQuery roleMembershipQuery,
    IAuditLogWriter auditLogWriter, IClock clock, IOptions<JwtOptions> jwtOptions)
    : ITokenIssuer
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResult> IssueAsync(
        ApplicationUser user, bool isStaff, bool isGuardian, bool isStudent, string? deviceLabel, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Only a staff principal can ever hold the System Administrator role — skip the query
        // entirely for guardian/student logins rather than asking a question that's always "no."
        var isSystemAdmin = isStaff
            && await roleMembershipQuery.HasRoleAsync(user.PersonId, RoleCodes.SystemAdministrator, clock.TodayUtc, cancellationToken);

        var (accessToken, expiresAtUtc) = jwtTokenService.IssueAccessToken(user, isStaff, isGuardian, isStudent, isSystemAdmin);

        var refreshToken = jwtTokenService.GenerateRefreshToken();
        dbContext.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IsStaff = isStaff,
            IsGuardian = isGuardian,
            IsStudent = isStudent,
            RefreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken),
            CreatedAtUtc = clock.UtcNow,
            ExpiresAtUtc = clock.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            DeviceLabel = deviceLabel,
            IpAddress = ipAddress,
        });

        await auditLogWriter.WriteAsync(new AuditLogEntry
        {
            OccurredAtUtc = clock.UtcNow,
            Action = AuditAction.Login,
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id.ToString(),
            ActorPersonId = user.PersonId,
            ActorUserId = user.Id,
            Summary = $"{PrincipalLabel(isStaff, isGuardian, isStudent)} sign-in: {user.UserName}.",
            IpAddress = ipAddress,
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult.Success(accessToken, expiresAtUtc, refreshToken);
    }

    private static string PrincipalLabel(bool isStaff, bool isGuardian, bool isStudent)
        => isStaff ? "Staff" : isGuardian ? "Guardian" : isStudent ? "Student" : "Unknown";
}

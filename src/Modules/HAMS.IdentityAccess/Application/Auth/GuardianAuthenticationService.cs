using System.Security.Cryptography;
using System.Text;
using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HAMS.IdentityAccess.Application.Auth;

/// <summary>
/// Real implementation of guardian OTP login. <b>Deliberately bypasses the notification outbox</b>
/// (<c>INotificationOutboxWriter</c>) and calls <see cref="ISmsSender"/> directly, synchronously,
/// in-request — the one deliberate exception to that kernel's "never send synchronously" rule
/// (build plan §5). The outbox's whole design trades immediacy for guaranteed, retried, at-least-
/// once delivery of a message nobody is actively waiting on (an absence alert, a result notice);
/// an OTP code is the opposite — a human is looking at a screen right now waiting to type it in,
/// and a code that might arrive up to a minute late (the outbox's Hangfire dispatch cadence) is a
/// broken login experience, not just a delayed courtesy notice. This still goes through the exact
/// same <see cref="ISmsSender"/> abstraction as everything else, so it stays carrier-agnostic.
/// </summary>
internal sealed class GuardianAuthenticationService(
    IdentityAccessDbContext dbContext, UserManager<ApplicationUser> userManager,
    IGuardianRelationshipService guardianRelationshipService, IPersonRoleAssignmentService roleAssignmentService,
    IRoleMembershipQuery roleMembershipQuery, ISmsSender smsSender, ITokenIssuer tokenIssuer,
    IAuditLogWriter auditLogWriter, IClock clock)
    : IGuardianAuthenticationService
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    private const int MaxAttempts = 5;

    public async Task<OtpRequestResult> RequestOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var guardianPersonId = await guardianRelationshipService.FindVerifiedGuardianPersonIdByPhoneAsync(phoneNumber, clock.TodayUtc, cancellationToken);
        if (guardianPersonId is null)
        {
            return OtpRequestResult.Failed("No verified guardian relationship is registered for this phone number.");
        }

        // Invalidate any code still outstanding for this number — only the most recently requested
        // code should ever be valid, avoiding ambiguity about which of several codes is current.
        var now = clock.UtcNow;
        var outstanding = await dbContext.GuardianOtpChallenges
            .Where(c => c.PhoneNumber == phoneNumber && c.ConsumedAtUtc == null && c.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
        foreach (var stale in outstanding)
        {
            stale.ExpiresAtUtc = now;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var challenge = new GuardianOtpChallenge
        {
            Id = Guid.NewGuid(),
            PersonId = guardianPersonId.Value,
            PhoneNumber = phoneNumber,
            CodeHash = HashCode(code),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(ChallengeLifetime),
        };
        dbContext.GuardianOtpChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        await smsSender.SendAsync(phoneNumber, $"Your HAMS verification code is {code}. It expires in 5 minutes.", cancellationToken);

        return OtpRequestResult.Success(challenge.Id, challenge.ExpiresAtUtc);
    }

    public async Task<AuthResult> VerifyOtpAsync(
        Guid challengeId, string code, string? deviceLabel, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var challenge = await dbContext.GuardianOtpChallenges.FindAsync([challengeId], cancellationToken);
        if (challenge is null || challenge.ConsumedAtUtc is not null || challenge.ExpiresAtUtc <= clock.UtcNow)
        {
            return AuthResult.Failed("Invalid or expired code.");
        }

        if (challenge.AttemptCount >= MaxAttempts)
        {
            return AuthResult.Failed("Too many attempts. Request a new code.");
        }

        challenge.AttemptCount++;

        if (challenge.CodeHash != HashCode(code))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await LogFailedAttemptAsync(challenge.PersonId, challenge.PhoneNumber, ipAddress, cancellationToken);
            return AuthResult.Failed("Invalid or expired code.");
        }

        challenge.ConsumedAtUtc = clock.UtcNow;

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.PersonId == challenge.PersonId, cancellationToken);
        if (user is null)
        {
            var newUserId = Guid.NewGuid();
            // Email is a syntactically-valid, guaranteed-unique placeholder, never a real address —
            // IdentityAccessModule configures RequireUniqueEmail=true (meaningful for staff sign-in),
            // and UserManager.CreateAsync's default validators reject a null/empty Email regardless
            // of that setting's actual purpose for this principal type, which has no email at all.
            user = new ApplicationUser
            {
                Id = newUserId, PersonId = challenge.PersonId, UserName = challenge.PhoneNumber, Email = $"{newUserId}@guardian.hams.local",
            };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return AuthResult.Failed(string.Join(" ", createResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await roleMembershipQuery.HasRoleAsync(challenge.PersonId, RoleCodes.Guardian, clock.TodayUtc, cancellationToken))
        {
            await roleAssignmentService.AssignRoleAsync(challenge.PersonId, RoleCodes.Guardian, schoolId: null, clock.TodayUtc, effectiveTo: null, cancellationToken);
        }

        return await tokenIssuer.IssueAsync(user, isStaff: false, isGuardian: true, isStudent: false, deviceLabel, ipAddress, cancellationToken);
    }

    private static string HashCode(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private async Task LogFailedAttemptAsync(Guid personId, string phoneNumber, string? ipAddress, CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogEntry
        {
            OccurredAtUtc = clock.UtcNow,
            Action = AuditAction.LoginFailed,
            EntityType = nameof(GuardianOtpChallenge),
            ActorPersonId = personId,
            Summary = $"Failed guardian OTP verification attempt: {phoneNumber}.",
            IpAddress = ipAddress,
        }, cancellationToken);
    }
}

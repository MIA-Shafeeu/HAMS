using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HAMS.IdentityAccess.Application.Auth;

internal sealed class StudentAuthenticationService(
    IdentityAccessDbContext dbContext, UserManager<ApplicationUser> userManager, IPasswordHasher<ApplicationUser> passwordHasher,
    IStudentProfileLookup studentProfileLookup, IPersonRoleAssignmentService roleAssignmentService,
    IRoleMembershipQuery roleMembershipQuery, ITokenIssuer tokenIssuer, IAuditLogWriter auditLogWriter, IClock clock)
    : IStudentAuthenticationService
{
    public async Task SetPinAsync(Guid studentPersonId, string pin, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.PersonId == studentPersonId, cancellationToken);
        if (user is null)
        {
            // UserName only needs to be stable and unique — nothing ever signs in "by username" for
            // a student, login always resolves the admission number to a PersonId first (see
            // LoginAsync), so this never needs to track a real-world identifier like AdmissionNumber.
            // Email is a syntactically-valid, guaranteed-unique placeholder, never a real address —
            // see GuardianAuthenticationService's identical remark on why one is needed at all.
            var newUserId = Guid.NewGuid();
            user = new ApplicationUser
            {
                Id = newUserId, PersonId = studentPersonId, UserName = studentPersonId.ToString(), Email = $"{newUserId}@student.hams.local",
            };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
            }
        }

        // Bypasses UserManager.ChangePasswordAsync/AddPasswordAsync deliberately — both re-validate
        // IdentityOptions.Password (RequiredLength = 10), which a numeric PIN was never meant to
        // satisfy. Hashing directly still uses Identity's own proven hasher; only the policy
        // validation step is skipped. CheckPasswordAsync (used at login) never re-validates policy,
        // only verifies, so this round-trips correctly.
        user.PasswordHash = passwordHasher.HashPassword(user, pin);
        await userManager.UpdateAsync(user);

        if (!await roleMembershipQuery.HasRoleAsync(studentPersonId, RoleCodes.Student, clock.TodayUtc, cancellationToken))
        {
            await roleAssignmentService.AssignRoleAsync(studentPersonId, RoleCodes.Student, schoolId: null, clock.TodayUtc, effectiveTo: null, cancellationToken);
        }
    }

    public async Task<AuthResult> LoginAsync(StudentLoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var personId = await studentProfileLookup.FindPersonIdByAdmissionNumberAsync(request.AdmissionNumber, cancellationToken);
        var user = personId is null ? null : await dbContext.Users.SingleOrDefaultAsync(u => u.PersonId == personId, cancellationToken);

        if (user is null)
        {
            await LogFailedAttemptAsync(personId, request.AdmissionNumber, ipAddress, cancellationToken);
            return AuthResult.Failed("Invalid admission number or PIN.");
        }

        if (user.Status != AccountStatus.Active)
        {
            return AuthResult.Failed("This account is not active. Contact your school administrator.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthResult.Failed("This account is temporarily locked due to repeated failed sign-in attempts.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Pin))
        {
            await userManager.AccessFailedAsync(user);
            await LogFailedAttemptAsync(personId, request.AdmissionNumber, ipAddress, cancellationToken);
            return AuthResult.Failed("Invalid admission number or PIN.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await tokenIssuer.IssueAsync(user, isStaff: false, isGuardian: false, isStudent: true, request.DeviceLabel, ipAddress, cancellationToken);
    }

    private async Task LogFailedAttemptAsync(Guid? personId, string attemptedAdmissionNumber, string? ipAddress, CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogEntry
        {
            OccurredAtUtc = clock.UtcNow,
            Action = AuditAction.LoginFailed,
            EntityType = nameof(ApplicationUser),
            ActorPersonId = personId,
            Summary = $"Failed student sign-in attempt: {attemptedAdmissionNumber}.",
            IpAddress = ipAddress,
        }, cancellationToken);
    }
}

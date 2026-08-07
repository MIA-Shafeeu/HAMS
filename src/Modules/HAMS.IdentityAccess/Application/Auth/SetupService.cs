using HAMS.IdentityAccess.Domain;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Identity;

namespace HAMS.IdentityAccess.Application.Auth;

internal sealed class SetupService(
    UserManager<ApplicationUser> userManager, IPersonRoleAssignmentService roleAssignmentService,
    IRoleMembershipQuery roleMembershipQuery, IClock clock)
    : ISetupService
{
    public async Task<bool> IsBootstrapNeededAsync(CancellationToken cancellationToken = default) =>
        !await IsAlreadyBootstrappedAsync(cancellationToken);

    public async Task<Guid> BootstrapFirstAdminAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        if (await IsAlreadyBootstrappedAsync(cancellationToken))
        {
            throw new InvalidOperationException("A System Administrator already exists — this one-time setup endpoint permanently refuses after the first use.");
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@hams.local",
            EmailConfirmed = true,
            PersonId = Guid.NewGuid(),
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }

        await roleAssignmentService.AssignRoleAsync(
            user.PersonId, RoleCodes.SystemAdministrator, schoolId: null,
            effectiveFrom: clock.TodayUtc, effectiveTo: null, cancellationToken);

        return user.Id;
    }

    private Task<bool> IsAlreadyBootstrappedAsync(CancellationToken cancellationToken) =>
        roleMembershipQuery.AnyPersonHasRoleAsync(RoleCodes.SystemAdministrator, clock.TodayUtc, cancellationToken);
}

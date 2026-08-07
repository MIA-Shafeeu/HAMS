using HAMS.IdentityAccess.Domain;
using HAMS.IdentityAccess.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HAMS.IdentityAccess.Application.Auth;

internal sealed class StaffAccountService(UserManager<ApplicationUser> userManager, IdentityAccessDbContext dbContext) : IStaffAccountService
{
    public async Task<Guid> CreateAccountAsync(Guid personId, string userName, string? email, string initialPassword, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(u => u.PersonId == personId, cancellationToken))
        {
            throw new InvalidOperationException("This person already has a login account.");
        }

        var user = new ApplicationUser { UserName = userName, Email = email, PersonId = personId, Status = AccountStatus.Active };
        var result = await userManager.CreateAsync(user, initialPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return user.Id;
    }

    public async Task<StaffAccountSummary?> GetAccountByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.PersonId == personId, cancellationToken);
        return user is null ? null : ToSummary(user);
    }

    public async Task<IReadOnlyList<StaffAccountSummary>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users.OrderBy(u => u.UserName).ToListAsync(cancellationToken);
        return users.Select(ToSummary).ToList();
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("Account not found.");

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", removeResult.Errors.Select(e => e.Description)));
        }

        var addResult = await userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", addResult.Errors.Select(e => e.Description)));
        }
    }

    public async Task SetAccountStatusAsync(Guid userId, AccountStatus status, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Account not found.");

        user.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static StaffAccountSummary ToSummary(ApplicationUser user) => new(user.Id, user.PersonId, user.UserName!, user.Email, user.Status);
}

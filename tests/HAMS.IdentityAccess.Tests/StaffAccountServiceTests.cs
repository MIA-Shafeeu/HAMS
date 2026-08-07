using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Domain;

namespace HAMS.IdentityAccess.Tests;

public class StaffAccountServiceTests
{
    private const string Password = "Correct-Horse-1!";

    private static StaffAccountService CreateService()
    {
        var (userManager, _, db) = IdentityTestHarness.Create();
        return new StaffAccountService(userManager, db);
    }

    [Fact]
    public async Task CreateAccountAsync_creates_an_Active_account_retrievable_by_PersonId()
    {
        var service = CreateService();
        var personId = Guid.NewGuid();

        var userId = await service.CreateAccountAsync(personId, "ahmed.naseer", "ahmed@hams.local", Password);

        var account = await service.GetAccountByPersonIdAsync(personId);
        Assert.NotNull(account);
        Assert.Equal(userId, account!.UserId);
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public async Task CreateAccountAsync_throws_when_the_person_already_has_an_account()
    {
        var service = CreateService();
        var personId = Guid.NewGuid();
        await service.CreateAccountAsync(personId, "ahmed.naseer", "ahmed@hams.local", Password);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAccountAsync(personId, "ahmed.naseer2", "ahmed2@hams.local", Password));
    }

    [Fact]
    public async Task CreateAccountAsync_throws_for_a_password_that_fails_the_configured_policy()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAccountAsync(Guid.NewGuid(), "weak.pw", "weak@hams.local", "short"));
    }

    [Fact]
    public async Task GetAccountByPersonIdAsync_returns_null_for_a_person_with_no_account()
    {
        var service = CreateService();

        Assert.Null(await service.GetAccountByPersonIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAccountsAsync_returns_every_created_account_ordered_by_username()
    {
        var service = CreateService();
        await service.CreateAccountAsync(Guid.NewGuid(), "zainab", "zainab@hams.local", Password);
        await service.CreateAccountAsync(Guid.NewGuid(), "ahmed", "ahmed@hams.local", Password);

        var accounts = await service.GetAccountsAsync();

        Assert.Equal(["ahmed", "zainab"], accounts.Select(a => a.UserName));
    }

    [Fact]
    public async Task ResetPasswordAsync_replaces_the_password_so_the_old_one_no_longer_verifies()
    {
        var (userManager, _, dbContext) = HAMS.IdentityAccess.Tests.IdentityTestHarness.Create();
        var service = new StaffAccountService(userManager, dbContext);
        var personId = Guid.NewGuid();
        var userId = await service.CreateAccountAsync(personId, "ahmed.naseer", "ahmed@hams.local", Password);

        const string newPassword = "New-Correct-Horse-2!";
        await service.ResetPasswordAsync(userId, newPassword);

        var user = await userManager.FindByIdAsync(userId.ToString());
        Assert.NotNull(user);
        Assert.False(await userManager.CheckPasswordAsync(user!, Password));
        Assert.True(await userManager.CheckPasswordAsync(user!, newPassword));
    }

    [Fact]
    public async Task ResetPasswordAsync_throws_for_an_unknown_account()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetPasswordAsync(Guid.NewGuid(), "New-Correct-Horse-2!"));
    }

    [Fact]
    public async Task SetAccountStatusAsync_updates_the_stored_status()
    {
        var service = CreateService();
        var personId = Guid.NewGuid();
        var userId = await service.CreateAccountAsync(personId, "ahmed.naseer", "ahmed@hams.local", Password);

        await service.SetAccountStatusAsync(userId, AccountStatus.Disabled);

        var account = await service.GetAccountByPersonIdAsync(personId);
        Assert.Equal(AccountStatus.Disabled, account!.Status);
    }

    [Fact]
    public async Task SetAccountStatusAsync_throws_for_an_unknown_account()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetAccountStatusAsync(Guid.NewGuid(), AccountStatus.Disabled));
    }
}

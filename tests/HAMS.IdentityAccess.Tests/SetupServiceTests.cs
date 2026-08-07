using HAMS.IdentityAccess.Application.Auth;
using HAMS.Platform.Access.Domain;

namespace HAMS.IdentityAccess.Tests;

public class SetupServiceTests
{
    private const string Password = "Correct-Horse-1!";

    private static (SetupService Service, FakePersonRoleAssignmentService RoleService) CreateService()
    {
        var (userManager, _, _) = IdentityTestHarness.Create();
        var roleService = new FakePersonRoleAssignmentService();
        var clock = new FakeClock(new DateOnly(2026, 8, 7));
        return (new SetupService(userManager, roleService, roleService, clock), roleService);
    }

    [Fact]
    public async Task IsBootstrapNeededAsync_is_true_when_no_System_Administrator_exists_yet()
    {
        var (service, _) = CreateService();

        Assert.True(await service.IsBootstrapNeededAsync());
    }

    [Fact]
    public async Task BootstrapFirstAdminAsync_creates_an_account_and_assigns_SystemAdministrator()
    {
        var (service, roleService) = CreateService();

        var userId = await service.BootstrapFirstAdminAsync("admin", Password);

        Assert.NotEqual(Guid.Empty, userId);
        Assert.Contains(roleService.Assignments, a => a.RoleCode == RoleCodes.SystemAdministrator);
    }

    [Fact]
    public async Task IsBootstrapNeededAsync_is_false_after_a_successful_bootstrap()
    {
        var (service, _) = CreateService();
        await service.BootstrapFirstAdminAsync("admin", Password);

        Assert.False(await service.IsBootstrapNeededAsync());
    }

    [Fact]
    public async Task BootstrapFirstAdminAsync_refuses_a_second_time_once_a_System_Administrator_exists()
    {
        var (service, _) = CreateService();
        await service.BootstrapFirstAdminAsync("admin", Password);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BootstrapFirstAdminAsync("admin2", Password));
    }

    [Fact]
    public async Task BootstrapFirstAdminAsync_throws_for_a_password_that_fails_the_configured_policy()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BootstrapFirstAdminAsync("admin", "short"));
    }
}

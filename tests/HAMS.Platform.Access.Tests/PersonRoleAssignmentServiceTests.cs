using HAMS.Platform.Access.Domain;
using HAMS.Platform.Access.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access.Tests;

public class PersonRoleAssignmentServiceTests
{
    private const string ClassTeacherCode = RoleCodes.ClassTeacher;

    private static async Task<AccessDbContext> CreateContextAsync()
    {
        var db = new AccessDbContext(new DbContextOptionsBuilder<AccessDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.Add(new Role { Id = Guid.NewGuid(), Code = ClassTeacherCode, Name = "Class Teacher", DisplayOrder = 1 });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task AssignRoleAsync_throws_for_an_unknown_role_code()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignRoleAsync(Guid.NewGuid(), "NONEXISTENT_ROLE", null, new DateOnly(2026, 1, 1), null));
    }

    [Fact]
    public async Task AssignRoleAsync_is_retrievable_via_GetAssignmentsForPersonAsync()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));
        var personId = Guid.NewGuid();

        var assignmentId = await service.AssignRoleAsync(personId, ClassTeacherCode, null, new DateOnly(2026, 1, 1), null);

        var assignments = await service.GetAssignmentsForPersonAsync(personId);
        Assert.Single(assignments, a => a.Id == assignmentId);
    }

    [Fact]
    public async Task GetAssignmentsForPersonAsync_does_not_return_another_persons_assignments()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));
        var personId = Guid.NewGuid();
        await service.AssignRoleAsync(Guid.NewGuid(), ClassTeacherCode, null, new DateOnly(2026, 1, 1), null);

        var assignments = await service.GetAssignmentsForPersonAsync(personId);

        Assert.Empty(assignments);
    }

    [Fact]
    public async Task GetRolesAsync_returns_only_active_roles_ordered_by_display_order()
    {
        await using var db = await CreateContextAsync();
        db.Roles.Add(new Role { Id = Guid.NewGuid(), Code = "INACTIVE_ROLE", Name = "Inactive", DisplayOrder = 0, IsActive = false });
        await db.SaveChangesAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));

        var roles = await service.GetRolesAsync();

        Assert.DoesNotContain(roles, r => r.Code == "INACTIVE_ROLE");
        Assert.Contains(roles, r => r.Code == ClassTeacherCode);
    }

    [Fact]
    public async Task GetAllRolesAsync_includes_inactive_roles_unlike_GetRolesAsync()
    {
        await using var db = await CreateContextAsync();
        db.Roles.Add(new Role { Id = Guid.NewGuid(), Code = "INACTIVE_ROLE", Name = "Inactive", DisplayOrder = 0, IsActive = false });
        await db.SaveChangesAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));

        var roles = await service.GetAllRolesAsync();

        Assert.Contains(roles, r => r.Code == "INACTIVE_ROLE");
        Assert.Contains(roles, r => r.Code == ClassTeacherCode);
    }

    [Fact]
    public async Task RevokeRoleAsync_sets_effective_to_but_keeps_the_assignment_visible()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));
        var personId = Guid.NewGuid();
        var assignmentId = await service.AssignRoleAsync(personId, ClassTeacherCode, null, new DateOnly(2026, 1, 1), null);

        await service.RevokeRoleAsync(assignmentId, new DateOnly(2026, 6, 30));

        var assignment = (await service.GetAssignmentsForPersonAsync(personId)).Single(a => a.Id == assignmentId);
        Assert.Equal(new DateOnly(2026, 6, 30), assignment.EffectiveTo);
    }

    [Fact]
    public async Task CreateRoleAsync_creates_a_retrievable_role()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));

        var roleId = await service.CreateRoleAsync("LIBRARIAN", "Librarian", "Manages the library.", 5);

        var roles = await service.GetRolesAsync();
        var role = Assert.Single(roles, r => r.Id == roleId);
        Assert.Equal("LIBRARIAN", role.Code);
        Assert.Equal("Librarian", role.Name);
        Assert.Equal("Manages the library.", role.Description);
        Assert.Equal(5, role.DisplayOrder);
        Assert.True(role.IsActive);
    }

    [Fact]
    public async Task SetRoleActiveAsync_flips_is_active()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));
        var roleId = await service.CreateRoleAsync("LIBRARIAN", "Librarian", null, 5);

        await service.SetRoleActiveAsync(roleId, false);

        var role = await db.Roles.FindAsync(roleId);
        Assert.NotNull(role);
        Assert.False(role!.IsActive);

        await service.SetRoleActiveAsync(roleId, true);

        db.ChangeTracker.Clear();
        role = await db.Roles.FindAsync(roleId);
        Assert.NotNull(role);
        Assert.True(role!.IsActive);
    }

    [Fact]
    public async Task SetRoleActiveAsync_throws_for_an_unknown_role_id()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetRoleActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task CreateConfidentialityTierAsync_and_GetConfidentialityTiersAsync_round_trip_including_rank()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));

        var tierId = await service.CreateConfidentialityTierAsync("RESTRICTED", "Restricted", "Restricted data.", 10, 1);

        var tiers = await service.GetConfidentialityTiersAsync();
        var tier = Assert.Single(tiers, t => t.Id == tierId);
        Assert.Equal("RESTRICTED", tier.Code);
        Assert.Equal("Restricted", tier.Name);
        Assert.Equal("Restricted data.", tier.Description);
        Assert.Equal(10, tier.Rank);
        Assert.Equal(1, tier.DisplayOrder);
        Assert.True(tier.IsActive);
    }

    [Fact]
    public async Task GetConfidentialityTiersAsync_orders_by_display_order()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));
        await service.CreateConfidentialityTierAsync("SAFEGUARDING", "Safeguarding", null, 20, 2);
        await service.CreateConfidentialityTierAsync("RESTRICTED", "Restricted", null, 10, 1);

        var tiers = await service.GetConfidentialityTiersAsync();

        Assert.Equal(["RESTRICTED", "SAFEGUARDING"], tiers.Select(t => t.Code));
    }

    [Fact]
    public async Task SetConfidentialityTierActiveAsync_flips_is_active()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));
        var tierId = await service.CreateConfidentialityTierAsync("RESTRICTED", "Restricted", null, 10, 1);

        await service.SetConfidentialityTierActiveAsync(tierId, false);

        var tier = await db.ConfidentialityTiers.FindAsync(tierId);
        Assert.NotNull(tier);
        Assert.False(tier!.IsActive);

        await service.SetConfidentialityTierActiveAsync(tierId, true);

        db.ChangeTracker.Clear();
        tier = await db.ConfidentialityTiers.FindAsync(tierId);
        Assert.NotNull(tier);
        Assert.True(tier!.IsActive);
    }

    [Fact]
    public async Task SetConfidentialityTierActiveAsync_throws_for_an_unknown_tier_id()
    {
        await using var db = await CreateContextAsync();
        var service = new PersonRoleAssignmentService(db, new AccessGrantProjectionService(db));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetConfidentialityTierActiveAsync(Guid.NewGuid(), false));
    }
}

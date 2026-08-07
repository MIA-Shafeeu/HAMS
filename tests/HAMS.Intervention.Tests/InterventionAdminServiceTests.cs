using HAMS.Intervention.Application;
using HAMS.Intervention.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Tests;

public class InterventionAdminServiceTests
{
    private static InterventionDbContext CreateContext() => new(
        new DbContextOptionsBuilder<InterventionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateBehaviourCategoryAsync_is_retrievable_via_GetBehaviourCategoriesAsync()
    {
        await using var db = CreateContext();
        var service = new InterventionAdminService(db);

        var id = await service.CreateBehaviourCategoryAsync("KINDNESS", "Kindness", isPositive: true, displayOrder: 1);

        var category = Assert.Single(await service.GetBehaviourCategoriesAsync());
        Assert.Equal(id, category.Id);
        Assert.True(category.IsPositive);
    }

    [Fact]
    public async Task SetBehaviourCategoryActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new InterventionAdminService(db);
        var id = await service.CreateBehaviourCategoryAsync("KINDNESS", "Kindness", isPositive: true, displayOrder: 1);

        await service.SetBehaviourCategoryActiveAsync(id, false);

        var category = await db.BehaviourCategories.SingleAsync(c => c.Id == id);
        Assert.False(category.IsActive);
    }

    [Fact]
    public async Task SetBehaviourCategoryActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new InterventionAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetBehaviourCategoryActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task CreateInterventionTypeAsync_is_retrievable_via_GetInterventionTypesAsync()
    {
        await using var db = CreateContext();
        var service = new InterventionAdminService(db);

        var id = await service.CreateInterventionTypeAsync("MENTORING", "Mentoring", 1);

        var type = Assert.Single(await service.GetInterventionTypesAsync());
        Assert.Equal(id, type.Id);
        Assert.Equal("MENTORING", type.Code);
    }

    [Fact]
    public async Task SetInterventionTypeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new InterventionAdminService(db);
        var id = await service.CreateInterventionTypeAsync("MENTORING", "Mentoring", 1);

        await service.SetInterventionTypeActiveAsync(id, false);

        var type = await db.InterventionTypes.SingleAsync(t => t.Id == id);
        Assert.False(type.IsActive);
    }

    [Fact]
    public async Task SetInterventionTypeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new InterventionAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetInterventionTypeActiveAsync(Guid.NewGuid(), false));
    }
}

using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class LessonPlanningServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateSchemeOfWorkAsync_creates_and_GetSchemesOfWorkAsync_filters_correctly()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var yearId = Guid.NewGuid();

        var id = await service.CreateSchemeOfWorkAsync(subjectId, gradeId, yearId, "Term 1 Maths");
        await service.CreateSchemeOfWorkAsync(Guid.NewGuid(), gradeId, yearId, "Different subject");

        var result = await service.GetSchemesOfWorkAsync(subjectId, gradeId, yearId);

        var scheme = Assert.Single(result);
        Assert.Equal(id, scheme.Id);
        Assert.Equal("Term 1 Maths", scheme.Title);
    }

    [Fact]
    public async Task AddSchemeOfWorkItemAsync_and_GetSchemeOfWorkItemsAsync_orders_by_DisplayOrder()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);
        var schemeId = await service.CreateSchemeOfWorkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Scheme");

        var secondId = await service.AddSchemeOfWorkItemAsync(schemeId, Guid.NewGuid(), 2, 2);
        var firstId = await service.AddSchemeOfWorkItemAsync(schemeId, Guid.NewGuid(), 1, 1);

        var items = await service.GetSchemeOfWorkItemsAsync(schemeId);

        Assert.Equal([firstId, secondId], items.Select(i => i.Id));
    }

    [Fact]
    public async Task CreateTeachingTopicAsync_persists_bilingual_name()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);
        var itemId = Guid.NewGuid();

        var topicId = await service.CreateTeachingTopicAsync(itemId, "Fractions", "ބައި ސުވާލު", 1);

        var topic = await db.TeachingTopics.SingleAsync(t => t.Id == topicId);
        Assert.Equal("Fractions", topic.NameEn);
        Assert.Equal("ބައި ސުވާލު", topic.NameDv);
    }

    [Fact]
    public async Task CreateLessonPlanAsync_and_GetLessonPlansAsync_orders_by_PlannedDate()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);
        var topicId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        var laterId = await service.CreateLessonPlanAsync(topicId, staffId, new DateOnly(2026, 8, 10), "Later lesson");
        var earlierId = await service.CreateLessonPlanAsync(topicId, staffId, new DateOnly(2026, 8, 3), "Earlier lesson");

        var plans = await service.GetLessonPlansAsync(topicId);

        Assert.Equal([earlierId, laterId], plans.Select(p => p.Id));
    }

    [Fact]
    public async Task AddResourceAsync_resolves_the_resource_type_by_code()
    {
        await using var db = CreateContext();
        var resourceTypeId = Guid.NewGuid();
        db.ResourceTypes.Add(new ResourceType { Id = resourceTypeId, Code = "VIDEO", Name = "Video", IsActive = true });
        await db.SaveChangesAsync();
        var service = new LessonPlanningService(db);
        var topicId = Guid.NewGuid();
        var uploaderId = Guid.NewGuid();

        var resourceId = await service.AddResourceAsync(topicId, "Intro video", "ތައާރަފް ވީޑިއޯ", "VIDEO", "https://example.test/video", uploaderId);

        var resources = await service.GetResourcesAsync(topicId);
        var resource = Assert.Single(resources);
        Assert.Equal(resourceId, resource.Id);
        Assert.Equal(resourceTypeId, resource.ResourceTypeId);
    }

    [Fact]
    public async Task AddResourceAsync_throws_for_an_unknown_or_inactive_resource_type_code()
    {
        await using var db = CreateContext();
        db.ResourceTypes.Add(new ResourceType { Id = Guid.NewGuid(), Code = "RETIRED", Name = "Retired", IsActive = false });
        await db.SaveChangesAsync();
        var service = new LessonPlanningService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddResourceAsync(Guid.NewGuid(), "x", "x", "RETIRED", "x", Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddResourceAsync(Guid.NewGuid(), "x", "x", "NONEXISTENT", "x", Guid.NewGuid()));
    }

    [Fact]
    public async Task GetResourceTypesAsync_orders_by_DisplayOrder()
    {
        await using var db = CreateContext();
        db.ResourceTypes.AddRange(
            new ResourceType { Id = Guid.NewGuid(), Code = "B", Name = "B", DisplayOrder = 2, IsActive = true },
            new ResourceType { Id = Guid.NewGuid(), Code = "A", Name = "A", DisplayOrder = 1, IsActive = true });
        await db.SaveChangesAsync();
        var service = new LessonPlanningService(db);

        var types = await service.GetResourceTypesAsync();

        Assert.Equal(["A", "B"], types.Select(t => t.Code));
    }

    [Fact]
    public async Task CreateResourceTypeAsync_is_retrievable_via_GetResourceTypesAsync()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);

        var id = await service.CreateResourceTypeAsync("AUDIO", "Audio", 3);

        var types = await service.GetResourceTypesAsync();
        Assert.Single(types, t => t.Id == id && t.Code == "AUDIO" && t.DisplayOrder == 3);
    }

    [Fact]
    public async Task SetResourceTypeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);
        var id = await service.CreateResourceTypeAsync("TEMP", "Temp", 1);

        await service.SetResourceTypeActiveAsync(id, false);

        var type = Assert.Single(await service.GetResourceTypesAsync());
        Assert.False(type.IsActive);
    }

    [Fact]
    public async Task SetResourceTypeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetResourceTypeActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task GetEvidenceTypesAsync_orders_by_DisplayOrder()
    {
        await using var db = CreateContext();
        db.EvidenceTypes.AddRange(
            new EvidenceType { Id = Guid.NewGuid(), Code = "B", Name = "B", DisplayOrder = 2, IsActive = true },
            new EvidenceType { Id = Guid.NewGuid(), Code = "A", Name = "A", DisplayOrder = 1, IsActive = true });
        await db.SaveChangesAsync();
        var service = new LessonPlanningService(db);

        var types = await service.GetEvidenceTypesAsync();

        Assert.Equal(["A", "B"], types.Select(t => t.Code));
    }

    [Fact]
    public async Task CreateEvidenceTypeAsync_is_retrievable_via_GetEvidenceTypesAsync()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);

        var id = await service.CreateEvidenceTypeAsync("VIDEO_LOG", "Video Log", 8);

        var types = await service.GetEvidenceTypesAsync();
        Assert.Single(types, t => t.Id == id && t.Code == "VIDEO_LOG" && t.DisplayOrder == 8);
    }

    [Fact]
    public async Task SetEvidenceTypeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);
        var id = await service.CreateEvidenceTypeAsync("TEMP", "Temp", 1);

        await service.SetEvidenceTypeActiveAsync(id, false);

        var type = Assert.Single(await service.GetEvidenceTypesAsync());
        Assert.False(type.IsActive);
    }

    [Fact]
    public async Task SetEvidenceTypeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new LessonPlanningService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetEvidenceTypeActiveAsync(Guid.NewGuid(), false));
    }
}

using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class LessonPlanningService(LearningDeliveryDbContext dbContext) : ILessonPlanningService
{
    public async Task<Guid> CreateSchemeOfWorkAsync(Guid subjectId, Guid gradeId, Guid academicYearId, string title, CancellationToken cancellationToken = default)
    {
        var scheme = new SchemeOfWork { Id = Guid.NewGuid(), SubjectId = subjectId, GradeId = gradeId, AcademicYearId = academicYearId, Title = title };
        dbContext.SchemeOfWorks.Add(scheme);
        await dbContext.SaveChangesAsync(cancellationToken);
        return scheme.Id;
    }

    public async Task<IReadOnlyList<SchemeOfWork>> GetSchemesOfWorkAsync(Guid subjectId, Guid gradeId, Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.SchemeOfWorks.Where(s => s.SubjectId == subjectId && s.GradeId == gradeId && s.AcademicYearId == academicYearId).ToListAsync(cancellationToken);

    public async Task<Guid> AddSchemeOfWorkItemAsync(Guid schemeOfWorkId, Guid learningOutcomeId, int plannedWeekNumber, int displayOrder, CancellationToken cancellationToken = default)
    {
        var item = new SchemeOfWorkItem
        {
            Id = Guid.NewGuid(), SchemeOfWorkId = schemeOfWorkId, LearningOutcomeId = learningOutcomeId,
            PlannedWeekNumber = plannedWeekNumber, DisplayOrder = displayOrder,
        };
        dbContext.SchemeOfWorkItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<IReadOnlyList<SchemeOfWorkItem>> GetSchemeOfWorkItemsAsync(Guid schemeOfWorkId, CancellationToken cancellationToken = default) =>
        await dbContext.SchemeOfWorkItems.Where(i => i.SchemeOfWorkId == schemeOfWorkId).OrderBy(i => i.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateTeachingTopicAsync(Guid schemeOfWorkItemId, string nameEn, string nameDv, int displayOrder, CancellationToken cancellationToken = default)
    {
        var topic = new TeachingTopic { Id = Guid.NewGuid(), SchemeOfWorkItemId = schemeOfWorkItemId, NameEn = nameEn, NameDv = nameDv, DisplayOrder = displayOrder };
        dbContext.TeachingTopics.Add(topic);
        await dbContext.SaveChangesAsync(cancellationToken);
        return topic.Id;
    }

    public async Task<Guid> CreateLessonPlanAsync(Guid teachingTopicId, Guid staffPersonId, DateOnly plannedDate, string objectives, CancellationToken cancellationToken = default)
    {
        var plan = new LessonPlan { Id = Guid.NewGuid(), TeachingTopicId = teachingTopicId, StaffPersonId = staffPersonId, PlannedDate = plannedDate, Objectives = objectives };
        dbContext.LessonPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }

    public async Task<IReadOnlyList<LessonPlan>> GetLessonPlansAsync(Guid teachingTopicId, CancellationToken cancellationToken = default) =>
        await dbContext.LessonPlans.Where(p => p.TeachingTopicId == teachingTopicId).OrderBy(p => p.PlannedDate).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ResourceType>> GetResourceTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ResourceTypes.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateResourceTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var resourceType = new ResourceType { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.ResourceTypes.Add(resourceType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return resourceType.Id;
    }

    public async Task SetResourceTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var resourceType = await dbContext.ResourceTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Resource type not found.");

        resourceType.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddResourceAsync(
        Guid teachingTopicId, string titleEn, string titleDv, string resourceTypeCode, string fileReference, Guid uploadedByPersonId,
        CancellationToken cancellationToken = default)
    {
        var resourceType = await dbContext.ResourceTypes.SingleOrDefaultAsync(t => t.Code == resourceTypeCode && t.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active resource type with code '{resourceTypeCode}'.");

        var resource = new Resource
        {
            Id = Guid.NewGuid(), TeachingTopicId = teachingTopicId, TitleEn = titleEn, TitleDv = titleDv,
            ResourceTypeId = resourceType.Id, FileReference = fileReference, UploadedByPersonId = uploadedByPersonId,
        };
        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
        return resource.Id;
    }

    public async Task<IReadOnlyList<Resource>> GetResourcesAsync(Guid teachingTopicId, CancellationToken cancellationToken = default) =>
        await dbContext.Resources.Where(r => r.TeachingTopicId == teachingTopicId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EvidenceType>> GetEvidenceTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.EvidenceTypes.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateEvidenceTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var evidenceType = new EvidenceType { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.EvidenceTypes.Add(evidenceType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return evidenceType.Id;
    }

    public async Task SetEvidenceTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var evidenceType = await dbContext.EvidenceTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Evidence type not found.");

        evidenceType.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

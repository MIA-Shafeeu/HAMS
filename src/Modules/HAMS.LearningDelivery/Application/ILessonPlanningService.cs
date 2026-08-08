using HAMS.LearningDelivery.Domain;

namespace HAMS.LearningDelivery.Application;

/// <summary>
/// Scheme of Work / Teaching Topic / Lesson Plan / Resource CRUD (build plan Phase 5 scope) —
/// extracted from what were previously inline <c>LearningDeliveryDbContext</c> queries directly
/// inside <c>LearningPlanEndpoints</c>' minimal-API lambdas (the same "structural CRUD only exists
/// at the endpoint layer" gap every early-phase module accumulated) into a real, DI-injectable
/// Application-layer service — needed the moment a Blazor page (not just an HTTP endpoint) wanted
/// this same functionality. The endpoints now delegate here too, so there's exactly one
/// implementation, not two that could silently drift apart.
/// </summary>
public interface ILessonPlanningService
{
    Task<Guid> CreateSchemeOfWorkAsync(Guid subjectId, Guid gradeId, Guid academicYearId, string title, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchemeOfWork>> GetSchemesOfWorkAsync(Guid subjectId, Guid gradeId, Guid academicYearId, CancellationToken cancellationToken = default);

    Task<Guid> AddSchemeOfWorkItemAsync(Guid schemeOfWorkId, Guid learningOutcomeId, int plannedWeekNumber, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchemeOfWorkItem>> GetSchemeOfWorkItemsAsync(Guid schemeOfWorkId, CancellationToken cancellationToken = default);

    Task<Guid> CreateTeachingTopicAsync(Guid schemeOfWorkItemId, string nameEn, string nameDv, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeachingTopic>> GetTeachingTopicsAsync(Guid schemeOfWorkItemId, CancellationToken cancellationToken = default);

    Task<Guid> CreateLessonPlanAsync(Guid teachingTopicId, Guid staffPersonId, DateOnly plannedDate, string objectives, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LessonPlan>> GetLessonPlansAsync(Guid teachingTopicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceType>> GetResourceTypesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateResourceTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetResourceTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task UpdateResourceTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active resource type with that code exists.</exception>
    Task<Guid> AddResourceAsync(
        Guid teachingTopicId, string titleEn, string titleDv, string resourceTypeCode, string fileReference, Guid uploadedByPersonId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Resource>> GetResourcesAsync(Guid teachingTopicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceType>> GetEvidenceTypesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateEvidenceTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetEvidenceTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task UpdateEvidenceTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);
}

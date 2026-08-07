using HAMS.OrgCurriculum.Domain;

namespace HAMS.OrgCurriculum.Application;

/// <summary>
/// Curriculum &amp; Syllabus setup (build plan Phase 2 scope) — extracted from what had been purely
/// inline <c>OrgDbContext</c> queries directly inside <c>CurriculumEndpoints</c>' minimal-API lambdas,
/// the same extraction already done for <see cref="IOrgAdminService"/>. Deliberately a thin wrapper
/// around <see cref="ISyllabusPublishingService"/>/<see cref="ICurriculumCsvImportService"/> for the
/// syllabus surface — this is not a curriculum-authoring tool, just admin CRUD over the catalogue
/// (CurriculumFramework/LearningArea/Subject) plus the handful of new reads (syllabus list, grade
/// applicability list) a Blazor page needs that no endpoint exposed before.
/// </summary>
public interface ICurriculumAdminService
{
    Task<Guid> CreateCurriculumFrameworkAsync(string code, string name, string? description, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurriculumFramework>> GetCurriculumFrameworksAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateLearningAreaAsync(Guid curriculumFrameworkId, string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LearningArea>> GetLearningAreasAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryMode>> GetDeliveryModesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every <c>DeliveryMode</c>, active or not — the Reference Data admin screen's list, distinct from <see cref="GetDeliveryModesAsync"/>'s active-only picker list.</summary>
    Task<IReadOnlyList<DeliveryMode>> GetAllDeliveryModesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateDeliveryModeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetDeliveryModeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediumOfInstruction>> GetMediumsOfInstructionAsync(CancellationToken cancellationToken = default);

    /// <summary>Every <c>MediumOfInstruction</c>, active or not — the Reference Data admin screen's list, distinct from <see cref="GetMediumsOfInstructionAsync"/>'s active-only picker list.</summary>
    Task<IReadOnlyList<MediumOfInstruction>> GetAllMediumsOfInstructionAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateMediumOfInstructionAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetMediumOfInstructionActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active delivery mode or medium of instruction with that code exists.</exception>
    Task<Guid> CreateSubjectAsync(
        Guid schoolId, Guid learningAreaId, string code, string name,
        string deliveryModeCode, string defaultMediumOfInstructionCode, int displayOrder,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subject>> GetSubjectsAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Syllabus>> GetSyllabusesForSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task AddSyllabusGradeApplicabilityAsync(Guid syllabusId, Guid gradeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyllabusGradeApplicability>> GetSyllabusGradeApplicabilitiesAsync(Guid syllabusId, CancellationToken cancellationToken = default);
}

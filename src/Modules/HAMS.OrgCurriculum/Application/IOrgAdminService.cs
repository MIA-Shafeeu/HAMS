using HAMS.OrgCurriculum.Domain;

namespace HAMS.OrgCurriculum.Application;

/// <summary>
/// School/Academic-Structure setup (build plan Phase 1 scope) — extracted from what had been, since
/// Phase 1, purely inline <c>OrgDbContext</c> queries directly inside <c>OrgEndpoints</c>' minimal-API
/// lambdas (the same "structural CRUD only ever got an endpoint, never a service" gap
/// <c>ILessonPlanningService</c> closed for LearningDelivery) into a real, DI-injectable
/// Application-layer service — needed the moment a System Administration Blazor UI wanted this same
/// functionality. <c>OrgEndpoints</c> now delegates here too, so there's exactly one implementation.
/// This is the literal prerequisite for every other page in the system: nothing else has anything to
/// select from a dropdown until a School/AcademicYear/Grade/Class/KeyStagePolicy exist.
/// </summary>
public interface IOrgAdminService
{
    Task<Guid> CreateSchoolAsync(string code, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<School>> GetSchoolsAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateCampusAsync(Guid schoolId, string code, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Campus>> GetCampusesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAcademicYearAsync(Guid schoolId, string code, string name, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicYear>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<Guid> CreateTermAsync(Guid academicYearId, string code, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Term>> GetTermsAsync(Guid academicYearId, CancellationToken cancellationToken = default);

    Task<Guid> CreatePhaseAsync(Guid schoolId, string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Phase>> GetPhasesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<Guid> CreateKeyStageAsync(Guid schoolId, Guid phaseId, string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KeyStage>> GetKeyStagesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<Guid> CreateGradeAsync(Guid schoolId, string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> GetGradesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task SetNextGradeAsync(Guid gradeId, Guid? nextGradeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationModel>> GetEvaluationModelsAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateEvaluationModelAsync(string code, string name, string? description, int displayOrder, CancellationToken cancellationToken = default);

    Task SetEvaluationModelActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<Guid> CreateClassAsync(Guid schoolId, Guid? campusId, Guid academicYearId, string code, string name, IReadOnlyList<Guid> gradeIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Class>> GetClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default);

    Task<Guid> CreateGradeKeyStageAssignmentAsync(Guid gradeId, Guid keyStageId, Guid academicYearId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active evaluation model with that code exists.</exception>
    Task<Guid> CreateKeyStagePolicyAsync(
        Guid keyStageId, Guid academicYearId, string evaluationModelCode,
        Guid? achievementScaleId, Guid? assessmentSchemeId, Guid? gradeScaleId, Guid? promotionPolicyId,
        CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The policy doesn't exist, or isn't currently Draft.</exception>
    Task PublishKeyStagePolicyAsync(Guid keyStagePolicyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KeyStagePolicy>> GetKeyStagePoliciesAsync(Guid keyStageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DayOfWeek>> GetWorkingDaysAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task SetWorkingDayAsync(Guid schoolId, DayOfWeek dayOfWeek, bool isWorkingDay, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HolidayType>> GetHolidayTypesAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateHolidayTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetHolidayTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Holiday>> GetHolidaysAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active holiday type with that code exists.</exception>
    Task<Guid> CreateHolidayAsync(Guid schoolId, DateOnly date, string holidayTypeCode, string nameEn, string nameDv, CancellationToken cancellationToken = default);
}

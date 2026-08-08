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

    /// <summary>Renames a <c>School</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateSchoolAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task<Guid> CreateCampusAsync(Guid schoolId, string code, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Campus>> GetCampusesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>Renames a <c>Campus</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateCampusAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task<Guid> CreateAcademicYearAsync(Guid schoolId, string code, string name, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicYear>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>Renames/reschedules an <c>AcademicYear</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateAcademicYearAsync(Guid id, string name, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<Guid> CreateTermAsync(Guid academicYearId, string code, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Term>> GetTermsAsync(Guid academicYearId, CancellationToken cancellationToken = default);

    /// <summary>Renames/reschedules/reorders a <c>Term</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateTermAsync(Guid id, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreatePhaseAsync(Guid schoolId, string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Phase>> GetPhasesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders a <c>Phase</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdatePhaseAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreateKeyStageAsync(Guid schoolId, Guid phaseId, string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KeyStage>> GetKeyStagesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders a <c>KeyStage</c>. Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateKeyStageAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreateGradeAsync(Guid schoolId, string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> GetGradesAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders a <c>Grade</c>. Code stays fixed (use <see cref="SetNextGradeAsync"/> to change promotion linkage). Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateGradeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task SetNextGradeAsync(Guid gradeId, Guid? nextGradeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationModel>> GetEvaluationModelsAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateEvaluationModelAsync(string code, string name, string? description, int displayOrder, CancellationToken cancellationToken = default);

    Task SetEvaluationModelActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Renames/reorders a <c>EvaluationModel</c>. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateEvaluationModelAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreateClassAsync(Guid schoolId, Guid? campusId, Guid academicYearId, string code, string name, IReadOnlyList<Guid> gradeIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Class>> GetClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default);

    /// <summary>Renames a <c>Class</c> and replaces its grade membership (required for combined classes, ORG-FR-018 - at least one grade). Code stays fixed. Throws <see cref="InvalidOperationException"/> if not found or if <paramref name="gradeIds"/> is empty.</summary>
    Task UpdateClassAsync(Guid id, string name, IReadOnlyList<Guid> gradeIds, CancellationToken cancellationToken = default);

    /// <summary>The grade(s) a <c>Class</c> is currently linked to via <c>ClassGrade</c> - needed to pre-populate an edit form, since <see cref="Class"/> itself doesn't carry its grade membership directly.</summary>
    Task<IReadOnlyList<Guid>> GetClassGradeIdsAsync(Guid classId, CancellationToken cancellationToken = default);

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

    /// <summary>Renames/reorders a <c>HolidayType</c>. Throws <see cref="InvalidOperationException"/> if not found.</summary>
    Task UpdateHolidayTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Holiday>> GetHolidaysAsync(Guid schoolId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No active holiday type with that code exists.</exception>
    Task<Guid> CreateHolidayAsync(Guid schoolId, DateOnly date, string holidayTypeCode, string nameEn, string nameDv, CancellationToken cancellationToken = default);
}

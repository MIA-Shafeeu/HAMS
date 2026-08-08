using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class OrgAdminService(OrgDbContext dbContext) : IOrgAdminService
{
    public async Task<Guid> CreateSchoolAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        var school = new School { Id = Guid.NewGuid(), Code = code, Name = name };
        dbContext.Schools.Add(school);

        // Default working week per the Maldivian school calendar (Sunday-Thursday) — real, editable
        // rows from the moment the school exists, never a hardcoded fallback in calendar logic.
        foreach (var dayOfWeek in OrgSeedData.DefaultWorkingDaysOfWeek)
        {
            dbContext.WorkingDays.Add(new WorkingDay { Id = Guid.NewGuid(), SchoolId = school.Id, DayOfWeek = dayOfWeek });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return school.Id;
    }

    public async Task<IReadOnlyList<School>> GetSchoolsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Schools.OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<Guid> CreateCampusAsync(Guid schoolId, string code, string name, CancellationToken cancellationToken = default)
    {
        var campus = new Campus { Id = Guid.NewGuid(), SchoolId = schoolId, Code = code, Name = name };
        dbContext.Campuses.Add(campus);
        await dbContext.SaveChangesAsync(cancellationToken);
        return campus.Id;
    }

    public async Task<IReadOnlyList<Campus>> GetCampusesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Campuses.Where(c => c.SchoolId == schoolId && c.IsActive).OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<Guid> CreateAcademicYearAsync(Guid schoolId, string code, string name, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var year = new AcademicYear { Id = Guid.NewGuid(), SchoolId = schoolId, Code = code, Name = name, StartDate = startDate, EndDate = endDate };
        dbContext.AcademicYears.Add(year);
        await dbContext.SaveChangesAsync(cancellationToken);
        return year.Id;
    }

    public async Task<IReadOnlyList<AcademicYear>> GetAcademicYearsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.AcademicYears.Where(a => a.SchoolId == schoolId).OrderByDescending(a => a.StartDate).ToListAsync(cancellationToken);

    public async Task<Guid> CreateTermAsync(Guid academicYearId, string code, string name, DateOnly startDate, DateOnly endDate, int displayOrder, CancellationToken cancellationToken = default)
    {
        var term = new Term { Id = Guid.NewGuid(), AcademicYearId = academicYearId, Code = code, Name = name, StartDate = startDate, EndDate = endDate, DisplayOrder = displayOrder };
        dbContext.Terms.Add(term);
        await dbContext.SaveChangesAsync(cancellationToken);
        return term.Id;
    }

    public async Task<IReadOnlyList<Term>> GetTermsAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.Terms.Where(t => t.AcademicYearId == academicYearId).OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreatePhaseAsync(Guid schoolId, string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var phase = new Phase { Id = Guid.NewGuid(), SchoolId = schoolId, Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.Phases.Add(phase);
        await dbContext.SaveChangesAsync(cancellationToken);
        return phase.Id;
    }

    public async Task<IReadOnlyList<Phase>> GetPhasesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Phases.Where(p => p.SchoolId == schoolId).OrderBy(p => p.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateKeyStageAsync(Guid schoolId, Guid phaseId, string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var keyStage = new KeyStage { Id = Guid.NewGuid(), SchoolId = schoolId, PhaseId = phaseId, Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.KeyStages.Add(keyStage);
        await dbContext.SaveChangesAsync(cancellationToken);
        return keyStage.Id;
    }

    public async Task<IReadOnlyList<KeyStage>> GetKeyStagesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.KeyStages.Where(k => k.SchoolId == schoolId).OrderBy(k => k.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateGradeAsync(Guid schoolId, string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var grade = new Grade { Id = Guid.NewGuid(), SchoolId = schoolId, Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.Grades.Add(grade);
        await dbContext.SaveChangesAsync(cancellationToken);
        return grade.Id;
    }

    public async Task<IReadOnlyList<Grade>> GetGradesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Grades.Where(g => g.SchoolId == schoolId).OrderBy(g => g.DisplayOrder).ToListAsync(cancellationToken);

    public async Task SetNextGradeAsync(Guid gradeId, Guid? nextGradeId, CancellationToken cancellationToken = default)
    {
        var grade = await dbContext.Grades.FindAsync([gradeId], cancellationToken)
            ?? throw new InvalidOperationException("Grade not found.");

        grade.NextGradeId = nextGradeId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvaluationModel>> GetEvaluationModelsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.EvaluationModels.OrderBy(m => m.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateEvaluationModelAsync(string code, string name, string? description, int displayOrder, CancellationToken cancellationToken = default)
    {
        var model = new EvaluationModel { Id = Guid.NewGuid(), Code = code, Name = name, Description = description, DisplayOrder = displayOrder };
        dbContext.EvaluationModels.Add(model);
        await dbContext.SaveChangesAsync(cancellationToken);
        return model.Id;
    }

    public async Task SetEvaluationModelActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.EvaluationModels.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Evaluation model not found.");

        model.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateEvaluationModelAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.EvaluationModels.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Evaluation model not found.");

        model.Name = name;
        model.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateClassAsync(Guid schoolId, Guid? campusId, Guid academicYearId, string code, string name, IReadOnlyList<Guid> gradeIds, CancellationToken cancellationToken = default)
    {
        var @class = new Class { Id = Guid.NewGuid(), SchoolId = schoolId, CampusId = campusId, AcademicYearId = academicYearId, Code = code, Name = name };
        dbContext.Classes.Add(@class);

        // Required for combined classes (ORG-FR-018) — a class always joins at least one grade.
        foreach (var gradeId in gradeIds)
        {
            dbContext.ClassGrades.Add(new ClassGrade { Id = Guid.NewGuid(), ClassId = @class.Id, GradeId = gradeId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return @class.Id;
    }

    public async Task<IReadOnlyList<Class>> GetClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default) =>
        await dbContext.Classes.Where(c => c.AcademicYearId == academicYearId).OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<Guid> CreateGradeKeyStageAssignmentAsync(Guid gradeId, Guid keyStageId, Guid academicYearId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
    {
        var assignment = new GradeKeyStageAssignment
        {
            Id = Guid.NewGuid(), GradeId = gradeId, KeyStageId = keyStageId, AcademicYearId = academicYearId,
            EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo,
        };
        dbContext.GradeKeyStageAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return assignment.Id;
    }

    public async Task<Guid> CreateKeyStagePolicyAsync(
        Guid keyStageId, Guid academicYearId, string evaluationModelCode,
        Guid? achievementScaleId, Guid? assessmentSchemeId, Guid? gradeScaleId, Guid? promotionPolicyId,
        CancellationToken cancellationToken = default)
    {
        var evaluationModel = await dbContext.EvaluationModels.SingleOrDefaultAsync(m => m.Code == evaluationModelCode && m.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active evaluation model with code '{evaluationModelCode}'.");

        var policy = new KeyStagePolicy
        {
            Id = Guid.NewGuid(),
            KeyStageId = keyStageId,
            AcademicYearId = academicYearId,
            EvaluationModelId = evaluationModel.Id,
            AchievementScaleId = achievementScaleId,
            AssessmentSchemeId = assessmentSchemeId,
            GradeScaleId = gradeScaleId,
            PromotionPolicyId = promotionPolicyId,
            Status = RecordStatus.Draft,
            IsCurrent = false,
        };
        dbContext.KeyStagePolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return policy.Id;
    }

    public async Task PublishKeyStagePolicyAsync(Guid keyStagePolicyId, CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.KeyStagePolicies.FindAsync([keyStagePolicyId], cancellationToken)
            ?? throw new InvalidOperationException("Key stage policy not found.");

        if (policy.Status != RecordStatus.Draft)
        {
            throw new InvalidOperationException("Only a Draft policy can be published.");
        }

        policy.Status = RecordStatus.Published;
        policy.IsCurrent = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KeyStagePolicy>> GetKeyStagePoliciesAsync(Guid keyStageId, CancellationToken cancellationToken = default) =>
        await dbContext.KeyStagePolicies.Where(p => p.KeyStageId == keyStageId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DayOfWeek>> GetWorkingDaysAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.WorkingDays.Where(w => w.SchoolId == schoolId).OrderBy(w => w.DayOfWeek).Select(w => w.DayOfWeek).ToListAsync(cancellationToken);

    public async Task SetWorkingDayAsync(Guid schoolId, DayOfWeek dayOfWeek, bool isWorkingDay, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.WorkingDays.SingleOrDefaultAsync(w => w.SchoolId == schoolId && w.DayOfWeek == dayOfWeek, cancellationToken);

        if (isWorkingDay && existing is null)
        {
            dbContext.WorkingDays.Add(new WorkingDay { Id = Guid.NewGuid(), SchoolId = schoolId, DayOfWeek = dayOfWeek });
        }
        else if (!isWorkingDay && existing is not null)
        {
            dbContext.WorkingDays.Remove(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HolidayType>> GetHolidayTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.HolidayTypes.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task<Guid> CreateHolidayTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var holidayType = new HolidayType { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.HolidayTypes.Add(holidayType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return holidayType.Id;
    }

    public async Task SetHolidayTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var holidayType = await dbContext.HolidayTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Holiday type not found.");

        holidayType.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateHolidayTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var holidayType = await dbContext.HolidayTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Holiday type not found.");

        holidayType.Name = name;
        holidayType.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Holiday>> GetHolidaysAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Holidays.Where(h => h.SchoolId == schoolId).OrderBy(h => h.Date).ToListAsync(cancellationToken);

    public async Task<Guid> CreateHolidayAsync(Guid schoolId, DateOnly date, string holidayTypeCode, string nameEn, string nameDv, CancellationToken cancellationToken = default)
    {
        var holidayType = await dbContext.HolidayTypes.SingleOrDefaultAsync(t => t.Code == holidayTypeCode && t.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active holiday type with code '{holidayTypeCode}'.");

        var holiday = new Holiday { Id = Guid.NewGuid(), SchoolId = schoolId, Date = date, HolidayTypeId = holidayType.Id, NameEn = nameEn, NameDv = nameDv };
        dbContext.Holidays.Add(holiday);
        await dbContext.SaveChangesAsync(cancellationToken);
        return holiday.Id;
    }
}

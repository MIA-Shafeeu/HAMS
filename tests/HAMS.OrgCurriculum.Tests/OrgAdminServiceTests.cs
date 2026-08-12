using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Tests;

public class OrgAdminServiceTests
{
    private static OrgDbContext CreateContext() => new(
        new DbContextOptionsBuilder<OrgDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateSchoolAsync_seeds_the_default_Sunday_to_Thursday_working_week()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);

        var schoolId = await service.CreateSchoolAsync("HES", "Hirilandhoo School");

        var workingDays = await service.GetWorkingDaysAsync(schoolId);
        Assert.Equal(
            [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
            workingDays.OrderBy(d => d).ToList());
    }

    [Fact]
    public async Task GetSchoolsAsync_returns_created_schools_ordered_by_name()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        await service.CreateSchoolAsync("B", "Beta School");
        await service.CreateSchoolAsync("A", "Alpha School");

        var schools = await service.GetSchoolsAsync();

        Assert.Equal(["Alpha School", "Beta School"], schools.Select(s => s.Name));
    }

    [Fact]
    public async Task SetNextGradeAsync_updates_the_configured_promotion_default()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        var schoolId = await service.CreateSchoolAsync("HES", "School");
        var grade5Id = await service.CreateGradeAsync(schoolId, "G5", "Grade 5", 5);
        var grade6Id = await service.CreateGradeAsync(schoolId, "G6", "Grade 6", 6);

        await service.SetNextGradeAsync(grade5Id, grade6Id);

        var grades = await service.GetGradesAsync(schoolId);
        Assert.Equal(grade6Id, grades.Single(g => g.Id == grade5Id).NextGradeId);
    }

    [Fact]
    public async Task SetNextGradeAsync_throws_for_an_unknown_grade()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetNextGradeAsync(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task CreateClassAsync_creates_a_ClassGrade_row_for_every_grade_supplied()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        var schoolId = await service.CreateSchoolAsync("HES", "School");
        var academicYearId = await service.CreateAcademicYearAsync(schoolId, "2026", "2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var grade5Id = await service.CreateGradeAsync(schoolId, "G5", "Grade 5", 5);
        var grade6Id = await service.CreateGradeAsync(schoolId, "G6", "Grade 6", 6);

        var classId = await service.CreateClassAsync(schoolId, null, academicYearId, "5-6C", "Grade 5/6 Combined", "#3B82F6", [grade5Id, grade6Id]);

        var classGrades = await db.ClassGrades.Where(cg => cg.ClassId == classId).ToListAsync();
        Assert.Equal(2, classGrades.Count);
        Assert.Contains(classGrades, cg => cg.GradeId == grade5Id);
        Assert.Contains(classGrades, cg => cg.GradeId == grade6Id);
    }

    [Fact]
    public async Task CreateEvaluationModelAsync_creates_a_retrievable_evaluation_model()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);

        var modelId = await service.CreateEvaluationModelAsync("HYBRID2", "Hybrid Model", "A blended model.", 3);

        var models = await service.GetEvaluationModelsAsync();
        Assert.Contains(models, m => m.Id == modelId && m.Name == "Hybrid Model" && m.Description == "A blended model.");
    }

    [Fact]
    public async Task GetEvaluationModelsAsync_orders_by_DisplayOrder()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        await service.CreateEvaluationModelAsync("B", "Beta", null, 2);
        await service.CreateEvaluationModelAsync("A", "Alpha", null, 1);

        var models = await service.GetEvaluationModelsAsync();

        Assert.Equal(["Alpha", "Beta"], models.Select(m => m.Name));
    }

    [Fact]
    public async Task SetEvaluationModelActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        var modelId = await service.CreateEvaluationModelAsync("MASTERY", "Mastery", null, 1);

        await service.SetEvaluationModelActiveAsync(modelId, false);

        var models = await service.GetEvaluationModelsAsync();
        Assert.False(models.Single(m => m.Id == modelId).IsActive);
    }

    [Fact]
    public async Task SetEvaluationModelActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetEvaluationModelActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task CreateKeyStagePolicyAsync_creates_a_Draft_not_current_policy()
    {
        await using var db = CreateContext();
        db.EvaluationModels.Add(new EvaluationModel { Id = Guid.NewGuid(), Code = "MASTERY", Name = "Mastery", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrgAdminService(db);
        var keyStageId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();

        var policyId = await service.CreateKeyStagePolicyAsync(keyStageId, academicYearId, "MASTERY", null, null, null, null);

        var policy = await db.KeyStagePolicies.SingleAsync(p => p.Id == policyId);
        Assert.Equal(RecordStatus.Draft, policy.Status);
        Assert.False(policy.IsCurrent);
    }

    [Fact]
    public async Task CreateKeyStagePolicyAsync_throws_for_an_unknown_or_inactive_evaluation_model_code()
    {
        await using var db = CreateContext();
        db.EvaluationModels.Add(new EvaluationModel { Id = Guid.NewGuid(), Code = "RETIRED", Name = "Retired", IsActive = false });
        await db.SaveChangesAsync();
        var service = new OrgAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateKeyStagePolicyAsync(Guid.NewGuid(), Guid.NewGuid(), "RETIRED", null, null, null, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateKeyStagePolicyAsync(Guid.NewGuid(), Guid.NewGuid(), "NONEXISTENT", null, null, null, null));
    }

    [Fact]
    public async Task PublishKeyStagePolicyAsync_flips_Draft_to_Published_and_sets_IsCurrent()
    {
        await using var db = CreateContext();
        db.EvaluationModels.Add(new EvaluationModel { Id = Guid.NewGuid(), Code = "MASTERY", Name = "Mastery", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrgAdminService(db);
        var policyId = await service.CreateKeyStagePolicyAsync(Guid.NewGuid(), Guid.NewGuid(), "MASTERY", null, null, null, null);

        await service.PublishKeyStagePolicyAsync(policyId);

        var policy = await db.KeyStagePolicies.SingleAsync(p => p.Id == policyId);
        Assert.Equal(RecordStatus.Published, policy.Status);
        Assert.True(policy.IsCurrent);
    }

    [Fact]
    public async Task PublishKeyStagePolicyAsync_rejects_publishing_the_same_policy_twice()
    {
        await using var db = CreateContext();
        db.EvaluationModels.Add(new EvaluationModel { Id = Guid.NewGuid(), Code = "MASTERY", Name = "Mastery", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrgAdminService(db);
        var policyId = await service.CreateKeyStagePolicyAsync(Guid.NewGuid(), Guid.NewGuid(), "MASTERY", null, null, null, null);
        await service.PublishKeyStagePolicyAsync(policyId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishKeyStagePolicyAsync(policyId));
    }

    [Fact]
    public async Task SetWorkingDayAsync_adds_and_removes_a_day_idempotently()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        var schoolId = await service.CreateSchoolAsync("HES", "School");

        await service.SetWorkingDayAsync(schoolId, DayOfWeek.Friday, isWorkingDay: true);
        Assert.Contains(DayOfWeek.Friday, await service.GetWorkingDaysAsync(schoolId));

        await service.SetWorkingDayAsync(schoolId, DayOfWeek.Thursday, isWorkingDay: false);
        Assert.DoesNotContain(DayOfWeek.Thursday, await service.GetWorkingDaysAsync(schoolId));

        // Removing an already-absent day, or adding an already-present one, must not throw or duplicate.
        await service.SetWorkingDayAsync(schoolId, DayOfWeek.Thursday, isWorkingDay: false);
        await service.SetWorkingDayAsync(schoolId, DayOfWeek.Friday, isWorkingDay: true);
        Assert.Single(await service.GetWorkingDaysAsync(schoolId), d => d == DayOfWeek.Friday);
    }

    [Fact]
    public async Task CreateHolidayTypeAsync_creates_a_retrievable_holiday_type()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);

        var typeId = await service.CreateHolidayTypeAsync("STAFF_DAY", "Staff Training Day", 4);

        var types = await service.GetHolidayTypesAsync();
        Assert.Contains(types, t => t.Id == typeId && t.Name == "Staff Training Day");
    }

    [Fact]
    public async Task SetHolidayTypeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        var typeId = await service.CreateHolidayTypeAsync("STAFF_DAY", "Staff Training Day", 4);

        await service.SetHolidayTypeActiveAsync(typeId, false);

        var types = await service.GetHolidayTypesAsync();
        Assert.False(types.Single(t => t.Id == typeId).IsActive);
    }

    [Fact]
    public async Task SetHolidayTypeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetHolidayTypeActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task CreateHolidayAsync_resolves_the_holiday_type_by_code()
    {
        await using var db = CreateContext();
        var holidayTypeId = Guid.NewGuid();
        db.HolidayTypes.Add(new HolidayType { Id = holidayTypeId, Code = HolidayTypeCodes.PublicHoliday, Name = "Public Holiday", IsActive = true });
        await db.SaveChangesAsync();
        var service = new OrgAdminService(db);
        var schoolId = await service.CreateSchoolAsync("HES", "School");

        var holidayId = await service.CreateHolidayAsync(schoolId, new DateOnly(2026, 11, 11), HolidayTypeCodes.PublicHoliday, "Republic Day", "ޖުމްހޫރީ ދުވަސް");

        var holidays = await service.GetHolidaysAsync(schoolId);
        var holiday = Assert.Single(holidays);
        Assert.Equal(holidayId, holiday.Id);
        Assert.Equal(holidayTypeId, holiday.HolidayTypeId);
    }

    [Fact]
    public async Task CreateHolidayAsync_throws_for_an_unknown_holiday_type_code()
    {
        await using var db = CreateContext();
        var service = new OrgAdminService(db);
        var schoolId = await service.CreateSchoolAsync("HES", "School");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateHolidayAsync(schoolId, new DateOnly(2026, 11, 11), "NONEXISTENT", "x", "x"));
    }
}

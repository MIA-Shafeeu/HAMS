using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Tests;

public class CurriculumAdminServiceTests
{
    private static OrgDbContext CreateContext() => new(
        new DbContextOptionsBuilder<OrgDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateCurriculumFrameworkAsync_creates_a_retrievable_framework()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);

        var frameworkId = await service.CreateCurriculumFrameworkAsync("NCF2", "National Curriculum Framework 2", "A future revision.");

        var frameworks = await service.GetCurriculumFrameworksAsync();
        Assert.Contains(frameworks, f => f.Id == frameworkId && f.Name == "National Curriculum Framework 2");
    }

    [Fact]
    public async Task GetCurriculumFrameworksAsync_includes_the_seeded_National_Curriculum_Framework()
    {
        await using var db = CreateContext();
        db.CurriculumFrameworks.Add(new CurriculumFramework { Id = Guid.NewGuid(), Code = "NCF", Name = "National Curriculum Framework" });
        await db.SaveChangesAsync();
        var service = new CurriculumAdminService(db);

        var frameworks = await service.GetCurriculumFrameworksAsync();

        Assert.Single(frameworks, f => f.Code == "NCF");
    }

    [Fact]
    public async Task CreateLearningAreaAsync_links_to_the_given_framework()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);
        var frameworkId = await service.CreateCurriculumFrameworkAsync("NCF", "National Curriculum Framework", null);

        var learningAreaId = await service.CreateLearningAreaAsync(frameworkId, "MATH", "Mathematics", 1);

        var learningAreas = await service.GetLearningAreasAsync();
        var learningArea = Assert.Single(learningAreas, a => a.Id == learningAreaId);
        Assert.Equal(frameworkId, learningArea.CurriculumFrameworkId);
    }

    [Fact]
    public async Task CreateDeliveryModeAsync_creates_a_retrievable_delivery_mode()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);

        var modeId = await service.CreateDeliveryModeAsync("BLENDED", "Blended", 3);

        var modes = await service.GetDeliveryModesAsync();
        Assert.Contains(modes, m => m.Id == modeId && m.Name == "Blended");
    }

    [Fact]
    public async Task SetDeliveryModeActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);
        var modeId = await service.CreateDeliveryModeAsync("BLENDED", "Blended", 3);
        Assert.Contains(await service.GetDeliveryModesAsync(), m => m.Id == modeId);

        await service.SetDeliveryModeActiveAsync(modeId, false);

        // GetDeliveryModesAsync only returns active rows, so a deactivated mode drops out of the list.
        Assert.DoesNotContain(await service.GetDeliveryModesAsync(), m => m.Id == modeId);
        var mode = await db.DeliveryModes.SingleAsync(m => m.Id == modeId);
        Assert.False(mode.IsActive);
    }

    [Fact]
    public async Task SetDeliveryModeActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetDeliveryModeActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task GetAllDeliveryModesAsync_includes_inactive_modes_unlike_GetDeliveryModesAsync()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);
        var modeId = await service.CreateDeliveryModeAsync("BLENDED", "Blended", 3);
        await service.SetDeliveryModeActiveAsync(modeId, false);

        Assert.Contains(await service.GetAllDeliveryModesAsync(), m => m.Id == modeId);
    }

    [Fact]
    public async Task CreateMediumOfInstructionAsync_creates_a_retrievable_medium()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);

        var mediumId = await service.CreateMediumOfInstructionAsync("ARABIC", "Arabic", 3);

        var mediums = await service.GetMediumsOfInstructionAsync();
        Assert.Contains(mediums, m => m.Id == mediumId && m.Name == "Arabic");
    }

    [Fact]
    public async Task SetMediumOfInstructionActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);
        var mediumId = await service.CreateMediumOfInstructionAsync("ARABIC", "Arabic", 3);

        await service.SetMediumOfInstructionActiveAsync(mediumId, false);

        Assert.DoesNotContain(await service.GetMediumsOfInstructionAsync(), m => m.Id == mediumId);
        var medium = await db.MediumsOfInstruction.SingleAsync(m => m.Id == mediumId);
        Assert.False(medium.IsActive);
    }

    [Fact]
    public async Task SetMediumOfInstructionActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetMediumOfInstructionActiveAsync(Guid.NewGuid(), false));
    }

    [Fact]
    public async Task GetAllMediumsOfInstructionAsync_includes_inactive_mediums_unlike_GetMediumsOfInstructionAsync()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);
        var mediumId = await service.CreateMediumOfInstructionAsync("ARABIC", "Arabic", 3);
        await service.SetMediumOfInstructionActiveAsync(mediumId, false);

        Assert.Contains(await service.GetAllMediumsOfInstructionAsync(), m => m.Id == mediumId);
    }

    [Fact]
    public async Task CreateSubjectAsync_resolves_delivery_mode_and_medium_of_instruction_by_code()
    {
        await using var db = CreateContext();
        var deliveryModeId = Guid.NewGuid();
        var mediumId = Guid.NewGuid();
        db.DeliveryModes.Add(new DeliveryMode { Id = deliveryModeId, Code = DeliveryModeCodes.Timetabled, Name = "Timetabled", IsActive = true });
        db.MediumsOfInstruction.Add(new MediumOfInstruction { Id = mediumId, Code = MediumOfInstructionCodes.English, Name = "English", IsActive = true });
        await db.SaveChangesAsync();
        var service = new CurriculumAdminService(db);
        var frameworkId = await service.CreateCurriculumFrameworkAsync("NCF", "National Curriculum Framework", null);
        var learningAreaId = await service.CreateLearningAreaAsync(frameworkId, "MATH", "Mathematics", 1);
        var schoolId = Guid.NewGuid();

        var subjectId = await service.CreateSubjectAsync(
            schoolId, learningAreaId, "MATH101", "Mathematics", DeliveryModeCodes.Timetabled, MediumOfInstructionCodes.English, 1);

        var subject = Assert.Single(await service.GetSubjectsAsync(schoolId));
        Assert.Equal(subjectId, subject.Id);
        Assert.Equal(deliveryModeId, subject.DeliveryModeId);
        Assert.Equal(mediumId, subject.DefaultMediumOfInstructionId);
    }

    [Fact]
    public async Task CreateSubjectAsync_throws_for_an_unknown_delivery_mode_code()
    {
        await using var db = CreateContext();
        db.MediumsOfInstruction.Add(new MediumOfInstruction { Id = Guid.NewGuid(), Code = MediumOfInstructionCodes.English, Name = "English", IsActive = true });
        await db.SaveChangesAsync();
        var service = new CurriculumAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSubjectAsync(Guid.NewGuid(), Guid.NewGuid(), "X", "X", "NONEXISTENT", MediumOfInstructionCodes.English, 1));
    }

    [Fact]
    public async Task CreateSubjectAsync_throws_for_an_unknown_medium_of_instruction_code()
    {
        await using var db = CreateContext();
        db.DeliveryModes.Add(new DeliveryMode { Id = Guid.NewGuid(), Code = DeliveryModeCodes.Timetabled, Name = "Timetabled", IsActive = true });
        await db.SaveChangesAsync();
        var service = new CurriculumAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSubjectAsync(Guid.NewGuid(), Guid.NewGuid(), "X", "X", DeliveryModeCodes.Timetabled, "NONEXISTENT", 1));
    }

    [Fact]
    public async Task GetSyllabusesForSubjectAsync_orders_by_version_descending()
    {
        await using var db = CreateContext();
        var subjectId = Guid.NewGuid();
        db.Syllabuses.Add(new Syllabus { Id = Guid.NewGuid(), SubjectId = subjectId, Version = 1, IsCurrent = false });
        db.Syllabuses.Add(new Syllabus { Id = Guid.NewGuid(), SubjectId = subjectId, Version = 2, IsCurrent = true });
        await db.SaveChangesAsync();
        var service = new CurriculumAdminService(db);

        var syllabuses = await service.GetSyllabusesForSubjectAsync(subjectId);

        Assert.Equal([2, 1], syllabuses.Select(s => s.Version));
    }

    [Fact]
    public async Task AddSyllabusGradeApplicabilityAsync_is_retrievable_via_GetSyllabusGradeApplicabilitiesAsync()
    {
        await using var db = CreateContext();
        var service = new CurriculumAdminService(db);
        var syllabusId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();

        await service.AddSyllabusGradeApplicabilityAsync(syllabusId, gradeId);

        var applicabilities = await service.GetSyllabusGradeApplicabilitiesAsync(syllabusId);
        Assert.Single(applicabilities, a => a.GradeId == gradeId);
    }
}

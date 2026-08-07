using HAMS.OrgCurriculum.Domain;

namespace HAMS.OrgCurriculum.Infrastructure;

/// <summary>
/// Fixed reference data seeded via EF Core migrations — see <c>AccessSeedData</c> for the same
/// pattern. Only genuinely school-independent universal lookups are seeded here (no
/// <c>SchoolId</c>); <c>Subject</c>/<c>Syllabus</c>/its content tree are ordinary admin-authored
/// data created at runtime through the API, exactly like <c>School</c>/<c>AcademicYear</c>/
/// <c>Grade</c> in Phase 1 — never HasData, since they depend on a real School existing first.
/// </summary>
internal static class OrgSeedData
{
    /// <summary>Referenced by <see cref="LearningAreas"/> below.</summary>
    public static readonly Guid NationalCurriculumFrameworkId = new("00000000-0000-0000-0004-000000000001");

    public static readonly EvaluationModel[] EvaluationModels =
    [
        new() { Id = new Guid("00000000-0000-0000-0003-000000000001"), Code = EvaluationModelCodes.Mastery, Name = "Mastery", Description = "Continuous learning-outcome mastery only.", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0003-000000000002"), Code = EvaluationModelCodes.Assessment, Name = "Assessment", Description = "External syndicated summative examination only.", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0003-000000000003"), Code = EvaluationModelCodes.Hybrid, Name = "Hybrid", Description = "Continuous assessment combined with a time-boxed exam.", DisplayOrder = 3 },
    ];

    public static readonly CurriculumFramework[] CurriculumFrameworks =
    [
        new() { Id = NationalCurriculumFrameworkId, Code = "NCF", Name = "National Curriculum Framework", Description = "Maldives National Curriculum Framework." },
    ];

    /// <summary>The NCF's eight Key Learning Areas — a real, sourced set (build plan Phase 2 notes), not invented.</summary>
    public static readonly LearningArea[] LearningAreas =
    [
        new() { Id = new Guid("00000000-0000-0000-0005-000000000001"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "ISLAM_SPIRITUALITY", Name = "Islam & Spirituality", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0005-000000000002"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "LANGUAGE_COMMUNICATION", Name = "Language & Communication", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0005-000000000003"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "MATHEMATICS", Name = "Mathematics", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0005-000000000004"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "ENVIRONMENT_SCIENCE_TECHNOLOGY", Name = "Environment/Science & Technology", DisplayOrder = 4 },
        new() { Id = new Guid("00000000-0000-0000-0005-000000000005"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "HEALTH_WELLBEING", Name = "Health & Wellbeing", DisplayOrder = 5 },
        new() { Id = new Guid("00000000-0000-0000-0005-000000000006"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "SOCIAL_SCIENCES", Name = "Social Sciences", DisplayOrder = 6 },
        new() { Id = new Guid("00000000-0000-0000-0005-000000000007"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "CREATIVE_ARTS", Name = "Creative Arts", DisplayOrder = 7 },
        new() { Id = new Guid("00000000-0000-0000-0005-000000000008"), CurriculumFrameworkId = NationalCurriculumFrameworkId, Code = "ENTREPRENEURSHIP", Name = "Entrepreneurship", DisplayOrder = 8 },
    ];

    public static readonly DeliveryMode[] DeliveryModes =
    [
        new() { Id = new Guid("00000000-0000-0000-0006-000000000001"), Code = DeliveryModeCodes.Timetabled, Name = "Timetabled", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0006-000000000002"), Code = DeliveryModeCodes.Integrated, Name = "Integrated", DisplayOrder = 2 },
    ];

    public static readonly MediumOfInstruction[] MediumsOfInstruction =
    [
        new() { Id = new Guid("00000000-0000-0000-0007-000000000001"), Code = MediumOfInstructionCodes.Dhivehi, Name = "Dhivehi", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0007-000000000002"), Code = MediumOfInstructionCodes.English, Name = "English", DisplayOrder = 2 },
    ];

    /// <summary>The Maldivian default working week — Sunday-Thursday — seeded onto a school at creation time (see <c>OrgEndpoints</c>), never hardcoded into calendar logic itself.</summary>
    public static readonly DayOfWeek[] DefaultWorkingDaysOfWeek =
    [
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
    ];

    public static readonly HolidayType[] HolidayTypes =
    [
        new() { Id = new Guid("00000000-0000-0000-0014-000000000001"), Code = HolidayTypeCodes.PublicHoliday, Name = "Public Holiday", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0014-000000000002"), Code = HolidayTypeCodes.ReligiousHoliday, Name = "Religious Holiday", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0014-000000000003"), Code = HolidayTypeCodes.SchoolDeclared, Name = "School-Declared Holiday", DisplayOrder = 3 },
    ];
}

using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class LearningEvidenceServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Guid> SeedEvidenceTypeAsync(LearningDeliveryDbContext db)
    {
        var type = new EvidenceType { Id = Guid.NewGuid(), Code = EvidenceTypeCodes.Observation, Name = "Observation", IsActive = true };
        db.EvidenceTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }

    private static async Task<Guid> SeedAchievementLevelAsync(LearningDeliveryDbContext db, bool isActive = true)
    {
        var scale = new AchievementScale { Id = Guid.NewGuid(), Code = "SCALE", Name = "Scale" };
        db.AchievementScales.Add(scale);
        var level = new AchievementLevel { Id = Guid.NewGuid(), AchievementScaleId = scale.Id, Code = "L1", Name = "Level 1", IsActive = isActive };
        db.AchievementLevels.Add(level);
        await db.SaveChangesAsync();
        return level.Id;
    }

    private static async Task<Guid> SeedLessonSessionAsync(LearningDeliveryDbContext db, LessonSessionStatus status)
    {
        var session = new LessonSession
        {
            Id = Guid.NewGuid(), LessonPlanId = Guid.NewGuid(), ClassId = Guid.NewGuid(),
            ActualDate = new DateOnly(2026, 1, 4), PeriodId = Guid.NewGuid(), Status = status,
        };
        db.LessonSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    [Fact]
    public async Task RecordAsync_records_evidence_with_no_lesson_session_tie()
    {
        await using var db = CreateContext();
        var evidenceTypeId = await SeedEvidenceTypeAsync(db);
        var levelId = await SeedAchievementLevelAsync(db);
        var service = new LearningEvidenceService(db);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();

        var evidenceId = await service.RecordAsync(
            studentId, outcomeId, lessonSessionId: null, EvidenceTypeCodes.Observation, levelId,
            new DateOnly(2026, 1, 4), Guid.NewGuid(), "did well");

        var evidence = await db.LearningEvidences.SingleAsync(e => e.Id == evidenceId);
        Assert.Equal(studentId, evidence.StudentPersonId);
        Assert.Equal(evidenceTypeId, evidence.EvidenceTypeId);
        Assert.Equal("did well", evidence.Notes);
    }

    [Fact]
    public async Task RecordAsync_accepts_a_lesson_session_that_is_Completed()
    {
        await using var db = CreateContext();
        await SeedEvidenceTypeAsync(db);
        var levelId = await SeedAchievementLevelAsync(db);
        var sessionId = await SeedLessonSessionAsync(db, LessonSessionStatus.Completed);
        var service = new LearningEvidenceService(db);

        var evidenceId = await service.RecordAsync(
            Guid.NewGuid(), Guid.NewGuid(), sessionId, EvidenceTypeCodes.Observation, levelId,
            new DateOnly(2026, 1, 4), Guid.NewGuid(), null);

        Assert.NotEqual(Guid.Empty, evidenceId);
    }

    [Fact]
    public async Task RecordAsync_rejects_a_lesson_session_that_is_still_Planned()
    {
        await using var db = CreateContext();
        await SeedEvidenceTypeAsync(db);
        var levelId = await SeedAchievementLevelAsync(db);
        var sessionId = await SeedLessonSessionAsync(db, LessonSessionStatus.Planned);
        var service = new LearningEvidenceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), sessionId, EvidenceTypeCodes.Observation, levelId, new DateOnly(2026, 1, 4), Guid.NewGuid(), null));
    }

    [Fact]
    public async Task RecordAsync_rejects_an_unknown_lesson_session()
    {
        await using var db = CreateContext();
        await SeedEvidenceTypeAsync(db);
        var levelId = await SeedAchievementLevelAsync(db);
        var service = new LearningEvidenceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EvidenceTypeCodes.Observation, levelId, new DateOnly(2026, 1, 4), Guid.NewGuid(), null));
    }

    [Fact]
    public async Task RecordAsync_rejects_an_unknown_evidence_type_code()
    {
        await using var db = CreateContext();
        var levelId = await SeedAchievementLevelAsync(db);
        var service = new LearningEvidenceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), null, "NOT_A_REAL_CODE", levelId, new DateOnly(2026, 1, 4), Guid.NewGuid(), null));
    }

    [Fact]
    public async Task RecordAsync_rejects_an_inactive_achievement_level()
    {
        await using var db = CreateContext();
        await SeedEvidenceTypeAsync(db);
        var inactiveLevelId = await SeedAchievementLevelAsync(db, isActive: false);
        var service = new LearningEvidenceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), null, EvidenceTypeCodes.Observation, inactiveLevelId, new DateOnly(2026, 1, 4), Guid.NewGuid(), null));
    }
}

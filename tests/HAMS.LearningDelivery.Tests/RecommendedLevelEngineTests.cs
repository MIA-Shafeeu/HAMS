using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class RecommendedLevelEngineTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Guid ScaleId, Guid Low, Guid Mid, Guid High)> SeedScaleAsync(LearningDeliveryDbContext db, int minimumEvidenceCount)
    {
        var scale = new AchievementScale { Id = Guid.NewGuid(), Code = "TEST_SCALE", Name = "Test Scale", MinimumEvidenceCount = minimumEvidenceCount };
        db.AchievementScales.Add(scale);
        var low = new AchievementLevel { Id = Guid.NewGuid(), AchievementScaleId = scale.Id, Code = "LOW", Name = "Working Towards", Rank = 1 };
        var mid = new AchievementLevel { Id = Guid.NewGuid(), AchievementScaleId = scale.Id, Code = "MID", Name = "Working At", Rank = 2 };
        var high = new AchievementLevel { Id = Guid.NewGuid(), AchievementScaleId = scale.Id, Code = "HIGH", Name = "Working Beyond", Rank = 3 };
        db.AchievementLevels.AddRange(low, mid, high);
        await db.SaveChangesAsync();
        return (scale.Id, low.Id, mid.Id, high.Id);
    }

    private static void AddEvidence(LearningDeliveryDbContext db, Guid studentId, Guid outcomeId, Guid levelId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            db.LearningEvidences.Add(new LearningEvidence
            {
                Id = Guid.NewGuid(), StudentPersonId = studentId, LearningOutcomeId = outcomeId, EvidenceTypeId = Guid.NewGuid(),
                AchievementLevelId = levelId, RecordedByPersonId = Guid.NewGuid(), RecordedDate = new DateOnly(2026, 1, 4),
            });
        }
    }

    [Fact]
    public async Task RecommendAsync_is_insufficient_when_evidence_count_is_below_the_scales_minimum()
    {
        await using var db = CreateContext();
        var (scaleId, low, _, _) = await SeedScaleAsync(db, minimumEvidenceCount: 3);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        AddEvidence(db, studentId, outcomeId, low, count: 2);
        await db.SaveChangesAsync();
        var engine = new RecommendedLevelEngine(db);

        var result = await engine.RecommendAsync(studentId, outcomeId, scaleId);

        Assert.False(result.IsSufficient);
        Assert.Null(result.RecommendedAchievementLevelId);
        Assert.Equal(2, result.EvidenceCount);
    }

    [Fact]
    public async Task RecommendAsync_recommends_the_most_frequently_demonstrated_level_once_sufficient()
    {
        await using var db = CreateContext();
        var (scaleId, low, mid, _) = await SeedScaleAsync(db, minimumEvidenceCount: 3);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        AddEvidence(db, studentId, outcomeId, mid, count: 2);
        AddEvidence(db, studentId, outcomeId, low, count: 1);
        await db.SaveChangesAsync();
        var engine = new RecommendedLevelEngine(db);

        var result = await engine.RecommendAsync(studentId, outcomeId, scaleId);

        Assert.True(result.IsSufficient);
        Assert.Equal(mid, result.RecommendedAchievementLevelId);
        Assert.Equal(3, result.EvidenceCount);
    }

    [Fact]
    public async Task RecommendAsync_breaks_a_tie_toward_the_lower_ranked_level()
    {
        await using var db = CreateContext();
        var (scaleId, low, _, high) = await SeedScaleAsync(db, minimumEvidenceCount: 2);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        AddEvidence(db, studentId, outcomeId, high, count: 1);
        AddEvidence(db, studentId, outcomeId, low, count: 1);
        await db.SaveChangesAsync();
        var engine = new RecommendedLevelEngine(db);

        var result = await engine.RecommendAsync(studentId, outcomeId, scaleId);

        Assert.True(result.IsSufficient);
        Assert.Equal(low, result.RecommendedAchievementLevelId);
    }

    [Fact]
    public async Task RecommendAsync_ignores_evidence_belonging_to_a_different_student_or_outcome()
    {
        await using var db = CreateContext();
        var (scaleId, low, mid, _) = await SeedScaleAsync(db, minimumEvidenceCount: 1);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        AddEvidence(db, studentId, outcomeId, mid, count: 1);
        AddEvidence(db, Guid.NewGuid(), outcomeId, low, count: 5); // a different student
        AddEvidence(db, studentId, Guid.NewGuid(), low, count: 5); // a different outcome
        await db.SaveChangesAsync();
        var engine = new RecommendedLevelEngine(db);

        var result = await engine.RecommendAsync(studentId, outcomeId, scaleId);

        Assert.True(result.IsSufficient);
        Assert.Equal(1, result.EvidenceCount);
        Assert.Equal(mid, result.RecommendedAchievementLevelId);
    }
}

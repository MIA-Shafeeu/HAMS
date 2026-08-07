using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => today.ToDateTime(TimeOnly.MinValue);
    public DateOnly TodayUtc => today;
}

public class MasteryEvaluationServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Guid ScaleId, Guid Low, Guid Mid)> SeedScaleAsync(LearningDeliveryDbContext db, int minimumEvidenceCount)
    {
        var scale = new AchievementScale { Id = Guid.NewGuid(), Code = "TEST_SCALE", Name = "Test Scale", MinimumEvidenceCount = minimumEvidenceCount };
        db.AchievementScales.Add(scale);
        var low = new AchievementLevel { Id = Guid.NewGuid(), AchievementScaleId = scale.Id, Code = "LOW", Name = "Working Towards", Rank = 1 };
        var mid = new AchievementLevel { Id = Guid.NewGuid(), AchievementScaleId = scale.Id, Code = "MID", Name = "Working At", Rank = 2 };
        db.AchievementLevels.AddRange(low, mid);
        await db.SaveChangesAsync();
        return (scale.Id, low.Id, mid.Id);
    }

    private static MasteryEvaluationService CreateService(LearningDeliveryDbContext db, DateOnly? today = null)
        => new(db, new RecommendedLevelEngine(db), new FakeClock(today ?? new DateOnly(2026, 1, 4)));

    [Fact]
    public async Task RecordEvaluationAsync_throws_when_evidence_is_insufficient_and_no_manual_override_is_given()
    {
        await using var db = CreateContext();
        var (scaleId, low, _) = await SeedScaleAsync(db, minimumEvidenceCount: 3);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        db.LearningEvidences.Add(new LearningEvidence
        {
            Id = Guid.NewGuid(), StudentPersonId = studentId, LearningOutcomeId = outcomeId, EvidenceTypeId = Guid.NewGuid(),
            AchievementLevelId = low, RecordedByPersonId = Guid.NewGuid(), RecordedDate = new DateOnly(2026, 1, 4),
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordEvaluationAsync(studentId, outcomeId, Guid.NewGuid(), scaleId, Guid.NewGuid(), manualAchievementLevelId: null));
    }

    [Fact]
    public async Task RecordEvaluationAsync_records_the_recommended_level_when_evidence_is_sufficient()
    {
        await using var db = CreateContext();
        var (scaleId, low, mid) = await SeedScaleAsync(db, minimumEvidenceCount: 2);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        var keyStagePolicyId = Guid.NewGuid();
        db.LearningEvidences.AddRange(
            new LearningEvidence { Id = Guid.NewGuid(), StudentPersonId = studentId, LearningOutcomeId = outcomeId, EvidenceTypeId = Guid.NewGuid(), AchievementLevelId = mid, RecordedByPersonId = Guid.NewGuid(), RecordedDate = new DateOnly(2026, 1, 4) },
            new LearningEvidence { Id = Guid.NewGuid(), StudentPersonId = studentId, LearningOutcomeId = outcomeId, EvidenceTypeId = Guid.NewGuid(), AchievementLevelId = mid, RecordedByPersonId = Guid.NewGuid(), RecordedDate = new DateOnly(2026, 1, 4) });
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var recordedBy = Guid.NewGuid();

        var evaluationId = await service.RecordEvaluationAsync(studentId, outcomeId, keyStagePolicyId, scaleId, recordedBy, manualAchievementLevelId: null);

        var evaluation = await db.MasteryEvaluations.SingleAsync(e => e.Id == evaluationId);
        Assert.Equal(mid, evaluation.AchievementLevelId);
        Assert.False(evaluation.WasManuallyOverridden);
        Assert.Equal(2, evaluation.EvidenceCountAtEvaluation);
        Assert.Equal(keyStagePolicyId, evaluation.KeyStagePolicyId);
        Assert.Equal(recordedBy, evaluation.RecordedByPersonId);
        _ = low; // seeded for scale completeness even though this test's evidence is all "mid"
    }

    [Fact]
    public async Task RecordEvaluationAsync_accepts_a_manual_override_even_with_insufficient_evidence()
    {
        await using var db = CreateContext();
        var (scaleId, low, _) = await SeedScaleAsync(db, minimumEvidenceCount: 10);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        var service = CreateService(db);

        var evaluationId = await service.RecordEvaluationAsync(
            studentId, outcomeId, Guid.NewGuid(), scaleId, Guid.NewGuid(), manualAchievementLevelId: low);

        var evaluation = await db.MasteryEvaluations.SingleAsync(e => e.Id == evaluationId);
        Assert.Equal(low, evaluation.AchievementLevelId);
        Assert.True(evaluation.WasManuallyOverridden);
        Assert.Equal(0, evaluation.EvidenceCountAtEvaluation);
    }

    [Fact]
    public async Task GetCurrentAsync_returns_the_most_recently_recorded_evaluation()
    {
        await using var db = CreateContext();
        var (scaleId, low, mid) = await SeedScaleAsync(db, minimumEvidenceCount: 0);
        var studentId = Guid.NewGuid();
        var outcomeId = Guid.NewGuid();
        var earlyService = CreateService(db, new DateOnly(2026, 1, 4));
        var lateService = CreateService(db, new DateOnly(2026, 1, 11));

        await earlyService.RecordEvaluationAsync(studentId, outcomeId, Guid.NewGuid(), scaleId, Guid.NewGuid(), manualAchievementLevelId: low);
        var latestId = await lateService.RecordEvaluationAsync(studentId, outcomeId, Guid.NewGuid(), scaleId, Guid.NewGuid(), manualAchievementLevelId: mid);

        var current = await lateService.GetCurrentAsync(studentId, outcomeId);

        Assert.NotNull(current);
        Assert.Equal(latestId, current!.Id);
        Assert.Equal(mid, current.AchievementLevelId);
    }

    [Fact]
    public async Task GetCurrentAsync_returns_null_when_no_evaluation_has_ever_been_recorded()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var current = await service.GetCurrentAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(current);
    }
}

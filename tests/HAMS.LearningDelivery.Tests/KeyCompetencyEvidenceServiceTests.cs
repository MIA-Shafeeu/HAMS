using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Tests;

public class KeyCompetencyEvidenceServiceTests
{
    private static LearningDeliveryDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LearningDeliveryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Guid> SeedIndicatorAsync(LearningDeliveryDbContext db)
    {
        var competency = new KeyCompetency { Id = Guid.NewGuid(), Code = KeyCompetencyCodes.RelatingToPeople, NameEn = "Relating to People" };
        db.KeyCompetencies.Add(competency);
        var indicator = new KeyCompetencyIndicator
        {
            Id = Guid.NewGuid(), KeyCompetencyId = competency.Id, KeyStageId = Guid.NewGuid(), Code = "RTP.KS3.01",
            DescriptionEn = "Works cooperatively in a group", DescriptionDv = "Works cooperatively in a group (Dv)",
        };
        db.KeyCompetencyIndicators.Add(indicator);
        await db.SaveChangesAsync();
        return indicator.Id;
    }

    private static async Task<Guid> SeedEvidenceTypeAsync(LearningDeliveryDbContext db)
    {
        var type = new EvidenceType { Id = Guid.NewGuid(), Code = EvidenceTypeCodes.RatingScale, Name = "Rating Scale", IsActive = true };
        db.EvidenceTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }

    [Fact]
    public async Task RecordAsync_records_evidence_with_a_rating_score()
    {
        await using var db = CreateContext();
        var indicatorId = await SeedIndicatorAsync(db);
        await SeedEvidenceTypeAsync(db);
        var service = new KeyCompetencyEvidenceService(db);
        var studentId = Guid.NewGuid();

        var evidenceId = await service.RecordAsync(
            studentId, indicatorId, EvidenceTypeCodes.RatingScale, ratingScore: 4, new DateOnly(2026, 1, 4), Guid.NewGuid(), "consistently cooperative");

        var evidence = await db.KeyCompetencyEvidences.SingleAsync(e => e.Id == evidenceId);
        Assert.Equal(studentId, evidence.StudentPersonId);
        Assert.Equal(4, evidence.RatingScore);
        Assert.Equal("consistently cooperative", evidence.Notes);
    }

    [Fact]
    public async Task RecordAsync_rejects_an_unknown_indicator()
    {
        await using var db = CreateContext();
        await SeedEvidenceTypeAsync(db);
        var service = new KeyCompetencyEvidenceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordAsync(Guid.NewGuid(), Guid.NewGuid(), EvidenceTypeCodes.RatingScale, 3, new DateOnly(2026, 1, 4), Guid.NewGuid(), null));
    }

    [Fact]
    public async Task RecordAsync_rejects_an_unknown_evidence_type_code()
    {
        await using var db = CreateContext();
        var indicatorId = await SeedIndicatorAsync(db);
        var service = new KeyCompetencyEvidenceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordAsync(Guid.NewGuid(), indicatorId, "NOT_A_REAL_CODE", null, new DateOnly(2026, 1, 4), Guid.NewGuid(), null));
    }
}

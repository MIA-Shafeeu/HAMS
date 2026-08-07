using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class KeyCompetencyEvidenceService(LearningDeliveryDbContext dbContext) : IKeyCompetencyEvidenceService
{
    public async Task<Guid> RecordAsync(
        Guid studentPersonId, Guid keyCompetencyIndicatorId, string evidenceTypeCode, int? ratingScore,
        DateOnly recordedDate, Guid recordedByPersonId, string? notes, CancellationToken cancellationToken = default)
    {
        var indicatorExists = await dbContext.KeyCompetencyIndicators
            .AnyAsync(i => i.Id == keyCompetencyIndicatorId, cancellationToken);
        if (!indicatorExists)
        {
            throw new InvalidOperationException("Key competency indicator not found.");
        }

        var evidenceType = await dbContext.EvidenceTypes
            .SingleOrDefaultAsync(t => t.Code == evidenceTypeCode && t.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active evidence type with code '{evidenceTypeCode}'.");

        var evidence = new KeyCompetencyEvidence
        {
            Id = Guid.NewGuid(),
            StudentPersonId = studentPersonId,
            KeyCompetencyIndicatorId = keyCompetencyIndicatorId,
            EvidenceTypeId = evidenceType.Id,
            RatingScore = ratingScore,
            RecordedByPersonId = recordedByPersonId,
            RecordedDate = recordedDate,
            Notes = notes,
        };
        dbContext.KeyCompetencyEvidences.Add(evidence);
        await dbContext.SaveChangesAsync(cancellationToken);

        return evidence.Id;
    }

    public async Task<IReadOnlyList<KeyCompetencySummary>> GetSummaryForStudentAsync(
        Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from evidence in dbContext.KeyCompetencyEvidences
            where evidence.StudentPersonId == studentPersonId && evidence.RecordedDate >= fromDate && evidence.RecordedDate <= toDate
            join indicator in dbContext.KeyCompetencyIndicators on evidence.KeyCompetencyIndicatorId equals indicator.Id
            select new { indicator.KeyCompetencyId, evidence.RatingScore })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.KeyCompetencyId)
            .Select(g => new KeyCompetencySummary(
                g.Key,
                g.Count(),
                g.Any(r => r.RatingScore is not null) ? g.Where(r => r.RatingScore is not null).Average(r => r.RatingScore!.Value) : null))
            .ToList();
    }
}

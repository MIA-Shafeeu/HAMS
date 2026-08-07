using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class LearningEvidenceService(LearningDeliveryDbContext dbContext) : ILearningEvidenceService
{
    public async Task<Guid> RecordAsync(
        Guid studentPersonId, Guid learningOutcomeId, Guid? lessonSessionId, string evidenceTypeCode,
        Guid achievementLevelId, DateOnly recordedDate, Guid recordedByPersonId, string? notes,
        CancellationToken cancellationToken = default)
    {
        if (lessonSessionId is { } sessionId)
        {
            var session = await dbContext.LessonSessions.FindAsync([sessionId], cancellationToken)
                ?? throw new InvalidOperationException("Lesson session not found.");

            if (session.Status != LessonSessionStatus.Completed)
            {
                throw new InvalidOperationException("Evidence can only be tied to a Completed lesson session.");
            }
        }

        var evidenceType = await dbContext.EvidenceTypes
            .SingleOrDefaultAsync(t => t.Code == evidenceTypeCode && t.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active evidence type with code '{evidenceTypeCode}'.");

        var levelExists = await dbContext.AchievementLevels
            .AnyAsync(l => l.Id == achievementLevelId && l.IsActive, cancellationToken);
        if (!levelExists)
        {
            throw new InvalidOperationException("Achievement level not found or inactive.");
        }

        var evidence = new LearningEvidence
        {
            Id = Guid.NewGuid(),
            StudentPersonId = studentPersonId,
            LearningOutcomeId = learningOutcomeId,
            LessonSessionId = lessonSessionId,
            EvidenceTypeId = evidenceType.Id,
            AchievementLevelId = achievementLevelId,
            RecordedByPersonId = recordedByPersonId,
            RecordedDate = recordedDate,
            Notes = notes,
        };
        dbContext.LearningEvidences.Add(evidence);
        await dbContext.SaveChangesAsync(cancellationToken);

        return evidence.Id;
    }
}

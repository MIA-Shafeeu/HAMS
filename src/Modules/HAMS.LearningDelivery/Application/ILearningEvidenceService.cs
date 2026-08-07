namespace HAMS.LearningDelivery.Application;

/// <summary>Records append-only <c>LearningEvidence</c> — the subject-outcome evidence track feeding <see cref="IRecommendedLevelEngine"/>.</summary>
public interface ILearningEvidenceService
{
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if <paramref name="lessonSessionId"/> is
    /// supplied but that session isn't <c>Completed</c> (LES-FR-012's "only Completed sessions
    /// count" rule applied to evidence too), if <paramref name="evidenceTypeCode"/> doesn't match
    /// an active <c>EvidenceType</c>, or if <paramref name="achievementLevelId"/> doesn't match an
    /// active <c>AchievementLevel</c>.
    /// </summary>
    Task<Guid> RecordAsync(
        Guid studentPersonId, Guid learningOutcomeId, Guid? lessonSessionId, string evidenceTypeCode,
        Guid achievementLevelId, DateOnly recordedDate, Guid recordedByPersonId, string? notes,
        CancellationToken cancellationToken = default);
}

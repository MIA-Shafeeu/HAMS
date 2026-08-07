namespace HAMS.LearningDelivery.Application;

/// <summary>
/// The small public read surface Phase 8's Mastery evaluation engine (AssessmentEvaluation) needs
/// to break ties when aggregating several outcome-level <c>AchievementLevel</c>s into one overall
/// subject-level result — the same "mode, tie-break toward lower rank" rule
/// <c>IRecommendedLevelEngine</c> already uses within one outcome, applied one level up.
/// </summary>
public interface IAchievementScaleQuery
{
    /// <summary>Every active level's <c>Rank</c> for <paramref name="achievementScaleId"/>, keyed by level id.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetLevelRanksAsync(Guid achievementScaleId, CancellationToken cancellationToken = default);
}

namespace HAMS.LearningDelivery.Application;

/// <summary>
/// The small public read surface Phase 9's topic closure workflow (Intervention) needs — a
/// <c>TeachingTopic</c> maps to exactly one outcome via its <c>SchemeOfWorkItem</c>, but nothing
/// exposes that chain outside this module today.
/// </summary>
public interface ITeachingTopicQuery
{
    /// <returns>Null if the topic doesn't exist.</returns>
    Task<Guid?> GetLearningOutcomeIdAsync(Guid teachingTopicId, CancellationToken cancellationToken = default);
}

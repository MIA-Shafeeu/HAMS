namespace HAMS.Intervention.Application;

public sealed record BehaviourCategoryInfo(string Name, bool IsPositive);

/// <summary>
/// Resolves a <see cref="Domain.BehaviourCategory"/>'s display name/polarity for a sibling module —
/// the same small, single-purpose cross-module read pattern as <c>ITeachingTopicQuery</c>/
/// <c>IEvaluationModelLookup</c> from earlier phases, needed by the guardian portal's non-sensitive
/// behaviour summary (Phase 13).
/// </summary>
public sealed record BehaviourCategoryOption(Guid Id, string Code, string Name, bool IsPositive);

public interface IBehaviourCategoryLookup
{
    Task<BehaviourCategoryInfo?> GetAsync(Guid behaviourCategoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BehaviourCategoryOption>> GetAllAsync(CancellationToken cancellationToken = default);
}

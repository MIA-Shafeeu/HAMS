namespace HAMS.LearningDelivery.Application;

public sealed record CoverageComparisonResult(int PlannedOutcomeCount, int CoveredOutcomeCount, IReadOnlyList<Guid> UncoveredOutcomeIds);

/// <summary>
/// Compares what a <c>SchemeOfWork</c> planned to cover against what's actually been delivered
/// (build plan Phase 5 scope: "coverage comparison") — only <c>Completed</c> lesson sessions count
/// towards actual coverage (LES-FR-012).
/// </summary>
public interface ICoverageComparisonService
{
    Task<CoverageComparisonResult> CompareAsync(Guid schemeOfWorkId, CancellationToken cancellationToken = default);
}

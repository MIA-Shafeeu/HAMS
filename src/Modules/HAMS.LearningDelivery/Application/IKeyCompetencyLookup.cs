namespace HAMS.LearningDelivery.Application;

public sealed record KeyCompetencyName(Guid Id, string NameEn, string? NameDv);

/// <summary>The one small read a report card (Phase 11, ReportingAnalyticsAudit) needs to label its key-competency summary by name.</summary>
public interface IKeyCompetencyLookup
{
    /// <summary>All 8 fixed national Key Competencies — small and stable enough to always return in full rather than filtering by id.</summary>
    Task<IReadOnlyList<KeyCompetencyName>> GetAllAsync(CancellationToken cancellationToken = default);
}

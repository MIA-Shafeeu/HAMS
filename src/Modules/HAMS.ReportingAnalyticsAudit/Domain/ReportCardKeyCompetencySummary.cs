namespace HAMS.ReportingAnalyticsAudit.Domain;

/// <summary>
/// A snapshot of one Key Competency's accumulated evidence for the report card's window, at the
/// moment the <see cref="ReportCard"/> was prepared (same snapshot-not-live-reference reasoning as
/// <see cref="ReportCardSubjectResult"/>). <see cref="AverageRatingScore"/> is null if none of the
/// evidence in the window was a rating-scale instrument — anecdotal-only evidence still counts
/// toward <see cref="EvidenceCount"/>.
/// </summary>
public sealed class ReportCardKeyCompetencySummary
{
    public Guid Id { get; init; }

    public Guid ReportCardId { get; init; }

    public Guid KeyCompetencyId { get; init; }

    public int EvidenceCount { get; init; }

    public double? AverageRatingScore { get; init; }
}

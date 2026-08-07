namespace HAMS.LearningDelivery.Application;

/// <summary>A student's accumulated evidence for one Key Competency over a reporting window (Phase 11's report-card summary).</summary>
public sealed record KeyCompetencySummary(Guid KeyCompetencyId, int EvidenceCount, double? AverageRatingScore);

/// <summary>Records append-only <c>KeyCompetencyEvidence</c> — the parallel, lighter-weight Key Competency evidence track (build plan §3).</summary>
public interface IKeyCompetencyEvidenceService
{
    Task<Guid> RecordAsync(
        Guid studentPersonId, Guid keyCompetencyIndicatorId, string evidenceTypeCode, int? ratingScore,
        DateOnly recordedDate, Guid recordedByPersonId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates every evidence row (across all of a competency's indicators) recorded for this
    /// student within the window into one row per <c>KeyCompetency</c> that has at least one — the
    /// read side this track never had until Phase 11's report card needed it.
    /// <see cref="KeyCompetencySummary.AverageRatingScore"/> is null if no evidence in the window
    /// was a rating-scale instrument (anecdotal-only evidence still counts toward
    /// <see cref="KeyCompetencySummary.EvidenceCount"/>, just doesn't contribute a number).
    /// </summary>
    Task<IReadOnlyList<KeyCompetencySummary>> GetSummaryForStudentAsync(
        Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}

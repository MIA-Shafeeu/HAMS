namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// One append-only piece of evidence against a <see cref="KeyCompetencyIndicator"/> — the second,
/// deliberately lighter-weight evidence track (build plan §3): free-text anecdotal notes,
/// rating-scale scores, self/peer-assessment checklist results, or portfolio references, reusing
/// the same <see cref="EvidenceType"/> lookup <see cref="LearningEvidence"/> uses rather than
/// inventing a parallel one. Unlike <see cref="LearningEvidence"/> there is no
/// <c>AchievementLevel</c>/mastery-recommendation concept here — this track only ever accumulates
/// evidence for later reporting (Phase 11's report-card key-competency summary), it does not feed
/// a recommended-level engine of its own.
/// </summary>
public sealed class KeyCompetencyEvidence
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid KeyCompetencyIndicatorId { get; init; }

    public Guid EvidenceTypeId { get; init; }

    /// <summary>Only meaningful when <see cref="EvidenceTypeId"/> is a rating-scale instrument — null otherwise.</summary>
    public int? RatingScore { get; init; }

    public Guid RecordedByPersonId { get; init; }

    public DateOnly RecordedDate { get; init; }

    public string? Notes { get; set; }
}

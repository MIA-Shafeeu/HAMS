namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A named, school-configurable set of achievement levels (build plan §1.6 — configurable
/// business data, not an enum) — e.g. a 3-point "Foundation Stage" scale vs. a 5-point "Key
/// Stage 3" scale. <see cref="OrgCurriculum.Domain.KeyStagePolicy.AchievementScaleId"/> (reserved
/// since Phase 1) is a loose forward reference to this table — Mastery/Evidence lives in
/// LearningDelivery per the build plan's Module Boundaries table, not OrgCurriculum, so this is
/// deliberately NOT placed alongside <c>KeyStagePolicy</c> despite being the thing it points to
/// (the same loose-reference pattern used everywhere a module boundary crosses a FK, e.g.
/// <c>SchemeOfWork.SubjectId</c> back into OrgCurriculum).
///
/// Deliberately NOT seeded with a default — unlike the Maldivian working week (a genuine, concrete
/// real-world default), an achievement scale's levels and names are a school-specific pedagogical
/// choice with no universal default to assume, the same reasoning that kept <c>Subject</c>
/// (Phase 2) un-seeded.
/// </summary>
public sealed class AchievementScale
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    /// <summary>
    /// How many <see cref="LearningEvidence"/> rows must exist for a student+outcome before
    /// <c>IRecommendedLevelEngine</c> will recommend a level at all — the configurable
    /// "sufficiency rule" the build plan's Phase 6 scope line calls for.
    /// </summary>
    public int MinimumEvidenceCount { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// A named, school-configurable weighting scheme (build plan §1.6 — configurable business data,
/// not an enum) — e.g. "Key Stage 3 Standard" might weight Term Exam 60% / Continuous Assessment
/// 40%. <see cref="OrgCurriculum.Domain.KeyStagePolicy.AssessmentSchemeId"/> (reserved since
/// Phase 1) is a loose forward reference to this table — Assessment/Exam lives in
/// AssessmentEvaluation per the build plan's Module Boundaries table, not OrgCurriculum, the same
/// loose-reference pattern as <see cref="LearningDelivery.Domain.AchievementScale"/>
/// (LearningDelivery) being pointed at by <c>KeyStagePolicy.AchievementScaleId</c>.
///
/// Deliberately NOT seeded with a default — a weighting scheme is a school-specific policy choice
/// with no universal default to assume, same reasoning as <c>AchievementScale</c>/<c>Subject</c>.
/// </summary>
public sealed class AssessmentScheme
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

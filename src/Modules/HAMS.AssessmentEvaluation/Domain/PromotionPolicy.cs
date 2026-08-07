namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// The configurable bar a student's subject evaluations must clear for promotion (build plan §1.6 —
/// a lookup entity, not hardcoded rules), resolved via <c>KeyStagePolicy.PromotionPolicyId</c> (the
/// last of that entity's four reserved forward references, unpopulated since Phase 1). Deliberately
/// minimal, mechanism-only: <see cref="MinimumRank"/> is checked against whichever facet a
/// <c>KeyStageEvaluation</c> populated — <c>AchievementLevel.Rank</c> for a Mastery-model subject,
/// <c>GradeBand.Rank</c> for an Assessment-model one (both already "higher = better" by convention) —
/// so one policy field works across evaluation models without inventing a cross-scale conversion.
/// <see cref="MinimumSubjectsRequiredToClear"/> is a simple count threshold, not a weighted/
/// core-vs-elective distinction (this system has no such distinction yet) — <c>IPromotionService</c>
/// surfaces exactly which subjects didn't clear so a human makes the real decision, this policy only
/// computes a recommendation.
/// </summary>
public sealed class PromotionPolicy
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int MinimumRank { get; set; }

    public int MinimumSubjectsRequiredToClear { get; set; }

    public bool IsActive { get; set; } = true;
}

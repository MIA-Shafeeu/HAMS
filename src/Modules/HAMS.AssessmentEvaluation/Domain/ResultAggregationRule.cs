using HAMS.Platform.Common.Contracts;

namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6/§3 — the "IFinalResultResolver" configured rule), not an
/// enum — how multiple <see cref="Assessment"/> instances within one
/// <see cref="AssessmentSchemeComponent"/> (e.g. the Ministry policy's "2-3 in-class continuous
/// assessments per term") combine into that component's one contributing percentage.
///
/// <b>Deliberate scope-down, flagged rather than silently done</b>: the build plan names five
/// example rules — "latest/highest/attempt-average/cap/component-replacement." Only the first
/// three (<see cref="ResultAggregationRuleCodes.Latest"/>/<see cref="ResultAggregationRuleCodes.Highest"/>/
/// <see cref="ResultAggregationRuleCodes.Average"/>) are implemented; Cap and Component-Replacement
/// are more specialized variants no school has asked for by name yet — add them, with real
/// implementations in <c>AssessmentEvaluationEngine</c>, the day one is actually needed, rather
/// than building unused logic now (the same reasoning Phase 7 applied to deferring the
/// Escalate/Delegate workflow verbs).
/// </summary>
public sealed class ResultAggregationRule : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class ResultAggregationRuleCodes
{
    public const string Latest = "LATEST";
    public const string Highest = "HIGHEST";
    public const string Average = "AVERAGE";
}

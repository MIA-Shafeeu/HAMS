namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// A named, school-configurable set of percentage-banded grades (build plan §1.6 — configurable
/// business data, not an enum) — e.g. A*/A/B/C/D/E/U bands. <see cref="OrgCurriculum.Domain.KeyStagePolicy.GradeScaleId"/>
/// (reserved since Phase 1) is a loose forward reference to this table, mirroring
/// <c>AssessmentSchemeId</c>'s wiring in this same phase.
///
/// Deliberately NOT seeded with a default — grade boundaries are a school/syllabus-specific policy
/// choice (and for externally-set exams, the external board's own choice) with no universal
/// default to assume.
/// </summary>
public sealed class GradeScale
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

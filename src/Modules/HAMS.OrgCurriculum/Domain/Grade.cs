namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// <see cref="NextGradeId"/> is the school's configured default promotion path (Phase 11) — e.g.
/// Grade 5 → Grade 6. Deliberately just a suggested default, not a constraint enforced anywhere:
/// <c>IPromotionService.RecordDecisionAsync</c> (AssessmentEvaluation) always takes an explicit
/// next-grade from the caller, so a school can still promote a student into a different grade,
/// hold them back, or skip a grade as a real exception, without this field getting in the way.
/// Null means no default is configured yet (a caller must always specify one explicitly).
/// </summary>
public sealed class Grade
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? NextGradeId { get; set; }
}

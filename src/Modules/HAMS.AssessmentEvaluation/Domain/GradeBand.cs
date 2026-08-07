namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// One percentage band within a <see cref="GradeScale"/> (e.g. "A*": 90-100%) — configurable per
/// school/syllabus, not an enum. <see cref="Rank"/> orders bands (higher = better grade), the same
/// role <c>AchievementLevel.Rank</c> plays for mastery scales.
/// </summary>
public sealed class GradeBand
{
    public Guid Id { get; init; }

    public Guid GradeScaleId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public decimal MinPercentage { get; set; }

    public decimal MaxPercentage { get; set; }

    public int Rank { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

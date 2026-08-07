namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// A named, school-configurable window an evaluation covers (build plan Phase 8 scope: "evaluation
/// periods") — e.g. "Term 1", "Full Year". <see cref="AcademicYearId"/> is a loose reference into
/// OrgCurriculum, the same convention as everywhere else; <see cref="StartDate"/>/<see cref="EndDate"/>
/// are stored directly here rather than derived by joining OrgCurriculum's <c>Term</c> (the same
/// "denormalize rather than force an extra cross-module join at evaluation time" choice
/// <c>Assessment</c> already makes by storing both <c>TermId</c> and <c>AcademicYearId</c>).
/// Scopes both evaluation tracks consistently: an <c>Assessment</c> counts if its
/// <c>ScheduledDate</c> falls within the window, a <c>MasteryEvaluation</c> counts if its
/// <c>RecordedAtUtc</c> does.
/// </summary>
public sealed class EvaluationPeriod
{
    public Guid Id { get; init; }

    public Guid AcademicYearId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int DisplayOrder { get; set; }
}

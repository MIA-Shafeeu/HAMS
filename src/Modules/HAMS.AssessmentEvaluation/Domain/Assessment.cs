namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// One scheduled assessment instance for a subject+grade+term (build plan §3 evaluation chain:
/// <c>Assessment → AssessmentResult</c>). <see cref="SubjectId"/>/<see cref="GradeId"/>/
/// <see cref="TermId"/>/<see cref="AcademicYearId"/> are loose references into OrgCurriculum,
/// the same unvalidated-Guid convention as every other cross-module reference in this codebase.
///
/// <see cref="DurationMinutes"/> and <see cref="ExternalExaminationBoardId"/> are mutually
/// exclusive in spirit, not enforced at the DB level: when an external syndicate (Cambridge,
/// Edexcel, SSC, HSC) sets the paper, this school doesn't author a duration locally (build plan
/// §3) — <see cref="ExternalSyllabusCode"/> instead names the specific qualification/paper (e.g.
/// "IGCSE", "A-Level").
/// </summary>
public sealed class Assessment
{
    public Guid Id { get; init; }

    public Guid SubjectId { get; init; }

    public Guid GradeId { get; init; }

    public Guid TermId { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid AssessmentCategoryId { get; init; }

    public required string Title { get; set; }

    public decimal MaxMarks { get; set; }

    public int? DurationMinutes { get; set; }

    public Guid? ExternalExaminationBoardId { get; set; }

    public string? ExternalSyllabusCode { get; set; }

    public DateOnly ScheduledDate { get; set; }
}

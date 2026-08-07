using HAMS.Platform.Common.Contracts;

namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6 explicitly names "AssessmentCategory" as an example), not
/// an enum — e.g. the Ministry's 2019 Assessment Policy distinguishes a time-boxed Key Stage 3
/// term exam (capped 120 minutes/60 marks, six named subjects) from in-class continuous
/// assessment (2-3 per term, the remaining subjects) — which category applies to a given
/// <see cref="Assessment"/> is data, not code.
/// </summary>
public sealed class AssessmentCategory : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class AssessmentCategoryCodes
{
    public const string TermExam = "TERM_EXAM";
    public const string ContinuousAssessment = "CONTINUOUS_ASSESSMENT";
    public const string Quiz = "QUIZ";
    public const string Project = "PROJECT";
    public const string Other = "OTHER";
}

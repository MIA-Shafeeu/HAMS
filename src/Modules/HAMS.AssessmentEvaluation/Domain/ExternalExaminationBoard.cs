using HAMS.Platform.Common.Contracts;

namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// A syndicate that sets its own exam duration/format externally (build plan §3: Key Stage 4/5
/// exams are "bounded by whatever the external syndicate sets for that paper rather than authored
/// locally"). When <see cref="Assessment.ExternalExaminationBoardId"/> is set,
/// <see cref="Assessment.DurationMinutes"/> is deliberately left null — this school doesn't author
/// that value, the board does; <see cref="Assessment.ExternalSyllabusCode"/> captures which
/// specific qualification/paper (e.g. "IGCSE", "O-Level", "A-Level", "IAL") within the board.
/// Seeded with the four bodies the Assessment Policy actually names for KS4/5 (Cambridge, Edexcel,
/// SSC, HSC) — real reference data, the same "use the real named source" precedent as Phase 2's
/// NCF Key Learning Areas and Phase 6's Key Competencies, not a fabricated placeholder list.
/// </summary>
public sealed class ExternalExaminationBoard : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class ExternalExaminationBoardCodes
{
    public const string Cambridge = "CAMBRIDGE";
    public const string Edexcel = "EDEXCEL";
    public const string Ssc = "SSC";
    public const string Hsc = "HSC";
}

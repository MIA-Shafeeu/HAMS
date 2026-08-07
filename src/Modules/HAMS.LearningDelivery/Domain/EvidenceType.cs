using HAMS.Platform.Common.Contracts;

namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// Configurable lookup (build plan §1.6 explicitly names "EvidenceType" as an example), not an
/// enum. Shared by both evidence tracks — <see cref="LearningEvidence"/> (subject-outcome mastery)
/// and <see cref="KeyCompetencyEvidence"/> (the parallel Key Competency track, build plan §3) —
/// since schools already use the same handful of real instruments (observation, work sample,
/// anecdotal note, rating scale, checklist, portfolio reference) for both, per the NIE's teacher
/// guide; a separate lookup per track would just duplicate the same rows.
/// </summary>
public sealed class EvidenceType : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class EvidenceTypeCodes
{
    public const string Observation = "OBSERVATION";
    public const string WorkSample = "WORK_SAMPLE";
    public const string Quiz = "QUIZ";
    public const string AnecdotalNote = "ANECDOTAL_NOTE";
    public const string RatingScale = "RATING_SCALE";
    public const string Checklist = "CHECKLIST";
    public const string PortfolioReference = "PORTFOLIO_REFERENCE";
    public const string Other = "OTHER";
}

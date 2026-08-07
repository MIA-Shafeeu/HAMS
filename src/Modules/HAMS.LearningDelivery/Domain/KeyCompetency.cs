namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// One of the National Curriculum Framework's 8 fixed, cross-cutting Key Competencies (build plan
/// §3) — sits alongside, not inside, the subject-based curriculum hierarchy. Seeded once; schools
/// don't add their own (this is a national, not school-configurable, list — the one deliberate
/// exception to "reference data is always admin-editable" in this codebase, since these 8 rows are
/// fixed by national policy, not a local choice). Bilingual per the established convention for
/// official, report-card-facing named content (same reasoning as <c>TeachingTopic</c>) — every
/// <c>ReportCard</c> (Phase 11) must surface these by name to guardians, many of whom read Dhivehi.
/// <see cref="NameDv"/> is nullable and seeded as null, same precedent as Phase 3's
/// <c>Atoll</c>/<c>Island</c> seed data: a fabricated Thaana translation risks a real transcription
/// error on an official document, worse than an honestly-empty field — fill it in with a
/// Dhivehi-fluent reviewer before go-live, don't guess it here.
/// </summary>
public sealed class KeyCompetency
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string NameEn { get; set; }

    public string? NameDv { get; set; }

    public int DisplayOrder { get; set; }
}

public static class KeyCompetencyCodes
{
    public const string PractisingIslam = "PRACTISING_ISLAM";
    public const string UnderstandingManagingSelf = "UNDERSTANDING_MANAGING_SELF";
    public const string ThinkingCriticallyCreatively = "THINKING_CRITICALLY_CREATIVELY";
    public const string RelatingToPeople = "RELATING_TO_PEOPLE";
    public const string MakingMeaning = "MAKING_MEANING";
    public const string LivingHealthyLife = "LIVING_HEALTHY_LIFE";
    public const string UsingSustainablePractices = "USING_SUSTAINABLE_PRACTICES";
    public const string UsingTechnologyMedia = "USING_TECHNOLOGY_MEDIA";
}

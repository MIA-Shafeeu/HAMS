namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// One piece of append-only evidence that a student demonstrated a given <see cref="AchievementLevel"/>
/// against a specific <c>LearningOutcome</c> (build plan §3 evaluation chain:
/// <c>LessonSession → LearningEvidence → MasteryEvaluation</c>). Never updated or deleted once
/// recorded — a correction is a new piece of evidence, not an edit to this one (same append-only
/// discipline as every judgement row in §3) — so this deliberately does NOT implement
/// <c>IVersionedRecord</c>: there is no code path that ever calls <c>Update</c>/<c>Remove</c> on
/// it, so the Draft/Published lifecycle machinery would be pure overhead.
/// <see cref="LessonSessionId"/> is optional — a lesson isn't the only source of evidence (a
/// portfolio review or standalone observation has none) — but when present it must reference a
/// <c>Completed</c> session (LES-FR-012's same "only Completed sessions count" rule applied here).
/// </summary>
public sealed class LearningEvidence
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid LearningOutcomeId { get; init; }

    public Guid? LessonSessionId { get; init; }

    public Guid EvidenceTypeId { get; init; }

    public Guid AchievementLevelId { get; init; }

    public Guid RecordedByPersonId { get; init; }

    public DateOnly RecordedDate { get; init; }

    public string? Notes { get; set; }
}

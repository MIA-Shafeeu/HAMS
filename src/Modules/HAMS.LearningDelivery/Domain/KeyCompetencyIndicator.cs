namespace HAMS.LearningDelivery.Domain;

/// <summary>
/// A per-key-stage progression indicator for a <see cref="KeyCompetency"/> (build plan §3), coded
/// like the NIE teacher guide's own scheme (e.g. <c>UMS.KS3.04</c>). <see cref="KeyStageId"/> is a
/// loose reference into OrgCurriculum's school-configurable <c>KeyStage</c> table.
///
/// <b>Deliberate scope-down, flagged rather than silently done</b>: the build plan says this
/// should be "versioned the same way as curriculum content — a new NCF revision clones the
/// indicator set forward, never edits history," matching <c>Syllabus</c>'s clone-on-publish
/// machinery. That full versioning apparatus is NOT built here — only one NCF revision exists in
/// practice today and no clone/publish workflow was requested. If a future NCF revision ever needs
/// its own historical indicator set preserved, add the same <c>IVersionedRecord</c> + clone-on-publish
/// pattern <c>ISyllabusPublishingService</c> already established, rather than editing these rows in
/// place at that point.
/// </summary>
public sealed class KeyCompetencyIndicator
{
    public Guid Id { get; init; }

    public Guid KeyCompetencyId { get; init; }

    public Guid KeyStageId { get; init; }

    public required string Code { get; init; }

    public required string DescriptionEn { get; set; }

    public required string DescriptionDv { get; set; }

    public int DisplayOrder { get; set; }
}

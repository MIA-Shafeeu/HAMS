using HAMS.Platform.Common.Contracts;

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// The unit of curriculum versioning (build plan §3, "version-as-new-parent-row"). Publishing a
/// new revision clones the entire <see cref="Strand"/>→<see cref="SubStrand"/>→
/// <see cref="LearningOutcome"/>→<see cref="Indicator"/> subtree into new rows under a new
/// <see cref="Id"/> — the old tree is frozen forever, and every downstream record (evidence,
/// lesson plans, mastery evaluations, in later phases) FKs directly to the exact
/// <c>LearningOutcomeId</c>/<c>IndicatorId</c> active at the time it was produced, so historical
/// records never silently rewrite (CUR-FR-004, BR-007). <see cref="ISyllabusPublishingService"/>
/// is the only sanctioned path that creates a new revision or publishes one.
/// </summary>
public sealed class Syllabus : IVersionedRecord<Guid>
{
    public Guid Id { get; init; }

    public Guid SubjectId { get; init; }

    public int Version { get; init; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesId { get; init; }
    public Guid? SupersededById { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Draft;

    public bool IsImmutable => Status is RecordStatus.Published or RecordStatus.Locked;
}

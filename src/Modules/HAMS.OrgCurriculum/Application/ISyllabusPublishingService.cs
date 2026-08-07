using HAMS.OrgCurriculum.Domain;

namespace HAMS.OrgCurriculum.Application;

/// <summary>
/// The only sanctioned way to create or advance a <see cref="Syllabus"/> revision (build plan §3's
/// "version-as-new-parent-row" pattern). Nothing else should insert directly into
/// <see cref="Strand"/>/<see cref="SubStrand"/>/<see cref="LearningOutcome"/>/<see cref="Indicator"/>
/// outside of these two operations.
/// </summary>
public interface ISyllabusPublishingService
{
    /// <summary>Starts a brand-new Draft syllabus for a subject that has no existing revision. Content is added to it afterwards.</summary>
    Task<Guid> CreateInitialDraftAsync(Guid subjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deep-clones an existing syllabus's entire Strand→SubStrand→LearningOutcome→Indicator
    /// subtree (including prerequisite links) into a new Draft revision, ready to be edited
    /// without touching the original tree at all.
    /// </summary>
    Task<Guid> CreateDraftRevisionAsync(Guid existingSyllabusId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a Draft syllabus: it becomes the current, immutable revision for its subject. If
    /// it supersedes an earlier revision, that revision is simultaneously marked Superseded and
    /// no longer current — its own tree is left untouched forever.
    /// </summary>
    Task PublishAsync(Guid syllabusId, CancellationToken cancellationToken = default);
}

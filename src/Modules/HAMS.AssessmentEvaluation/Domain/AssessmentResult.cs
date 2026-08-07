using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;

namespace HAMS.AssessmentEvaluation.Domain;

/// <summary>
/// One attempt at an <see cref="Assessment"/> for one student (build plan §3: "raw/adjusted/
/// moderated/final kept as separate never-overwritten columns, append-only, stores
/// KeyStagePolicyId"). Unlike Phase 6's evidence entities (deliberately plain, insert-only), this
/// implements <see cref="IVersionedRecord{TKey}"/> for real: the plan explicitly groups
/// <c>AssessmentResult</c> with <c>KeyStagePolicy</c>/<c>Syllabus</c> in the
/// "append-only rows + explicit status lifecycle" bucket, and assessment moderation is explicitly
/// named as a <c>Platform.Workflow</c> consumer — a genuine multi-step human review process
/// (Submit → Review → Approve/Reject/Return), not a one-shot insert. A correction to an already
/// Published/Locked result must insert a new version via <c>ImmutableRecordCorrectionScope</c>
/// (mirroring <c>ISyllabusPublishingService.PublishAsync</c>), never mutate this row in place.
///
/// <see cref="ModerationStatus"/> is the separate, business-facing axis from <see cref="Status"/>
/// (the structural immutability lifecycle): reaching <see cref="WorkflowStatus.Approved"/> is what
/// triggers <see cref="Status"/> flipping <c>Draft</c> → <c>Published</c> (see
/// <c>IAssessmentModerationService.ApproveAsync</c>) — the one meaningful junction between "the
/// human process finished" and "this row is now structurally locked."
///
/// <see cref="RawMark"/> is nullable specifically for the Ministry Assessment Policy's special
/// circumstances (build plan §3): a Medical-Certificate-excused or calibration-only result may
/// have no mark at all — <c>IAssessmentModerationService</c> enforces that at least one of
/// <see cref="RawMark"/>/<see cref="SpecialResultStateId"/> is always set.
/// </summary>
public sealed class AssessmentResult : IVersionedRecord<Guid>
{
    public Guid Id { get; init; }

    public Guid AssessmentId { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid KeyStagePolicyId { get; init; }

    public decimal? RawMark { get; set; }
    public decimal? AdjustedMark { get; set; }
    public decimal? ModeratedMark { get; set; }
    public decimal? FinalMark { get; set; }

    public Guid? SpecialResultStateId { get; set; }

    public Guid RecordedByPersonId { get; set; }

    public WorkflowStatus ModerationStatus { get; set; } = WorkflowStatus.Draft;

    /// <summary>Set only when <see cref="ModerationStatus"/> reaches <see cref="WorkflowStatus.Escalated"/> (Phase 13 — "advanced moderation").</summary>
    public string? EscalationReason { get; set; }

    public Guid? EscalatedByPersonId { get; set; }

    public int Version { get; init; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesId { get; init; }
    public Guid? SupersededById { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Draft;

    public bool IsImmutable => Status is RecordStatus.Published or RecordStatus.Locked;
}

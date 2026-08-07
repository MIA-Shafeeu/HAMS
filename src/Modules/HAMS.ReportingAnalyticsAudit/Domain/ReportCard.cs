using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;

namespace HAMS.ReportingAnalyticsAudit.Domain;

/// <summary>
/// One term's report for one student (build plan Phase 11 — the real Ministry Assessment Policy's
/// four mandatory sections, modelled as distinct fields per the plan's own instruction: "not one
/// opaque body/PDF blob"). <see cref="NarrativeEn"/>/<see cref="NarrativeDv"/> (learning-progress
/// narrative) and <see cref="NextStepsEn"/>/<see cref="NextStepsDv"/> (forward-looking next steps)
/// are bilingual — official, guardian-facing content, the same judgment call as <c>TeachingTopic</c>
/// and <c>KeyCompetency</c>'s names. The other two mandatory sections — subject results and the
/// key-competency achievement summary — are separate satellite tables
/// (<see cref="ReportCardSubjectResult"/>/<see cref="ReportCardKeyCompetencySummary"/>), snapshotted
/// at <c>IReportCardService.PrepareAsync</c> time rather than resolved live: the Ministry policy
/// requires every published report card stay retrievable from admission to leaving, and a snapshot
/// is what makes that true even if the source <c>KeyStageEvaluation</c>/evidence data model, or a
/// subject's name, changes later.
///
/// Implements <see cref="IVersionedRecord{TKey}"/> for real, the exact same two-axis shape as
/// <c>AssessmentResult</c>: <see cref="ApprovalStatus"/> is the <c>Platform.Workflow</c>-driven
/// business process (Draft → Submitted → UnderReview → Approved/Rejected/Returned — this report
/// card is that kernel's third consumer, after assessment moderation and topic closure, with zero
/// kernel changes needed), <see cref="Status"/> is the structural immutability lifecycle — reaching
/// <see cref="WorkflowStatus.Approved"/> is what flips <see cref="Status"/> to
/// <see cref="RecordStatus.Published"/>, the one moment this becomes visible to a guardian/student
/// portal and structurally locked.
/// </summary>
public sealed class ReportCard : IVersionedRecord<Guid>
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid EvaluationPeriodId { get; init; }

    public required string NarrativeEn { get; set; }
    public required string NarrativeDv { get; set; }

    public required string NextStepsEn { get; set; }
    public required string NextStepsDv { get; set; }

    public Guid PreparedByPersonId { get; init; }

    public DateTimeOffset PreparedAtUtc { get; init; }

    public WorkflowStatus ApprovalStatus { get; set; } = WorkflowStatus.Draft;

    public int Version { get; init; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? SupersedesId { get; init; }
    public Guid? SupersededById { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Draft;

    public bool IsImmutable => Status is RecordStatus.Published or RecordStatus.Locked;
}

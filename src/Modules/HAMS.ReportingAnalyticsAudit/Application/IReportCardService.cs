using HAMS.PeopleEnrollment.Application;
using HAMS.ReportingAnalyticsAudit.Domain;

namespace HAMS.ReportingAnalyticsAudit.Application;

public sealed record PrepareReportCardRequest(
    Guid StudentPersonId, Guid AcademicYearId, Guid EvaluationPeriodId,
    string NarrativeEn, string NarrativeDv, string NextStepsEn, string NextStepsDv, Guid PreparedByPersonId);

public sealed record ReviseReportCardRequest(string NarrativeEn, string NarrativeDv, string NextStepsEn, string NextStepsDv);

/// <summary>
/// The report-card lifecycle (build plan Phase 11): prepare (snapshots the student's current
/// subject evaluations and Key Competency evidence for the period) → the same Draft→Submitted→
/// UnderReview→Approved/Rejected/Returned pipeline every other <c>Platform.Workflow</c> consumer
/// uses (Phase 7's assessment moderation, Phase 9's topic closure — this is the kernel's third
/// consumer, zero kernel changes) → PDF rendering, generated fresh from the stored snapshot on
/// every request rather than a stored file (no file-storage pipeline needed at all — the durable
/// structured data already satisfies the Ministry policy's long-term-retrievability requirement;
/// see <c>ReportCard</c>'s own remarks).
/// </summary>
public interface IReportCardService
{
    /// <exception cref="InvalidOperationException">The student has no current evaluations for that period, or the evaluation period was not found.</exception>
    Task<Guid> PrepareAsync(PrepareReportCardRequest request, CancellationToken cancellationToken = default);

    Task SubmitAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    Task BeginReviewAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    Task ApproveAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    Task ReturnAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The one sanctioned way to correct an already-Published/Locked report card (mirrors
    /// <c>IAssessmentModerationService.ReviseApprovedResultAsync</c>'s exact
    /// <c>ImmutableRecordCorrectionScope</c> pattern) — corrects the narrative/next-steps text only;
    /// the subject-result and key-competency snapshots carry forward unchanged onto the new version,
    /// since a wording correction shouldn't silently re-derive the underlying academic record.
    /// </summary>
    Task<Guid> ReviseApprovedReportCardAsync(Guid reportCardId, ReviseReportCardRequest request, CancellationToken cancellationToken = default);

    Task<ReportCard?> GetAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    /// <summary>Every Published, current report card for this student — the "retrievable from admission to leaving" surface (build plan Phase 11), never Draft/in-review ones.</summary>
    Task<IReadOnlyList<ReportCard>> GetPublishedForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportCardSubjectResult>> GetSubjectResultsAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportCardKeyCompetencySummary>> GetKeyCompetencySummariesAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The report card was not found.</exception>
    Task<byte[]> RenderPdfAsync(Guid reportCardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every student in this grade/year with an active enrolment but no <see cref="ReportCard"/>
    /// prepared yet for that evaluation period — a report-card authoring UI's worklist (the first
    /// "who still needs one" read anywhere in this codebase; every prior caller already knew the
    /// specific student it wanted). A student with a Draft/Submitted/UnderReview/Rejected/Returned
    /// report card already prepared drops off this list too — "needing a report card" means needing
    /// one PREPARED, not one published; an in-progress one is tracked through its own workflow instead.
    /// </summary>
    Task<IReadOnlyList<ClassRosterEntry>> GetStudentsNeedingReportCardAsync(
        Guid gradeId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default);
}

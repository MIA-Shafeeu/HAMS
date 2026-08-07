using HAMS.AssessmentEvaluation.Domain;
using HAMS.Attendance.Application;
using HAMS.CommunicationPortals.Domain;
using HAMS.LearningDelivery.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.ReportingAnalyticsAudit.Domain;

namespace HAMS.CommunicationPortals.Application;

/// <summary>
/// A guardian-facing, deliberately minimal summary of an intervention case (Phase 10) — never the
/// full <c>InterventionCase</c> row. A guardian gets awareness that support exists/started/ended,
/// not the case's <c>InterventionTypeId</c>/<c>ConfidentialityTierCode</c>/notes — those stay a
/// staff (Intervention module) and <c>IConfidentialRecordAccessor</c> concern.
/// </summary>
public sealed record InterventionUpdateSummary(Guid SubjectId, DateOnly OpenedDate, bool IsOpen, DateOnly? ClosedDate);

/// <summary>
/// A guardian-facing, deliberately minimal summary of a behaviour incident (Phase 13) — category
/// name/polarity and date only, the same "awareness, never detail" precedent
/// <see cref="InterventionUpdateSummary"/> already established: never the incident's
/// <c>Description</c>/<c>ActionTaken</c>/<c>ReviewNotes</c>/<c>ConfidentialityTierCode</c>, which stay
/// a staff and <c>IConfidentialRecordAccessor</c> concern.
/// </summary>
public sealed record BehaviourIncidentSummary(string CategoryName, bool IsPositive, DateOnly OccurredDate);

/// <summary>
/// The guardian portal's whole read surface (build plan Phase 10: "published-only portal views").
/// Every method beyond <see cref="GetMyStudentsAsync"/> re-derives the caller's permission for the
/// specific student from <see cref="IGuardianRelationshipService.GetStudentsForGuardianAsync"/>
/// itself — never trusts a client-supplied flag — since that's the one place a guardian's
/// Verified, currently-active, Can-View-flagged relationship to a student is authoritative.
/// </summary>
public interface IGuardianPortalService
{
    Task<IReadOnlyList<GuardianStudentSummary>> GetMyStudentsAsync(Guid guardianPersonId, CancellationToken cancellationToken = default);

    /// <exception cref="UnauthorizedAccessException">No active, Verified, academic-record-visible relationship exists between this guardian and student.</exception>
    Task<IReadOnlyList<KeyStageEvaluation>> GetStudentResultsAsync(Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default);

    /// <exception cref="UnauthorizedAccessException">No active, Verified, attendance-visible relationship exists between this guardian and student.</exception>
    Task<IReadOnlyList<AttendanceRecordSummary>> GetStudentAttendanceAsync(
        Guid guardianPersonId, Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    /// <exception cref="UnauthorizedAccessException">No active, Verified, intervention-visible relationship exists between this guardian and student.</exception>
    Task<IReadOnlyList<InterventionUpdateSummary>> GetStudentInterventionUpdatesAsync(
        Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default);

    /// <summary>Published report cards only (build plan Phase 11/10: "published-only portal views") — reuses <c>CanViewAcademicRecords</c>, the same flag <see cref="GetStudentResultsAsync"/> already gates on, since a report card is itself an academic record.</summary>
    /// <exception cref="UnauthorizedAccessException">No active, Verified, academic-record-visible relationship exists between this guardian and student.</exception>
    Task<IReadOnlyList<ReportCard>> GetStudentReportCardsAsync(Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default);

    /// <exception cref="UnauthorizedAccessException">No active, Verified, academic-record-visible relationship exists between this guardian and student.</exception>
    /// <exception cref="InvalidOperationException">The report card doesn't exist, or doesn't belong to this student.</exception>
    Task<byte[]> GetStudentReportCardPdfAsync(Guid guardianPersonId, Guid studentPersonId, Guid reportCardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Homework/assignments (build plan Phase 13, 7.17) — reuses <c>CanViewAcademicRecords</c>, the
    /// same flag report cards/results already gate on, since homework is itself an academic record.
    /// Resolves the student's class from their active enrolment for <paramref name="academicYearId"/>
    /// (an explicit caller-supplied parameter, the same convention <c>GetStudentAttendanceAsync</c>'s
    /// date range already uses, rather than this module inventing a new "resolve the current academic
    /// year" capability nothing else needs yet).
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">No active, Verified, academic-record-visible relationship exists between this guardian and student.</exception>
    Task<IReadOnlyList<Homework>> GetStudentHomeworkAsync(
        Guid guardianPersonId, Guid studentPersonId, Guid academicYearId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approved behaviour incidents only (build plan Phase 13, 7.18) — a Draft/Submitted/UnderReview
    /// incident is still unconfirmed staff work in progress, never shown to a guardian before a
    /// reviewer has actually approved it, the same "published-only" discipline every other portal
    /// read in this module already follows (report cards, evaluations).
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">No active, Verified, behaviour-visible relationship exists between this guardian and student.</exception>
    Task<IReadOnlyList<BehaviourIncidentSummary>> GetStudentBehaviourSummaryAsync(
        Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that this guardian has seen/read something shown to them in the portal (build plan
    /// Phase 13: "parent acknowledgements") — gated only on an active, Verified relationship
    /// existing at all (not a specific <c>CanView*</c> flag), since acknowledging is orthogonal to
    /// which data categories a guardian may see: they can only ever acknowledge an item the portal
    /// itself already chose to show them.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">No active, Verified relationship exists between this guardian and student.</exception>
    Task<Guid> AcknowledgeAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <exception cref="UnauthorizedAccessException">No active, Verified relationship exists between this guardian and student.</exception>
    Task<GuardianAcknowledgement?> GetAcknowledgementAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default);
}

namespace HAMS.ReportingAnalyticsAudit.Application;

public sealed record AcademicYearOption(Guid AcademicYearId, string Code, string Name);

public sealed record EnrollmentByGrade(string GradeName, int StudentCount);

public sealed record AttendanceRateSummary(int TotalRecords, int PresentCount, double PresentRatePercent);

public sealed record InterventionTypeCaseCounts(string InterventionTypeName, int OpenCount, int ClosedCount);

public sealed record PromotionSummary(int PromotedCount, int NotPromotedCount);

public sealed record DashboardSnapshot(
    int TotalActiveStudents,
    IReadOnlyList<EnrollmentByGrade> EnrollmentByGrade,
    AttendanceRateSummary AttendanceLast30Days,
    IReadOnlyList<InterventionTypeCaseCounts> InterventionCasesByType,
    PromotionSummary PromotionDecisions);

/// <summary>
/// Management dashboard aggregation (build plan Phase 12) — every number here is computed from the
/// read-only cross-schema SQL views this module owns for exactly this purpose (§2's stated
/// exception), never by reaching into another module's DbContext directly. Intervention case
/// counts deliberately never resolve to a student — see <c>InterventionCaseSummaryRow</c>'s remarks.
/// </summary>
public interface IDashboardQueryService
{
    /// <summary>Every academic year with at least one enrolment on record, most recent first — feeds the dashboard's year selector.</summary>
    Task<IReadOnlyList<AcademicYearOption>> GetAvailableAcademicYearsAsync(CancellationToken cancellationToken = default);

    Task<DashboardSnapshot> GetSnapshotAsync(Guid academicYearId, CancellationToken cancellationToken = default);
}

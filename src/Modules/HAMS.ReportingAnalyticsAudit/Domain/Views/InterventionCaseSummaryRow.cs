namespace HAMS.ReportingAnalyticsAudit.Domain.Views;

/// <summary>
/// Pre-aggregated at the SQL level (<c>reporting.vw_InterventionCaseSummary</c> groups by type+status
/// itself) — deliberately never row-level/student-identifiable, unlike the other view-row types.
/// Intervention cases are confidential records gated by <c>IConfidentialRecordAccessor</c> (Phase 9);
/// a dashboard headline count ("N cases of type X currently open") doesn't need — and must not
/// bypass — that per-case gate, so this view only ever surfaces counts, never a case or a student.
/// </summary>
public sealed class InterventionCaseSummaryRow
{
    public Guid AcademicYearId { get; init; }
    public Guid InterventionTypeId { get; init; }
    public required string InterventionTypeCode { get; init; }
    public required string InterventionTypeName { get; init; }
    public required string Status { get; init; }
    public int CaseCount { get; init; }
}

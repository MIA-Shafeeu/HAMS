namespace HAMS.ReportingAnalyticsAudit.Application;

/// <summary>
/// A packaging choice (CSV vs. a real spreadsheet), not configurable business data — the no-enums
/// rule's own carve-out for genuinely structural/technical concepts applies here the same way it
/// does to <c>Platform.Workflow</c>'s action verbs (build plan §1.6).
/// </summary>
public enum ExportFormat
{
    Csv,
    Xlsx,
}

public sealed record GeneratedExport(byte[] Content, string FileName, string ContentType);

/// <summary>
/// Regulatory reporting (build plan Phase 12 — "PDF/CSV/spreadsheet regulatory exports"). Each
/// method sources from this module's own read-only cross-schema SQL views (§2's stated exception),
/// the same views the management dashboard aggregates from — one set of views, two consumers.
/// PDF isn't offered here: Phase 11 already established the PDF path (report cards) for genuinely
/// document-shaped output; these three reports are tabular data, where CSV/XLSX is the right format.
/// </summary>
public interface IRegulatoryReportingService
{
    Task<GeneratedExport> ExportStudentRosterAsync(Guid academicYearId, ExportFormat format, CancellationToken cancellationToken = default);

    Task<GeneratedExport> ExportAttendanceSummaryAsync(
        Guid academicYearId, DateOnly fromDate, DateOnly toDate, ExportFormat format, CancellationToken cancellationToken = default);

    Task<GeneratedExport> ExportPromotionDecisionsAsync(Guid academicYearId, ExportFormat format, CancellationToken cancellationToken = default);
}

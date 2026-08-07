using HAMS.ReportingAnalyticsAudit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.ReportingAnalyticsAudit.Application;

internal sealed class RegulatoryReportingService(ReportingAnalyticsAuditDbContext dbContext) : IRegulatoryReportingService
{
    private const string CsvContentType = "text/csv";
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<GeneratedExport> ExportStudentRosterAsync(Guid academicYearId, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var rosterRows = await dbContext.StudentRoster
            .Where(r => r.AcademicYearId == academicYearId)
            .OrderBy(r => r.GradeName).ThenBy(r => r.StudentNameEn)
            .ToListAsync(cancellationToken);

        string[] headers = ["Admission Number", "Student Name (En)", "Student Name (Dv)", "Grade", "Class", "Academic Year", "Enrolled From", "Enrolled To"];
        var rows = rosterRows.Select(r => new[]
        {
            r.AdmissionNumber, r.StudentNameEn, r.StudentNameDv ?? "", r.GradeName, r.ClassName, r.AcademicYearName,
            r.EffectiveFrom.ToString("yyyy-MM-dd"), r.EffectiveTo?.ToString("yyyy-MM-dd") ?? "",
        }).ToList();

        return Build("student-roster", headers, rows, format);
    }

    public async Task<GeneratedExport> ExportAttendanceSummaryAsync(
        Guid academicYearId, DateOnly fromDate, DateOnly toDate, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var attendanceRows = await dbContext.AttendanceRecords
            .Where(a => a.AcademicYearId == academicYearId && a.Date >= fromDate && a.Date <= toDate)
            .OrderBy(a => a.Date).ThenBy(a => a.StudentNameEn)
            .ToListAsync(cancellationToken);

        string[] headers = ["Date", "Student Name (En)", "Student Name (Dv)", "Attendance Status"];
        var rows = attendanceRows.Select(a => new[]
        {
            a.Date.ToString("yyyy-MM-dd"), a.StudentNameEn, a.StudentNameDv ?? "", a.AttendanceStatusName,
        }).ToList();

        return Build("attendance-summary", headers, rows, format);
    }

    public async Task<GeneratedExport> ExportPromotionDecisionsAsync(Guid academicYearId, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var decisionRows = await dbContext.PromotionDecisions
            .Where(p => p.AcademicYearId == academicYearId)
            .OrderBy(p => p.StudentNameEn)
            .ToListAsync(cancellationToken);

        string[] headers = ["Student Name (En)", "Student Name (Dv)", "Academic Year", "Current Grade", "Promoted", "Next Grade", "Decision Date", "Notes"];
        var rows = decisionRows.Select(p => new[]
        {
            p.StudentNameEn, p.StudentNameDv ?? "", p.AcademicYearCode, p.CurrentGradeName, p.Promoted ? "Yes" : "No",
            p.NextGradeName ?? "", p.DecisionDate.ToString("yyyy-MM-dd"), p.Notes ?? "",
        }).ToList();

        return Build("promotion-decisions", headers, rows, format);
    }

    private static GeneratedExport Build(string baseFileName, string[] headers, IReadOnlyList<string[]> rows, ExportFormat format) => format switch
    {
        ExportFormat.Csv => new GeneratedExport(TabularExportBuilder.BuildCsv(headers, rows), $"{baseFileName}.csv", CsvContentType),
        ExportFormat.Xlsx => new GeneratedExport(TabularExportBuilder.BuildXlsx(baseFileName, headers, rows), $"{baseFileName}.xlsx", XlsxContentType),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format."),
    };
}

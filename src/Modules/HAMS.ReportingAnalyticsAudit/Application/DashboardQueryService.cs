using HAMS.ReportingAnalyticsAudit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.ReportingAnalyticsAudit.Application;

internal sealed class DashboardQueryService(ReportingAnalyticsAuditDbContext dbContext) : IDashboardQueryService
{
    public async Task<IReadOnlyList<AcademicYearOption>> GetAvailableAcademicYearsAsync(CancellationToken cancellationToken = default)
    {
        // Selecting straight into the AcademicYearOption record before Distinct() doesn't translate
        // (EF Core can't express record-type equality in a SQL DISTINCT) — project an anonymous
        // type server-side instead, then map to the record client-side after materializing.
        var rows = await dbContext.StudentRoster
            .Select(r => new { r.AcademicYearId, r.AcademicYearCode, r.AcademicYearName })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new AcademicYearOption(r.AcademicYearId, r.AcademicYearCode, r.AcademicYearName))
            .OrderByDescending(y => y.Code)
            .ToList();
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(Guid academicYearId, CancellationToken cancellationToken = default)
    {
        var roster = await dbContext.StudentRoster
            .Where(r => r.AcademicYearId == academicYearId)
            .Select(r => new { r.StudentPersonId, r.GradeName })
            .ToListAsync(cancellationToken);

        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var attendanceStatusCodes = await dbContext.AttendanceRecords
            .Where(a => a.AcademicYearId == academicYearId && a.Date >= since)
            .Select(a => a.AttendanceStatusCode)
            .ToListAsync(cancellationToken);

        var interventionRows = await dbContext.InterventionCaseSummary
            .Where(r => r.AcademicYearId == academicYearId)
            .Select(r => new { r.InterventionTypeName, r.Status, r.CaseCount })
            .ToListAsync(cancellationToken);

        var promotionOutcomes = await dbContext.PromotionDecisions
            .Where(p => p.AcademicYearId == academicYearId)
            .Select(p => p.Promoted)
            .ToListAsync(cancellationToken);

        return DashboardSnapshotCalculator.Calculate(
            roster.Select(r => (r.StudentPersonId, r.GradeName)).ToList(),
            attendanceStatusCodes,
            interventionRows.Select(r => (r.InterventionTypeName, r.Status, r.CaseCount)).ToList(),
            promotionOutcomes);
    }
}

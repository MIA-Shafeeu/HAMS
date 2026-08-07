namespace HAMS.ReportingAnalyticsAudit.Application;

/// <summary>
/// The real business logic behind <see cref="IDashboardQueryService.GetSnapshotAsync"/> — grouping,
/// distinct-student counting, and rate math — split out from the SQL-view fetch specifically so it
/// can be unit-tested against plain in-memory data, without needing a relational provider that
/// understands <c>ToView(...)</c> (the views themselves are verified live, per this codebase's
/// standing practice for provider-specific behaviour the InMemory EF Core provider can't represent).
/// </summary>
internal static class DashboardSnapshotCalculator
{
    public const string PresentStatusCode = "PRESENT";
    public const string OpenCaseStatus = "Open";
    public const string ClosedCaseStatus = "Closed";

    public static DashboardSnapshot Calculate(
        IReadOnlyList<(Guid StudentPersonId, string GradeName)> roster,
        IReadOnlyList<string> attendanceStatusCodes,
        IReadOnlyList<(string InterventionTypeName, string Status, int CaseCount)> interventionRows,
        IReadOnlyList<bool> promotionOutcomes)
    {
        var totalActiveStudents = roster.Select(r => r.StudentPersonId).Distinct().Count();
        var enrollmentByGrade = roster
            .GroupBy(r => r.GradeName)
            .Select(g => new EnrollmentByGrade(g.Key, g.Select(r => r.StudentPersonId).Distinct().Count()))
            .OrderBy(e => e.GradeName)
            .ToList();

        var totalRecords = attendanceStatusCodes.Count;
        var presentCount = attendanceStatusCodes.Count(code => code == PresentStatusCode);
        var attendanceSummary = new AttendanceRateSummary(
            totalRecords, presentCount, totalRecords == 0 ? 0 : Math.Round(100.0 * presentCount / totalRecords, 1));

        var interventionByType = interventionRows
            .GroupBy(r => r.InterventionTypeName)
            .Select(g => new InterventionTypeCaseCounts(
                g.Key,
                g.Where(r => r.Status == OpenCaseStatus).Sum(r => r.CaseCount),
                g.Where(r => r.Status == ClosedCaseStatus).Sum(r => r.CaseCount)))
            .OrderBy(c => c.InterventionTypeName)
            .ToList();

        var promotionSummary = new PromotionSummary(promotionOutcomes.Count(p => p), promotionOutcomes.Count(p => !p));

        return new DashboardSnapshot(totalActiveStudents, enrollmentByGrade, attendanceSummary, interventionByType, promotionSummary);
    }
}

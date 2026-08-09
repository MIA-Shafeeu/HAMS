using HAMS.ReportingAnalyticsAudit.Application;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class ReportsModel(IDashboardQueryService dashboardQuery) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly AttendanceFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

    [BindProperty(SupportsGet = true)]
    public DateOnly AttendanceTo { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];

    public async Task OnGetAsync()
    {
        AcademicYears = await dashboardQuery.GetAvailableAcademicYearsAsync();
        if (AcademicYearId == Guid.Empty && AcademicYears.Count > 0)
        {
            AcademicYearId = AcademicYears[0].AcademicYearId;
        }
    }

    public string BuildRosterUrl(string format)
        => $"/api/v1/reporting/exports/student-roster?academicYearId={AcademicYearId}&format={format}";

    public string BuildAttendanceUrl(string format)
        => $"/api/v1/reporting/exports/attendance-summary?academicYearId={AcademicYearId}" +
           $"&fromDate={AttendanceFrom:yyyy-MM-dd}&toDate={AttendanceTo:yyyy-MM-dd}&format={format}";

    public string BuildPromotionUrl(string format)
        => $"/api/v1/reporting/exports/promotion-decisions?academicYearId={AcademicYearId}&format={format}";
}

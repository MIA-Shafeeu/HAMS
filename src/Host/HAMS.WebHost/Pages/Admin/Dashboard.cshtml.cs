using HAMS.ReportingAnalyticsAudit.Application;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class DashboardModel(IDashboardQueryService dashboardQuery) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];
    public DashboardSnapshot? Snapshot { get; private set; }

    public async Task OnGetAsync()
    {
        AcademicYears = await dashboardQuery.GetAvailableAcademicYearsAsync();

        if (AcademicYearId == Guid.Empty && AcademicYears.Count > 0)
        {
            AcademicYearId = AcademicYears[0].AcademicYearId;
        }

        if (AcademicYearId != Guid.Empty)
        {
            Snapshot = await dashboardQuery.GetSnapshotAsync(AcademicYearId);
        }
    }
}

using HAMS.Attendance.Application;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Staff;

/// <summary>
/// Razor Pages migration of the old Blazor <c>Attendance.razor</c> (build plan's frontend-migration
/// effort — MudBlazor's focus/click-reliability bugs, see the migration plan doc). No tabs on this
/// page (unlike the Admin pages migrated so far): a single School -&gt; Academic Year -&gt; Class
/// cascade plus a date filter, then an "always-editable" roster where every row's Status/Notes are
/// live inputs at once (never a per-row edit-toggle), so the whole table is one POST body with
/// array-indexed field names (<c>Rows[i].StudentPersonId</c> etc.) bound straight to <see cref="Rows"/>.
/// </summary>
[Authorize(Policy = StaffPolicy.Name)]
public sealed class AttendanceModel(
    IOrgStructureLookup orgLookup,
    IStudentEnrollmentService enrollmentService,
    IAttendanceQueryService attendanceQuery,
    IAttendanceService attendanceService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public IReadOnlyList<SchoolOption> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];
    public IReadOnlyList<ClassOption> Classes { get; private set; } = [];
    public IReadOnlyList<AttendanceStatusOption> Statuses { get; private set; } = [];
    public IReadOnlyList<RosterRow> Roster { get; private set; } = [];

    // POST body for the single "Save All" form - array-indexed (Rows[0].StudentPersonId, ...) so
    // ASP.NET Core's default model binder reconstructs the whole roster's edits from one submit,
    // matching every row's Status/Notes inputs being live simultaneously (no per-row edit toggle).
    [BindProperty]
    public List<RosterRowInput> Rows { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // Replicates the old Blazor OnInitializedAsync/OnSchoolChangedAsync/OnAcademicYearChangedAsync
    // auto-select-first-option behavior: each level only auto-picks the first option when nothing is
    // selected yet (an empty Guid) - it never overrides an explicit selection, the same "don't try to
    // be clever about stale cross-cascade values" approach OrgStructureModel.LoadAllAsync takes.
    private async Task LoadAsync()
    {
        Schools = await orgLookup.GetSchoolsAsync();
        if (SchoolId == Guid.Empty && Schools.Count > 0)
        {
            SchoolId = Schools[0].Id;
        }

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);
            if (AcademicYearId == Guid.Empty && AcademicYears.Count > 0)
            {
                AcademicYearId = AcademicYears[0].Id;
            }
        }

        if (AcademicYearId != Guid.Empty)
        {
            Classes = await orgLookup.GetClassesAsync(AcademicYearId);
            if (ClassId == Guid.Empty && Classes.Count > 0)
            {
                ClassId = Classes[0].Id;
            }
        }

        Statuses = await attendanceQuery.GetStatusesAsync();

        if (ClassId != Guid.Empty)
        {
            await LoadRosterAsync();
        }
    }

    private async Task LoadRosterAsync()
    {
        var roster = await enrollmentService.GetActiveRosterForClassAsync(ClassId, Date);
        var existing = await attendanceQuery.GetDailyRecordsForStudentsAsync(
            roster.Select(r => r.StudentPersonId).ToList(), Date);
        var existingByStudent = existing.ToDictionary(e => e.StudentPersonId, e => e.AttendanceStatusCode);
        var defaultStatus = Statuses.FirstOrDefault(s => s.Code == "PRESENT")?.Code ?? Statuses.FirstOrDefault()?.Code ?? "";

        Roster = roster
            .Select(r => new RosterRow(
                r.StudentPersonId,
                r.NameEn,
                r.NameDv,
                r.AdmissionNumber,
                existingByStudent.TryGetValue(r.StudentPersonId, out var code) ? code : defaultStatus))
            .ToList();
    }

    public async Task<IActionResult> OnPostSaveAttendanceAsync()
    {
        var personIdValue = User.FindFirst(HamsClaimTypes.PersonId)?.Value;
        if (!Guid.TryParse(personIdValue, out var recordedByPersonId))
        {
            TempData["FlashMessage"] = "Could not resolve your staff profile.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        var failureCount = 0;
        foreach (var row in Rows)
        {
            try
            {
                await attendanceService.MarkDailyAttendanceAsync(
                    SchoolId, row.StudentPersonId, Date, AcademicYearId, row.StatusCode, recordedByPersonId, row.Notes);
            }
            catch (InvalidOperationException)
            {
                failureCount++;
            }
        }

        if (failureCount > 0)
        {
            TempData["FlashMessage"] = $"{Date:yyyy-MM-dd} isn't a school day — no attendance was recorded.";
            TempData["FlashSeverity"] = "warning";
        }
        else
        {
            TempData["FlashMessage"] = "Attendance saved.";
            TempData["FlashSeverity"] = "success";
        }

        return BackToScope();
    }

    private RedirectToPageResult BackToScope() =>
        RedirectToPage(new { SchoolId, AcademicYearId, ClassId, Date });

    public sealed record RosterRow(Guid StudentPersonId, string NameEn, string NameDv, string AdmissionNumber, string StatusCode);

    public sealed class RosterRowInput
    {
        public Guid StudentPersonId { get; set; }
        public string StatusCode { get; set; } = "";
        public string? Notes { get; set; }
    }
}

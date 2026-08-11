using HAMS.Attendance.Application;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Common.Contracts;
using HAMS.TeachingTimetable.Application;
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
    IAttendanceService attendanceService,
    IStaffAccessScopeQuery scopeQuery,
    IClock clock) : PageModel
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

    /// <summary>False when <see cref="ClassId"/> is set but isn't one of the caller's assigned
    /// classes (e.g. a stale link, or a directly-edited query string) - the roster is then not
    /// loaded at all, and the page shows an access-denied message instead of silently rendering
    /// nothing. Always true once <see cref="ClassId"/> is empty (nothing selected yet to deny).</summary>
    public bool ClassAccessAuthorized { get; private set; } = true;

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
        var personId = ResolvePersonId();

        // Resolved twice, deliberately: once against just the caller's accessible Schools (before
        // an Academic Year is even chosen - GetScopeAsync's own null-schoolId shortcut skips the
        // OrgCurriculum/SubjectTeachingAssignment joins entirely for this cheap first pass), then
        // again scoped to whichever School+Year end up selected, once Classes need filtering too.
        var schoolScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId: null, academicYearId: null);

        var allSchools = await orgLookup.GetSchoolsAsync();
        Schools = schoolScope.HasUnrestrictedAccess ? allSchools : [.. allSchools.Where(s => schoolScope.CanAccessSchool(s.Id))];
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

        StaffAccessScope? fullScope = null;
        if (SchoolId != Guid.Empty && AcademicYearId != Guid.Empty)
        {
            fullScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, SchoolId, AcademicYearId);

            var allClasses = await orgLookup.GetClassesAsync(AcademicYearId);
            Classes = fullScope.HasUnrestrictedAccess ? allClasses : [.. allClasses.Where(c => fullScope.CanAccessClass(c.Id))];
            if (ClassId == Guid.Empty && Classes.Count > 0)
            {
                ClassId = Classes[0].Id;
            }
        }

        Statuses = await attendanceQuery.GetStatusesAsync();

        if (ClassId != Guid.Empty)
        {
            ClassAccessAuthorized = fullScope?.CanAccessClass(ClassId) ?? false;
            if (ClassAccessAuthorized)
            {
                await LoadRosterAsync();
            }
        }
    }

    private Guid ResolvePersonId() =>
        Guid.TryParse(User.FindFirst(HamsClaimTypes.PersonId)?.Value, out var personId) ? personId : Guid.Empty;

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
        var recordedByPersonId = ResolvePersonId();
        if (recordedByPersonId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Could not resolve your staff profile.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        // Re-derived from a fresh scope check, never trusted from the posted ClassId alone - a
        // Razor Page POST handler is a directly-callable HTTP endpoint, unlike the old Blazor
        // Server circuit event this page was migrated from, so the class picker only being
        // filtered client-side would not be enough on its own to stop a tampered ClassId.
        var scope = await scopeQuery.GetScopeAsync(recordedByPersonId, clock.TodayUtc, SchoolId, AcademicYearId);
        if (!scope.CanAccessClass(ClassId))
        {
            TempData["FlashMessage"] = "You do not have access to this class.";
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

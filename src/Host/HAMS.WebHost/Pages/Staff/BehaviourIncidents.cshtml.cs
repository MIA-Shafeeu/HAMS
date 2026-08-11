using System.Security.Claims;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Intervention.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;
using HAMS.TeachingTimetable.Application;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Staff;

[Authorize(Policy = StaffPolicy.Name)]
public sealed class BehaviourIncidentsModel(
    IOrgStructureLookup orgLookup,
    IStudentEnrollmentService enrollmentService,
    IBehaviourCategoryLookup categoryLookup,
    IBehaviourIncidentService incidentService,
    IStaffAccessScopeQuery scopeQuery,
    IClock clock) : PageModel
{
    // ---- Cascading scope: School -> Academic Year -> Class -> Student ----
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid StudentId { get; set; }

    public IReadOnlyList<SchoolOption> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];
    public IReadOnlyList<ClassOption> Classes { get; private set; } = [];
    public IReadOnlyList<ClassRosterEntry> Roster { get; private set; } = [];
    public IReadOnlyList<BehaviourCategoryOption> Categories { get; private set; } = [];
    public List<IncidentRow> Incidents { get; private set; } = [];

    /// <summary>False when <see cref="ClassId"/> is set but isn't one of the caller's assigned
    /// classes (e.g. a stale link, or a directly-edited query string) - the roster (and everything
    /// downstream of it: student selection, incident history) is then not loaded at all, and the
    /// page shows an access-denied message instead of silently rendering nothing. Always true once
    /// <see cref="ClassId"/> is empty (nothing selected yet to deny). Mirrors Attendance's guard.</summary>
    public bool ClassAccessAuthorized { get; private set; } = true;

    [BindProperty]
    public NewIncidentInput NewIncident { get; set; } = new();

    public sealed record IncidentRow(Guid Id, string CategoryName, string Description, DateOnly OccurredDate, WorkflowStatus Status);

    public sealed class NewIncidentInput
    {
        public Guid CategoryId { get; set; }
        public string ConfidentialityTierCode { get; set; } = ConfidentialityTierCodes.Restricted;
        public DateOnly OccurredDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string Description { get; set; } = "";
    }

    private Guid? CurrentPersonId => Guid.TryParse(User.FindFirstValue(HamsClaimTypes.PersonId), out var id) ? id : null;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // Mirrors the original Blazor page's OnSchoolChangedAsync/OnAcademicYearChangedAsync/
    // OnClassChangedAsync cascade: whenever a parent level's selection is empty or no longer valid
    // for the newly-loaded parent, the first option at that level is auto-selected and every
    // downstream level is reloaded from it — same "auto-select-first" behaviour, just re-run as one
    // full-page GET instead of four separate ValueChanged handlers.
    private async Task LoadAsync()
    {
        var personId = ResolvePersonId();

        // Resolved twice, deliberately: once against just the caller's accessible Schools (before
        // an Academic Year is even chosen), then again scoped to whichever School+Year end up
        // selected, once Classes need filtering too - mirrors Attendance's LoadAsync.
        var schoolScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId: null, academicYearId: null);

        var allSchools = await orgLookup.GetSchoolsAsync();
        Schools = schoolScope.HasUnrestrictedAccess ? allSchools : [.. allSchools.Where(s => schoolScope.CanAccessSchool(s.Id))];
        Categories = await categoryLookup.GetAllAsync();
        if (NewIncident.CategoryId == Guid.Empty && Categories.Count > 0)
        {
            NewIncident.CategoryId = Categories[0].Id;
        }

        if (SchoolId == Guid.Empty || Schools.All(s => s.Id != SchoolId))
        {
            SchoolId = Schools.Count > 0 ? Schools[0].Id : Guid.Empty;
        }

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);
        }

        if (AcademicYearId == Guid.Empty || AcademicYears.All(y => y.Id != AcademicYearId))
        {
            AcademicYearId = AcademicYears.Count > 0 ? AcademicYears[0].Id : Guid.Empty;
        }

        StaffAccessScope? fullScope = null;
        if (AcademicYearId != Guid.Empty)
        {
            fullScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, SchoolId, AcademicYearId);

            var allClasses = await orgLookup.GetClassesAsync(AcademicYearId);
            Classes = fullScope.HasUnrestrictedAccess ? allClasses : [.. allClasses.Where(c => fullScope.CanAccessClass(c.Id))];
        }

        if (ClassId == Guid.Empty || Classes.All(c => c.Id != ClassId))
        {
            ClassId = Classes.Count > 0 ? Classes[0].Id : Guid.Empty;
        }

        if (ClassId != Guid.Empty)
        {
            ClassAccessAuthorized = fullScope?.CanAccessClass(ClassId) ?? false;
            if (ClassAccessAuthorized)
            {
                Roster = await enrollmentService.GetActiveRosterForClassAsync(ClassId, DateOnly.FromDateTime(DateTime.Today));
            }
        }

        if (StudentId == Guid.Empty || Roster.All(r => r.StudentPersonId != StudentId))
        {
            StudentId = Roster.Count > 0 ? Roster[0].StudentPersonId : Guid.Empty;
        }

        if (StudentId != Guid.Empty)
        {
            await LoadIncidentsAsync();
        }
    }

    private Guid ResolvePersonId() =>
        Guid.TryParse(User.FindFirst(HamsClaimTypes.PersonId)?.Value, out var personId) ? personId : Guid.Empty;

    private async Task LoadIncidentsAsync()
    {
        var incidents = await incidentService.GetForStudentAsync(StudentId);
        var rows = new List<IncidentRow>();
        foreach (var incident in incidents)
        {
            var category = await categoryLookup.GetAsync(incident.BehaviourCategoryId);
            rows.Add(new IncidentRow(incident.Id, category?.Name ?? "(unknown)", incident.Description, incident.OccurredDate, incident.Status));
        }

        Incidents = rows;
    }

    private RedirectToPageResult BackToScope() => RedirectToPage(new { SchoolId, AcademicYearId, ClassId, StudentId });

    public async Task<IActionResult> OnPostRecordAsync()
    {
        if (StudentId == Guid.Empty || AcademicYearId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Choose a school, academic year, class and student to continue.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        if (CurrentPersonId is not { } recordedBy)
        {
            TempData["FlashMessage"] = "Could not resolve the current user.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        // Re-derived from a fresh scope check, never trusted from the posted ClassId alone - a
        // Razor Page POST handler is a directly-callable HTTP endpoint, so the class picker only
        // being filtered client-side would not be enough on its own to stop a tampered ClassId.
        // Mirrors Attendance's OnPostSaveAttendanceAsync.
        var scope = await scopeQuery.GetScopeAsync(recordedBy, clock.TodayUtc, SchoolId, AcademicYearId);
        if (!scope.CanAccessClass(ClassId))
        {
            TempData["FlashMessage"] = "You do not have access to this class.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        if (string.IsNullOrWhiteSpace(NewIncident.Description))
        {
            TempData["FlashMessage"] = "A description is required.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        await incidentService.RecordAsync(
            StudentId, NewIncident.CategoryId, null, AcademicYearId, NewIncident.Description,
            NewIncident.ConfidentialityTierCode, recordedBy, NewIncident.OccurredDate);

        TempData["FlashMessage"] = "Incident recorded.";
        TempData["FlashSeverity"] = "success";
        return BackToScope();
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id)
    {
        if (CurrentPersonId is not { } personId)
        {
            return await NoCurrentUserAsync();
        }

        if (!await IsAuthorizedForIncidentAsync(id, personId))
        {
            return await AccessDeniedAsync();
        }

        return await RunTransitionAsync(() => incidentService.SubmitAsync(id), "Incident submitted.");
    }

    public async Task<IActionResult> OnPostBeginReviewAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return await NoCurrentUserAsync();
        }

        if (!await IsAuthorizedForIncidentAsync(id, reviewedBy))
        {
            return await AccessDeniedAsync();
        }

        return await RunTransitionAsync(() => incidentService.BeginReviewAsync(id, reviewedBy), "Review started.");
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return await NoCurrentUserAsync();
        }

        if (!await IsAuthorizedForIncidentAsync(id, reviewedBy))
        {
            return await AccessDeniedAsync();
        }

        return await RunTransitionAsync(() => incidentService.ApproveAsync(id, reviewedBy, null, null), "Incident approved.");
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return await NoCurrentUserAsync();
        }

        if (!await IsAuthorizedForIncidentAsync(id, reviewedBy))
        {
            return await AccessDeniedAsync();
        }

        return await RunTransitionAsync(() => incidentService.RejectAsync(id, reviewedBy, null), "Incident rejected.");
    }

    public async Task<IActionResult> OnPostReturnAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return await NoCurrentUserAsync();
        }

        if (!await IsAuthorizedForIncidentAsync(id, reviewedBy))
        {
            return await AccessDeniedAsync();
        }

        return await RunTransitionAsync(() => incidentService.ReturnAsync(id, reviewedBy, null), "Incident returned.");
    }

    /// <summary>
    /// Re-derives the caller's access to an EXISTING incident from scratch - never trusted from
    /// whatever the page happened to have loaded for a DIFFERENT class/student, since the posted
    /// <paramref name="incidentId"/> is an independent input a caller could point at any incident
    /// system-wide. A <see cref="BehaviourIncident"/> carries no ClassId/SchoolId of its own (only
    /// <c>StudentPersonId</c>+<c>AcademicYearId</c>), so its class is resolved the same way the
    /// page's own roster is built: the student's active enrolment for that year.
    /// </summary>
    private async Task<bool> IsAuthorizedForIncidentAsync(Guid incidentId, Guid personId)
    {
        var incident = await incidentService.GetAsync(incidentId);
        if (incident is null)
        {
            return false;
        }

        var enrollment = await enrollmentService.GetActiveEnrollmentAsync(incident.StudentPersonId, incident.AcademicYearId, clock.TodayUtc);
        if (enrollment is null || await orgLookup.GetClassSchoolIdAsync(enrollment.ClassId) is not { } schoolId)
        {
            return false;
        }

        var scope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId, incident.AcademicYearId);
        return scope.CanAccessClass(enrollment.ClassId);
    }

    private Task<IActionResult> NoCurrentUserAsync()
    {
        TempData["FlashMessage"] = "Could not resolve the current user.";
        TempData["FlashSeverity"] = "danger";
        return Task.FromResult<IActionResult>(BackToScope());
    }

    private Task<IActionResult> AccessDeniedAsync()
    {
        TempData["FlashMessage"] = "You do not have access to this incident.";
        TempData["FlashSeverity"] = "danger";
        return Task.FromResult<IActionResult>(BackToScope());
    }

    private async Task<IActionResult> RunTransitionAsync(Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            TempData["FlashMessage"] = successMessage;
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToScope();
    }
}

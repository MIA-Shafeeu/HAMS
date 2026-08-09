using System.Security.Claims;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Intervention.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Workflow.Domain;
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
    IBehaviourIncidentService incidentService) : PageModel
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
        Schools = await orgLookup.GetSchoolsAsync();
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

        if (AcademicYearId != Guid.Empty)
        {
            Classes = await orgLookup.GetClassesAsync(AcademicYearId);
        }

        if (ClassId == Guid.Empty || Classes.All(c => c.Id != ClassId))
        {
            ClassId = Classes.Count > 0 ? Classes[0].Id : Guid.Empty;
        }

        if (ClassId != Guid.Empty)
        {
            Roster = await enrollmentService.GetActiveRosterForClassAsync(ClassId, DateOnly.FromDateTime(DateTime.Today));
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

    public Task<IActionResult> OnPostSubmitAsync(Guid id) =>
        RunTransitionAsync(() => incidentService.SubmitAsync(id), "Incident submitted.");

    public Task<IActionResult> OnPostBeginReviewAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return NoCurrentUserAsync();
        }

        return RunTransitionAsync(() => incidentService.BeginReviewAsync(id, reviewedBy), "Review started.");
    }

    public Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return NoCurrentUserAsync();
        }

        return RunTransitionAsync(() => incidentService.ApproveAsync(id, reviewedBy, null, null), "Incident approved.");
    }

    public Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return NoCurrentUserAsync();
        }

        return RunTransitionAsync(() => incidentService.RejectAsync(id, reviewedBy, null), "Incident rejected.");
    }

    public Task<IActionResult> OnPostReturnAsync(Guid id)
    {
        if (CurrentPersonId is not { } reviewedBy)
        {
            return NoCurrentUserAsync();
        }

        return RunTransitionAsync(() => incidentService.ReturnAsync(id, reviewedBy, null), "Incident returned.");
    }

    private Task<IActionResult> NoCurrentUserAsync()
    {
        TempData["FlashMessage"] = "Could not resolve the current user.";
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

using System.Security.Claims;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Intervention.Application;
using HAMS.Intervention.Domain;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Staff;

[Authorize(Policy = StaffPolicy.Name)]
public sealed class InterventionCasesModel(
    IOrgStructureLookup orgLookup,
    IStudentEnrollmentService enrollmentService,
    ISubjectLookup subjectLookup,
    IInterventionCaseService caseService,
    IConfidentialRecordAccessor confidentialAccessor) : PageModel
{
    // ---- Scope cascade (School -> Academic Year -> Class -> Student), each level its own tiny GET
    // form so picking a new value at one level naturally drops every level below it from the query
    // string (mirrors the original Blazor OnSchoolChangedAsync/OnAcademicYearChangedAsync/etc., which
    // explicitly reset every downstream selection) ----
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid StudentId { get; set; }

    // Which case's detail panel is expanded - just navigation (a GET), same as OrgStructure's
    // Edit/Manage links.
    [BindProperty(SupportsGet = true)]
    public Guid? SelectedCaseId { get; set; }

    public IReadOnlyList<SchoolOption> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];
    public IReadOnlyList<ClassOption> Classes { get; private set; } = [];
    public IReadOnlyList<ClassRosterEntry> Roster { get; private set; } = [];
    public IReadOnlyList<SubjectOption> Subjects { get; private set; } = [];
    public IReadOnlyList<InterventionTypeOption> InterventionTypes { get; private set; } = [];

    public IReadOnlyList<CaseRow> Cases { get; private set; } = [];

    // ---- Selected case detail - populated ONLY when the confidentiality check below passes ----
    public bool SelectedCaseAuthorized { get; private set; }
    public InterventionCaseStatus SelectedCaseStatus { get; private set; }
    public IReadOnlyList<InterventionPlan> Plans { get; private set; } = [];
    public IReadOnlyList<ReassessmentAttempt> ReassessmentAttempts { get; private set; } = [];

    [BindProperty] public NewCaseInput NewCase { get; set; } = new();
    [BindProperty] public NewPlanInput NewPlan { get; set; } = new();

    public sealed record CaseRow(InterventionCase Case, string SubjectName);

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        Schools = await orgLookup.GetSchoolsAsync();
        InterventionTypes = await caseService.GetActiveInterventionTypesAsync();

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);
            Subjects = await orgLookup.GetSubjectsAsync(SchoolId);
        }

        if (AcademicYearId != Guid.Empty)
        {
            Classes = await orgLookup.GetClassesAsync(AcademicYearId);
        }

        if (ClassId != Guid.Empty)
        {
            Roster = await enrollmentService.GetActiveRosterForClassAsync(ClassId, DateOnly.FromDateTime(DateTime.Today));
        }

        if (StudentId != Guid.Empty)
        {
            await LoadCasesAsync();
        }

        if (SelectedCaseId is { } caseId)
        {
            await LoadCaseDetailAsync(caseId);
        }
    }

    private async Task LoadCasesAsync()
    {
        var cases = await caseService.GetCasesForStudentAsync(StudentId);
        var rows = new List<CaseRow>();
        foreach (var interventionCase in cases)
        {
            var subjectName = await subjectLookup.GetNameAsync(interventionCase.SubjectId) ?? "(unknown subject)";
            rows.Add(new CaseRow(interventionCase, subjectName));
        }

        Cases = rows;
    }

    /// <summary>
    /// Server-side confidentiality gate - replicates the original Blazor ViewDetailsAsync's exact
    /// check (same method, same 4 arguments: user, resource, entityType, entityId), just moved into
    /// OnGetAsync so it runs BEFORE the page ever renders. <see cref="Plans"/> and
    /// <see cref="ReassessmentAttempts"/> are only ever assigned inside the
    /// "if (SelectedCaseAuthorized)" branch below - if the accessor denies access, this method
    /// returns having never populated them, so there is no confidential data on the page model for
    /// the view to accidentally render even if a future edit added a careless @@if around the markup.
    /// </summary>
    private async Task LoadCaseDetailAsync(Guid caseId)
    {
        var interventionCase = await caseService.GetAsync(caseId);
        if (interventionCase is null)
        {
            SelectedCaseId = null;
            return;
        }

        SelectedCaseStatus = interventionCase.Status;
        SelectedCaseAuthorized = await confidentialAccessor.CanAccessAsync(User, interventionCase, nameof(InterventionCase), caseId.ToString());

        if (!SelectedCaseAuthorized)
        {
            return;
        }

        Plans = await caseService.GetPlansAsync(caseId);
        ReassessmentAttempts = await caseService.GetReassessmentAttemptsAsync(caseId);
    }

    private bool TryGetCurrentPersonId(out Guid personId) =>
        Guid.TryParse(User.FindFirstValue(HamsClaimTypes.PersonId), out personId);

    private RedirectToPageResult BackToScope() =>
        RedirectToPage(new { SchoolId, AcademicYearId, ClassId, StudentId, SelectedCaseId });

    public async Task<IActionResult> OnPostOpenCaseAsync()
    {
        if (StudentId == Guid.Empty || !TryGetCurrentPersonId(out var openedBy))
        {
            TempData["FlashMessage"] = "Select a student before opening a case.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        if (NewCase.SubjectId == Guid.Empty || NewCase.InterventionTypeId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a subject and an intervention type.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        try
        {
            await caseService.OpenCaseAsync(
                StudentId, NewCase.SubjectId, AcademicYearId, NewCase.InterventionTypeId, NewCase.ConfidentialityTierCode,
                null, null, null, openedBy, DateOnly.FromDateTime(DateTime.Today));

            TempData["FlashMessage"] = "Case opened.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToScope();
    }

    public async Task<IActionResult> OnPostAddPlanAsync(Guid caseId)
    {
        if (!await IsAuthorizedForCaseAsync(caseId))
        {
            TempData["FlashMessage"] = "You do not have access to this case.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        if (!TryGetCurrentPersonId(out var assignedTo) || string.IsNullOrWhiteSpace(NewPlan.Description))
        {
            TempData["FlashMessage"] = "Provide a plan description.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        try
        {
            await caseService.CreatePlanAsync(caseId, NewPlan.Description, assignedTo, NewPlan.StartDate, NewPlan.TargetDate, null);
            TempData["FlashMessage"] = "Plan added.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToScope();
    }

    public async Task<IActionResult> OnPostCloseCaseAsync(Guid caseId)
    {
        if (!await IsAuthorizedForCaseAsync(caseId))
        {
            TempData["FlashMessage"] = "You do not have access to this case.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        try
        {
            await caseService.CloseCaseAsync(caseId, DateOnly.FromDateTime(DateTime.Today));
            TempData["FlashMessage"] = "Case closed.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToScope();
    }

    // The original Blazor page only ever hid the Add-Plan/Close-Case buttons from an unauthorized
    // viewer's rendered markup - CaseService.CloseCaseAsync/CreatePlanAsync never re-checked
    // confidentiality themselves. That was already soft (a determined client could invoke the same
    // circuit handler), but a classic Razor Page POST handler is an unambiguous, directly-callable
    // HTTP endpoint - anyone with a valid antiforgery token could POST an arbitrary caseId and bypass
    // the gate entirely if these write handlers didn't check it themselves. Confidentiality is always
    // an explicit, AND-ed check, never implied by having reached a particular page (build plan §4).
    private async Task<bool> IsAuthorizedForCaseAsync(Guid caseId)
    {
        var interventionCase = await caseService.GetAsync(caseId);
        return interventionCase is not null
            && await confidentialAccessor.CanAccessAsync(User, interventionCase, nameof(InterventionCase), caseId.ToString());
    }

    public sealed class NewCaseInput
    {
        public Guid SubjectId { get; set; }
        public Guid InterventionTypeId { get; set; }
        public string ConfidentialityTierCode { get; set; } = ConfidentialityTierCodes.Restricted;
    }

    public sealed class NewPlanInput
    {
        public string Description { get; set; } = "";
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly TargetDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
    }
}

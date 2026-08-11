using System.Security.Claims;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
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
/// Razor Pages migration of the old Blazor <c>Homework.razor</c> (frontend migration plan - MudBlazor's
/// focus/click-reliability bugs). Same School -&gt; Academic Year -&gt; Class cascade as
/// <c>AttendanceModel</c>/<c>BehaviourIncidentsModel</c>, plus a Subject picker used only when assigning
/// new homework (mirrors the original's <c>_newSubjectId</c> - subject never filters the assignment
/// list itself). Drilling into one assignment's submissions is a manual GET-navigated
/// <see cref="SelectedAssignmentId"/>, same shape as <c>InterventionCasesModel</c>'s
/// <c>SelectedCaseId</c>/"View Details" - deliberately never auto-selected. Score/Feedback are
/// always-editable per submission row (never a toggle-edit), so each row is its own tiny POST form
/// using the "inputs outside the form, linked by the HTML <c>form</c> attribute" technique
/// established by <c>OrgStructureModel</c>'s inline-edit rows.
/// </summary>
[Authorize(Policy = StaffPolicy.Name)]
public sealed class HomeworkModel(
    IOrgStructureLookup orgLookup,
    IStudentEnrollmentService enrollmentService,
    IHomeworkService homeworkService,
    IHomeworkSubmissionService submissionService,
    IStaffAccessScopeQuery scopeQuery,
    IClock clock) : PageModel
{
    // ---- Scope cascade (School -> Academic Year -> Class), plus a Subject picker used only when
    // assigning new homework - each a query-string property so a full-page GET reload (via the
    // cascade <form>'s onchange="this.form.submit()") naturally drops SelectedAssignmentId from the
    // URL too, matching the original Blazor page's OnClassChangedAsync explicitly clearing it. ----
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SubjectId { get; set; }

    // Which assignment's submissions are expanded below - just navigation (a GET), same as
    // InterventionCasesModel's SelectedCaseId.
    [BindProperty(SupportsGet = true)]
    public Guid? SelectedAssignmentId { get; set; }

    public IReadOnlyList<SchoolOption> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];
    public IReadOnlyList<ClassOption> Classes { get; private set; } = [];
    public IReadOnlyList<SubjectOption> Subjects { get; private set; } = [];
    public IReadOnlyList<Homework> HomeworkItems { get; private set; } = [];
    public IReadOnlyList<SubmissionRow> SubmissionRows { get; private set; } = [];

    /// <summary>False when <see cref="ClassId"/> is set but isn't one of the caller's assigned
    /// classes (e.g. a stale link, or a directly-edited query string) - homework/submissions are
    /// then not loaded at all, and the page shows an access-denied message instead of silently
    /// rendering nothing. Always true once <see cref="ClassId"/> is empty (nothing selected yet
    /// to deny). Same convention as <c>AttendanceModel.ClassAccessAuthorized</c>.</summary>
    public bool ClassAccessAuthorized { get; private set; } = true;

    [BindProperty]
    public NewHomeworkInput NewHomework { get; set; } = new();

    [BindProperty]
    public GradeSubmissionInput GradeForm { get; set; } = new();

    public sealed record SubmissionRow(
        Guid SubmissionId, string StudentName, string Status, DateTimeOffset SubmittedAtUtc, int? Score, string? FeedbackText);

    public sealed class NewHomeworkInput
    {
        public string TitleEn { get; set; } = "";
        public string TitleDv { get; set; } = "";
        public string InstructionsEn { get; set; } = "";
        public string InstructionsDv { get; set; } = "";
        public DateOnly AssignedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        public int? MaxScore { get; set; }
    }

    public sealed class GradeSubmissionInput
    {
        public Guid SubmissionId { get; set; }
        public int? Score { get; set; }
        public string? FeedbackText { get; set; }
    }

    private Guid? CurrentPersonId => Guid.TryParse(User.FindFirstValue(HamsClaimTypes.PersonId), out var id) ? id : null;

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    // Mirrors the original Blazor page's OnSchoolChangedAsync/OnAcademicYearChangedAsync/
    // OnClassChangedAsync cascade: whenever a parent level's selection is empty or no longer valid for
    // the newly-loaded parent, the first option at that level is auto-selected - same "auto-select-
    // first" behaviour as BehaviourIncidentsModel/InterventionCasesModel, just re-run as one full-page
    // GET instead of separate ValueChanged handlers.
    private async Task LoadAllAsync()
    {
        var personId = CurrentPersonId ?? Guid.Empty;

        // Resolved twice, deliberately: once against just the caller's accessible Schools (before
        // an Academic Year is even chosen), then again scoped to whichever School+Year end up
        // selected, once Classes need filtering too - same two-pass approach as AttendanceModel.
        var schoolScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId: null, academicYearId: null);

        var allSchools = await orgLookup.GetSchoolsAsync();
        Schools = schoolScope.HasUnrestrictedAccess ? allSchools : [.. allSchools.Where(s => schoolScope.CanAccessSchool(s.Id))];
        if (SchoolId == Guid.Empty || Schools.All(s => s.Id != SchoolId))
        {
            SchoolId = Schools.Count > 0 ? Schools[0].Id : Guid.Empty;
        }

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);
            Subjects = await orgLookup.GetSubjectsAsync(SchoolId);
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

        if (SubjectId == Guid.Empty || Subjects.All(s => s.Id != SubjectId))
        {
            SubjectId = Subjects.Count > 0 ? Subjects[0].Id : Guid.Empty;
        }

        if (ClassId != Guid.Empty)
        {
            ClassAccessAuthorized = fullScope?.CanAccessClass(ClassId) ?? false;
            if (ClassAccessAuthorized)
            {
                HomeworkItems = await homeworkService.ListForClassAsync(ClassId);
            }
        }

        // SelectedAssignmentId is deliberately never auto-selected - manual drill-down only, matching
        // InterventionCasesModel's SelectedCaseId. Also gated on ClassAccessAuthorized, since
        // submissions are scoped to the (possibly access-denied) currently-selected class.
        if (ClassAccessAuthorized && SelectedAssignmentId is { } homeworkId)
        {
            await LoadSubmissionsAsync(homeworkId);
        }
    }

    private async Task LoadSubmissionsAsync(Guid homeworkId)
    {
        var roster = await enrollmentService.GetActiveRosterForClassAsync(ClassId, DateOnly.FromDateTime(DateTime.Today));
        var namesByStudentId = roster.ToDictionary(r => r.StudentPersonId, r => r.NameEn);

        var submissions = await submissionService.ListForHomeworkAsync(homeworkId);
        SubmissionRows = submissions
            .Select(s => new SubmissionRow(
                s.Id,
                namesByStudentId.TryGetValue(s.StudentPersonId, out var name) ? name : "(not in current roster)",
                s.Status.ToString(),
                s.SubmittedAtUtc,
                s.Score,
                s.FeedbackText))
            .ToList();
    }

    private RedirectToPageResult BackToScope() =>
        RedirectToPage(new { SchoolId, AcademicYearId, ClassId, SubjectId, SelectedAssignmentId });

    public async Task<IActionResult> OnPostCreateHomeworkAsync()
    {
        if (ClassId == Guid.Empty || SubjectId == Guid.Empty || CurrentPersonId is not { } assignedBy)
        {
            TempData["FlashMessage"] = "Choose a school, academic year, class and subject to continue.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        // Re-derived from a fresh scope check, never trusted from the posted ClassId alone - a
        // Razor Page POST handler is a directly-callable HTTP endpoint, so the class picker only
        // being filtered client-side would not be enough on its own to stop a tampered ClassId.
        // Same reasoning as AttendanceModel.OnPostSaveAttendanceAsync.
        var scope = await scopeQuery.GetScopeAsync(assignedBy, clock.TodayUtc, SchoolId, AcademicYearId);
        if (!scope.CanAccessClass(ClassId))
        {
            TempData["FlashMessage"] = "You do not have access to this class.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        if (string.IsNullOrWhiteSpace(NewHomework.TitleEn) || string.IsNullOrWhiteSpace(NewHomework.TitleDv)
            || string.IsNullOrWhiteSpace(NewHomework.InstructionsEn) || string.IsNullOrWhiteSpace(NewHomework.InstructionsDv))
        {
            TempData["FlashMessage"] = "Title and instructions are required in both languages.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        try
        {
            await homeworkService.CreateAsync(
                ClassId, SubjectId, null, NewHomework.TitleEn, NewHomework.TitleDv,
                NewHomework.InstructionsEn, NewHomework.InstructionsDv,
                NewHomework.AssignedDate, NewHomework.DueDate, NewHomework.MaxScore, assignedBy);

            TempData["FlashMessage"] = "Homework assigned.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToScope();
    }

    public async Task<IActionResult> OnPostGradeSubmissionAsync()
    {
        if (CurrentPersonId is not { } gradedBy)
        {
            TempData["FlashMessage"] = "Could not resolve the current user.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        // The posted SubmissionId names an EXISTING submission - resolved fresh here (never trusted
        // from whatever the page happened to have loaded) back to its own Homework's ClassId, and
        // that Class's own School+AcademicYear (Homework carries neither directly, and neither can
        // be assumed to be the page's currently-selected SchoolId/AcademicYearId, which are
        // independent inputs from this POST body's SubmissionId). Same "never trust a caller-posted
        // id without re-deriving its real scope" reasoning as every other handler on this page.
        var submission = await submissionService.GetAsync(GradeForm.SubmissionId);
        var homework = submission is null ? null : await homeworkService.GetAsync(submission.HomeworkId);
        if (homework is null
            || await orgLookup.GetClassSchoolIdAsync(homework.ClassId) is not { } schoolId
            || await orgLookup.GetClassAcademicYearIdAsync(homework.ClassId) is not { } academicYearId)
        {
            TempData["FlashMessage"] = "That submission no longer exists.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        var scope = await scopeQuery.GetScopeAsync(gradedBy, clock.TodayUtc, schoolId, academicYearId);
        if (!scope.CanAccessClass(homework.ClassId))
        {
            TempData["FlashMessage"] = "You do not have access to this class.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        try
        {
            await submissionService.GradeAsync(GradeForm.SubmissionId, GradeForm.Score, GradeForm.FeedbackText, gradedBy);
            TempData["FlashMessage"] = "Grade saved.";
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

using System.Security.Claims;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.OrgCurriculum.Application;
using HAMS.Platform.Common.Contracts;
using HAMS.TeachingTimetable.Application;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Staff;

/// <summary>
/// Razor Pages migration of the old Blazor <c>LessonPlanning.razor</c> (frontend migration plan —
/// MudBlazor's focus/click-reliability bugs). The trickiest page in the migration so far: on top of
/// the usual School -&gt; Academic Year / Grade / Subject scope cascade (siblings, all depending only
/// on School, exactly like <see cref="AttendanceModel"/>'s School/Year/Class row — one combined GET
/// form), there is a 3-level manual drill-down (Scheme of Work -&gt; Scheme Item -&gt; Teaching Topic)
/// that the original Blazor page navigated via in-memory button clicks. Each level is modeled as its
/// own nullable-Guid query-string property and an "Open" link (exactly <see cref="InterventionCasesModel"/>'s
/// <c>SelectedCaseId</c> pattern, just carried three levels deep instead of one) — opening a shallower
/// item's link omits every deeper property from the URL, which resets it by simple absence.
/// </summary>
[Authorize(Policy = StaffPolicy.Name)]
public sealed class LessonPlanningModel(
    IOrgStructureLookup orgLookup,
    ILessonPlanningService planningService,
    ISyllabusResolver syllabusResolver,
    IStaffAccessScopeQuery scopeQuery,
    IClock clock) : PageModel
{
    // ---- Scope cascade: School, then Academic Year / Grade / Subject (siblings - all three
    // depend only on School, not on each other) ----
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid GradeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SubjectId { get; set; }

    public IReadOnlyList<SchoolOption> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];
    public IReadOnlyList<GradeOption> Grades { get; private set; } = [];
    public IReadOnlyList<SubjectOption> Subjects { get; private set; } = [];
    public IReadOnlyList<ResourceType> ResourceTypes { get; private set; } = [];

    /// <summary>False when <see cref="GradeId"/> is set but isn't one of the caller's assigned
    /// grades (e.g. a stale link, or a directly-edited query string) - the scheme-of-work content
    /// is then not loaded at all, and the page shows an access-denied message instead of silently
    /// rendering nothing. Always true once <see cref="GradeId"/> is empty (nothing selected yet to
    /// deny). Same idea as AttendanceModel.ClassAccessAuthorized, just Grade- instead of Class-scoped.</summary>
    public bool GradeAccessAuthorized { get; private set; } = true;

    // ---- Drill-down: Scheme of Work -> Scheme Item -> Teaching Topic. Each is just navigation (a
    // GET), same idea as OrgStructure's "Manage Campuses"/"Manage Terms" links extended to 3 levels
    // instead of 1 - opening a shallower row's link naturally drops every deeper id from the URL. ----
    [BindProperty(SupportsGet = true)]
    public Guid? SelectedSchemeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedSchemeItemId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedTopicId { get; set; }

    public IReadOnlyList<SchemeOfWork> Schemes { get; private set; } = [];
    public IReadOnlyList<SchemeOfWorkItem> Items { get; private set; } = [];
    public IReadOnlyList<LearningOutcomeOption> LearningOutcomeOptions { get; private set; } = [];
    public IReadOnlyList<TeachingTopic> Topics { get; private set; } = [];
    public IReadOnlyList<LessonPlan> LessonPlans { get; private set; } = [];
    public IReadOnlyList<Resource> Resources { get; private set; } = [];

    [BindProperty] public NewSchemeInput NewScheme { get; set; } = new();
    [BindProperty] public NewItemInput NewItem { get; set; } = new();
    [BindProperty] public NewTopicInput NewTopic { get; set; } = new();
    [BindProperty] public NewLessonPlanInput NewPlan { get; set; } = new();
    [BindProperty] public NewResourceInput NewResource { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    // Replicates the old Blazor OnInitializedAsync/OnSchoolChangedAsync auto-select-first-option
    // behavior (same approach as AttendanceModel.LoadAsync): each level only auto-picks the first
    // option when nothing is selected yet, never overriding an explicit selection.
    private async Task LoadAllAsync()
    {
        TryGetCurrentPersonId(out var personId);

        // Resolved twice, deliberately: once against just the caller's accessible Schools (before
        // an Academic Year is even chosen - GetScopeAsync's own null-schoolId shortcut skips the
        // OrgCurriculum/SubjectTeachingAssignment joins entirely for this cheap first pass), then
        // again scoped to whichever School+Year end up selected, once Grades need filtering too.
        // Same approach as AttendanceModel.LoadAsync, just Grade- instead of Class-scoped.
        var schoolScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId: null, academicYearId: null);

        var allSchools = await orgLookup.GetSchoolsAsync();
        Schools = schoolScope.HasUnrestrictedAccess ? allSchools : [.. allSchools.Where(s => schoolScope.CanAccessSchool(s.Id))];
        if (SchoolId == Guid.Empty && Schools.Count > 0)
        {
            SchoolId = Schools[0].Id;
        }

        ResourceTypes = await planningService.GetResourceTypesAsync();
        if (string.IsNullOrEmpty(NewResource.ResourceTypeCode) && ResourceTypes.Count > 0)
        {
            NewResource.ResourceTypeCode = ResourceTypes[0].Code;
        }

        StaffAccessScope? fullScope = null;
        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);
            if (AcademicYearId == Guid.Empty && AcademicYears.Count > 0)
            {
                AcademicYearId = AcademicYears[0].Id;
            }

            var allGrades = await orgLookup.GetGradesAsync(SchoolId);
            if (AcademicYearId != Guid.Empty)
            {
                fullScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, SchoolId, AcademicYearId);
            }
            Grades = fullScope is null || fullScope.HasUnrestrictedAccess ? allGrades : [.. allGrades.Where(g => fullScope.CanAccessGrade(g.Id))];
            if (GradeId == Guid.Empty && Grades.Count > 0)
            {
                GradeId = Grades[0].Id;
            }

            Subjects = await orgLookup.GetSubjectsAsync(SchoolId);
            if (SubjectId == Guid.Empty && Subjects.Count > 0)
            {
                SubjectId = Subjects[0].Id;
            }
        }

        if (SubjectId != Guid.Empty && GradeId != Guid.Empty && AcademicYearId != Guid.Empty)
        {
            GradeAccessAuthorized = fullScope?.CanAccessGrade(GradeId) ?? false;
            if (GradeAccessAuthorized)
            {
                Schemes = await planningService.GetSchemesOfWorkAsync(SubjectId, GradeId, AcademicYearId);
            }
        }

        if (SelectedSchemeId is { } schemeId)
        {
            Items = await planningService.GetSchemeOfWorkItemsAsync(schemeId);

            // The picker for "which learning outcome does this item cover" needs the Subject's
            // current published syllabus for this Grade - if none exists yet (syllabus still Draft,
            // or none written at all), the picker is correctly empty rather than erroring, since
            // AddItem's whole premise (there's a published curriculum to plan against) doesn't hold.
            var syllabus = await syllabusResolver.ResolveAsync(SubjectId, GradeId);
            if (syllabus is not null)
            {
                LearningOutcomeOptions = await syllabusResolver.GetLearningOutcomeOptionsAsync(syllabus.Id);
            }
        }

        if (SelectedSchemeItemId is { } itemId)
        {
            Topics = await planningService.GetTeachingTopicsAsync(itemId);
        }

        if (SelectedTopicId is { } topicId)
        {
            LessonPlans = await planningService.GetLessonPlansAsync(topicId);
            Resources = await planningService.GetResourcesAsync(topicId);
        }
    }

    private bool TryGetCurrentPersonId(out Guid personId) =>
        Guid.TryParse(User.FindFirstValue(HamsClaimTypes.PersonId), out personId);

    private RedirectToPageResult BackToScope() =>
        RedirectToPage(new { SchoolId, AcademicYearId, GradeId, SubjectId, SelectedSchemeId, SelectedSchemeItemId, SelectedTopicId });

    public async Task<IActionResult> OnPostCreateSchemeAsync()
    {
        if (SubjectId == Guid.Empty || GradeId == Guid.Empty || AcademicYearId == Guid.Empty || string.IsNullOrWhiteSpace(NewScheme.Title))
        {
            TempData["FlashMessage"] = "Select a grade and academic year, and enter a title.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        if (!TryGetCurrentPersonId(out var personId))
        {
            TempData["FlashMessage"] = "Could not resolve your staff profile.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        // Re-derived from a fresh scope check, never trusted from the posted GradeId alone - a
        // Razor Page POST handler is a directly-callable HTTP endpoint, so the grade picker only
        // being filtered client-side would not be enough on its own to stop a tampered GradeId.
        // Same reasoning as AttendanceModel.OnPostSaveAttendanceAsync.
        var scope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, SchoolId, AcademicYearId);
        if (!scope.CanAccessGrade(GradeId))
        {
            TempData["FlashMessage"] = "You do not have access to this grade.";
            TempData["FlashSeverity"] = "danger";
            return BackToScope();
        }

        await planningService.CreateSchemeOfWorkAsync(SubjectId, GradeId, AcademicYearId, NewScheme.Title);
        TempData["FlashMessage"] = "Scheme of work created.";
        TempData["FlashSeverity"] = "success";
        return BackToScope();
    }

    public async Task<IActionResult> OnPostAddItemAsync()
    {
        if (SelectedSchemeId is not { } schemeId || NewItem.LearningOutcomeId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a learning outcome.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        if (!TryGetCurrentPersonId(out var personId) || !await IsAuthorizedForSchemeAsync(schemeId, personId))
        {
            return AccessDeniedToGrade();
        }

        await planningService.AddSchemeOfWorkItemAsync(schemeId, NewItem.LearningOutcomeId, NewItem.PlannedWeekNumber, NewItem.DisplayOrder);
        TempData["FlashMessage"] = "Item added.";
        TempData["FlashSeverity"] = "success";
        return BackToScope();
    }

    public async Task<IActionResult> OnPostCreateTopicAsync()
    {
        if (SelectedSchemeItemId is not { } itemId || string.IsNullOrWhiteSpace(NewTopic.NameEn) || string.IsNullOrWhiteSpace(NewTopic.NameDv))
        {
            TempData["FlashMessage"] = "Enter both the English and Dhivehi topic name.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        if (!TryGetCurrentPersonId(out var personId) || !await IsAuthorizedForSchemeItemAsync(itemId, personId))
        {
            return AccessDeniedToGrade();
        }

        await planningService.CreateTeachingTopicAsync(itemId, NewTopic.NameEn, NewTopic.NameDv, NewTopic.DisplayOrder);
        TempData["FlashMessage"] = "Teaching topic added.";
        TempData["FlashSeverity"] = "success";
        return BackToScope();
    }

    public async Task<IActionResult> OnPostCreateLessonPlanAsync()
    {
        if (SelectedTopicId is not { } topicId || !TryGetCurrentPersonId(out var staffId)
            || NewPlan.PlannedDate is not { } plannedDate || string.IsNullOrWhiteSpace(NewPlan.Objectives))
        {
            TempData["FlashMessage"] = "Set a planned date and enter objectives.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        if (!await IsAuthorizedForTopicAsync(topicId, staffId))
        {
            return AccessDeniedToGrade();
        }

        await planningService.CreateLessonPlanAsync(topicId, staffId, plannedDate, NewPlan.Objectives);
        TempData["FlashMessage"] = "Lesson plan added.";
        TempData["FlashSeverity"] = "success";
        return BackToScope();
    }

    public async Task<IActionResult> OnPostAddResourceAsync()
    {
        if (SelectedTopicId is not { } topicId || !TryGetCurrentPersonId(out var uploaderId)
            || string.IsNullOrWhiteSpace(NewResource.TitleEn) || string.IsNullOrWhiteSpace(NewResource.FileReference)
            || string.IsNullOrWhiteSpace(NewResource.ResourceTypeCode))
        {
            TempData["FlashMessage"] = "Enter a title, resource type and link or file path.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        if (!await IsAuthorizedForTopicAsync(topicId, uploaderId))
        {
            return AccessDeniedToGrade();
        }

        try
        {
            await planningService.AddResourceAsync(topicId, NewResource.TitleEn, NewResource.TitleDv, NewResource.ResourceTypeCode, NewResource.FileReference, uploaderId);
            TempData["FlashMessage"] = "Resource added.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToScope();
    }

    // ---- Re-authorization for the 3-level drill-down: none of Scheme Item/Teaching Topic/Lesson
    // Plan/Resource carry a Grade of their own - each is only reachable by walking back up to its
    // owning SchemeOfWork, which does. Every posted id here (SelectedSchemeId/SelectedSchemeItemId/
    // SelectedTopicId) is an independent input a caller could point at ANY scheme/item/topic
    // system-wide, so this walk is redone from scratch on every POST, never trusted from whatever
    // the page's own GET happened to have loaded for a different scheme.

    private async Task<bool> IsAuthorizedForSchemeAsync(Guid schemeOfWorkId, Guid personId)
    {
        var scheme = await planningService.GetSchemeOfWorkAsync(schemeOfWorkId);
        if (scheme is null || await orgLookup.GetGradeSchoolIdAsync(scheme.GradeId) is not { } schoolId)
        {
            return false;
        }

        var scope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId, scheme.AcademicYearId);
        return scope.CanAccessGrade(scheme.GradeId);
    }

    private async Task<bool> IsAuthorizedForSchemeItemAsync(Guid schemeOfWorkItemId, Guid personId)
    {
        var item = await planningService.GetSchemeOfWorkItemAsync(schemeOfWorkItemId);
        return item is not null && await IsAuthorizedForSchemeAsync(item.SchemeOfWorkId, personId);
    }

    private async Task<bool> IsAuthorizedForTopicAsync(Guid teachingTopicId, Guid personId)
    {
        var topic = await planningService.GetTeachingTopicAsync(teachingTopicId);
        return topic is not null && await IsAuthorizedForSchemeItemAsync(topic.SchemeOfWorkItemId, personId);
    }

    private IActionResult AccessDeniedToGrade()
    {
        TempData["FlashMessage"] = "You do not have access to this grade.";
        TempData["FlashSeverity"] = "danger";
        return BackToScope();
    }

    public sealed class NewSchemeInput
    {
        public string Title { get; set; } = "";
    }

    public sealed class NewItemInput
    {
        public Guid LearningOutcomeId { get; set; }
        public int PlannedWeekNumber { get; set; } = 1;
        public int DisplayOrder { get; set; } = 1;
    }

    public sealed class NewTopicInput
    {
        public string NameEn { get; set; } = "";
        public string NameDv { get; set; } = "";
        public int DisplayOrder { get; set; } = 1;
    }

    public sealed class NewLessonPlanInput
    {
        public DateOnly? PlannedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string Objectives { get; set; } = "";
    }

    public sealed class NewResourceInput
    {
        public string TitleEn { get; set; } = "";
        public string TitleDv { get; set; } = "";
        public string ResourceTypeCode { get; set; } = "";
        public string FileReference { get; set; } = "";
    }
}

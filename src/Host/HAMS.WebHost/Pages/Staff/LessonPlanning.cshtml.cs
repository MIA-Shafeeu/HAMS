using System.Security.Claims;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.OrgCurriculum.Application;
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
public sealed class LessonPlanningModel(IOrgStructureLookup orgLookup, ILessonPlanningService planningService) : PageModel
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
        Schools = await orgLookup.GetSchoolsAsync();
        if (SchoolId == Guid.Empty && Schools.Count > 0)
        {
            SchoolId = Schools[0].Id;
        }

        ResourceTypes = await planningService.GetResourceTypesAsync();
        if (string.IsNullOrEmpty(NewResource.ResourceTypeCode) && ResourceTypes.Count > 0)
        {
            NewResource.ResourceTypeCode = ResourceTypes[0].Code;
        }

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);
            if (AcademicYearId == Guid.Empty && AcademicYears.Count > 0)
            {
                AcademicYearId = AcademicYears[0].Id;
            }

            Grades = await orgLookup.GetGradesAsync(SchoolId);
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
            Schemes = await planningService.GetSchemesOfWorkAsync(SubjectId, GradeId, AcademicYearId);
        }

        if (SelectedSchemeId is { } schemeId)
        {
            Items = await planningService.GetSchemeOfWorkItemsAsync(schemeId);
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

        await planningService.CreateSchemeOfWorkAsync(SubjectId, GradeId, AcademicYearId, NewScheme.Title);
        TempData["FlashMessage"] = "Scheme of work created.";
        TempData["FlashSeverity"] = "success";
        return BackToScope();
    }

    public async Task<IActionResult> OnPostAddItemAsync()
    {
        if (SelectedSchemeId is not { } schemeId || !Guid.TryParse(NewItem.LearningOutcomeId, out var outcomeId))
        {
            TempData["FlashMessage"] = "Enter a valid learning outcome id.";
            TempData["FlashSeverity"] = "warning";
            return BackToScope();
        }

        await planningService.AddSchemeOfWorkItemAsync(schemeId, outcomeId, NewItem.PlannedWeekNumber, NewItem.DisplayOrder);
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

    public sealed class NewSchemeInput
    {
        public string Title { get; set; } = "";
    }

    public sealed class NewItemInput
    {
        public string LearningOutcomeId { get; set; } = "";
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

using System.ComponentModel.DataAnnotations;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class OrgStructureModel(IOrgAdminService orgAdmin) : PageModel
{
    // ---- Cross-tab data ----
    public IReadOnlyList<School> Schools { get; private set; } = [];

    // ---- Tab selection (which tab shows as active after a full-page reload) ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "schools";

    // ---- Schools tab ----
    [BindProperty(SupportsGet = true)]
    public Guid CampusesSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditCampusId { get; set; }

    public IReadOnlyList<Campus> Campuses { get; private set; } = [];

    // ---- Academic Years & Terms tab ----
    [BindProperty(SupportsGet = true)]
    public Guid YearsSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid TermsYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditTermId { get; set; }

    public IReadOnlyList<AcademicYear> YearsForYearsTab { get; private set; } = [];
    public IReadOnlyList<Term> Terms { get; private set; } = [];

    // ---- Phases & Key Stages tab ----
    [BindProperty(SupportsGet = true)]
    public Guid PhasesSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SelectedPhaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditPhaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditKeyStageId { get; set; }

    public IReadOnlyList<Phase> Phases { get; private set; } = [];
    public IReadOnlyList<KeyStage> KeyStages { get; private set; } = [];

    // ---- Key Stage Policies tab ----
    [BindProperty(SupportsGet = true)]
    public Guid PoliciesSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid PolicyKeyStageId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid PolicyYearId { get; set; }

    public IReadOnlyList<AcademicYear> YearsForPoliciesTab { get; private set; } = [];
    public IReadOnlyList<KeyStage> AllKeyStagesForSchool { get; private set; } = [];
    public IReadOnlyList<KeyStagePolicy> KeyStagePolicies { get; private set; } = [];

    // ---- Grades tab ----
    [BindProperty(SupportsGet = true)]
    public Guid GradesSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditGradeId { get; set; }

    public IReadOnlyList<Grade> Grades { get; private set; } = [];

    // ---- Classes tab ----
    [BindProperty(SupportsGet = true)]
    public Guid ClassesSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid ClassesYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditClassId { get; set; }

    public IReadOnlyList<AcademicYear> YearsForClassesTab { get; private set; } = [];
    public IReadOnlyList<Grade> GradesForClassesTab { get; private set; } = [];
    public IReadOnlyList<Class> Classes { get; private set; } = [];
    public IReadOnlyList<Guid> EditClassGradeIds { get; private set; } = [];

    // ---- Form inputs (POST bodies) ----
    [BindProperty] public NewSchoolInput NewSchool { get; set; } = new();
    [BindProperty] public EditSchoolInput EditSchoolForm { get; set; } = new();
    [BindProperty] public NewCampusInput NewCampus { get; set; } = new();
    [BindProperty] public EditCampusInput EditCampusForm { get; set; } = new();
    [BindProperty] public NewYearInput NewYear { get; set; } = new();
    [BindProperty] public EditYearInput EditYearForm { get; set; } = new();
    [BindProperty] public NewTermInput NewTerm { get; set; } = new();
    [BindProperty] public EditTermInput EditTermForm { get; set; } = new();
    [BindProperty] public NewPhaseInput NewPhase { get; set; } = new();
    [BindProperty] public EditPhaseInput EditPhaseForm { get; set; } = new();
    [BindProperty] public NewKeyStageInput NewKeyStage { get; set; } = new();
    [BindProperty] public EditKeyStageInput EditKeyStageForm { get; set; } = new();
    [BindProperty] public NewPolicyInput NewPolicy { get; set; } = new();
    [BindProperty] public NewGradeInput NewGrade { get; set; } = new();
    [BindProperty] public EditGradeInput EditGradeForm { get; set; } = new();
    [BindProperty] public NewClassInput NewClass { get; set; } = new();
    [BindProperty] public EditClassInput EditClassForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    // Every tab's data is loaded unconditionally on every request (not just the active tab) since
    // Bootstrap's tabs are just CSS show/hide - all tab content lives in one server-rendered
    // response, unlike MudTabs' lazy per-panel rendering.
    private async Task LoadAllAsync()
    {
        Schools = await orgAdmin.GetSchoolsAsync();

        if (CampusesSchoolId != Guid.Empty)
        {
            Campuses = await orgAdmin.GetCampusesAsync(CampusesSchoolId);
        }

        if (YearsSchoolId != Guid.Empty)
        {
            YearsForYearsTab = await orgAdmin.GetAcademicYearsAsync(YearsSchoolId);
        }
        if (TermsYearId != Guid.Empty)
        {
            Terms = await orgAdmin.GetTermsAsync(TermsYearId);
        }

        if (PhasesSchoolId != Guid.Empty)
        {
            Phases = await orgAdmin.GetPhasesAsync(PhasesSchoolId);
        }
        if (SelectedPhaseId != Guid.Empty)
        {
            KeyStages = (await orgAdmin.GetKeyStagesAsync(PhasesSchoolId)).Where(k => k.PhaseId == SelectedPhaseId).ToList();
        }

        if (PoliciesSchoolId != Guid.Empty)
        {
            YearsForPoliciesTab = await orgAdmin.GetAcademicYearsAsync(PoliciesSchoolId);
            AllKeyStagesForSchool = await orgAdmin.GetKeyStagesAsync(PoliciesSchoolId);
        }
        if (PolicyKeyStageId != Guid.Empty)
        {
            KeyStagePolicies = await orgAdmin.GetKeyStagePoliciesAsync(PolicyKeyStageId);
        }

        if (GradesSchoolId != Guid.Empty)
        {
            Grades = await orgAdmin.GetGradesAsync(GradesSchoolId);
        }

        if (ClassesSchoolId != Guid.Empty)
        {
            YearsForClassesTab = await orgAdmin.GetAcademicYearsAsync(ClassesSchoolId);
            GradesForClassesTab = await orgAdmin.GetGradesAsync(ClassesSchoolId);
        }
        if (ClassesYearId != Guid.Empty)
        {
            Classes = await orgAdmin.GetClassesAsync(ClassesYearId);
        }
        if (EditClassId is { } editClassId)
        {
            EditClassGradeIds = await orgAdmin.GetClassGradeIdsAsync(editClassId);
        }
    }

    private RedirectToPageResult BackToTab(string tab, object? extraRouteValues = null)
    {
        var routeValues = new RouteValueDictionary(extraRouteValues) { ["tab"] = tab };
        return RedirectToPage(routeValues);
    }

    // ---- Schools ----

    public async Task<IActionResult> OnPostCreateSchoolAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSchool.Code) || string.IsNullOrWhiteSpace(NewSchool.Name))
        {
            TempData["FlashMessage"] = "Code and name are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("schools");
        }

        await orgAdmin.CreateSchoolAsync(NewSchool.Code, NewSchool.Name);
        TempData["FlashMessage"] = "School created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("schools");
    }

    public async Task<IActionResult> OnPostSaveSchoolEditAsync()
    {
        await orgAdmin.UpdateSchoolAsync(EditSchoolForm.Id, EditSchoolForm.Name);
        TempData["FlashMessage"] = "School updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("schools", new { CampusesSchoolId });
    }

    public async Task<IActionResult> OnPostCreateCampusAsync()
    {
        if (CampusesSchoolId == Guid.Empty || string.IsNullOrWhiteSpace(NewCampus.Code) || string.IsNullOrWhiteSpace(NewCampus.Name))
        {
            TempData["FlashMessage"] = "Select a school, and provide a code and name.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("schools", new { CampusesSchoolId });
        }

        await orgAdmin.CreateCampusAsync(CampusesSchoolId, NewCampus.Code, NewCampus.Name);
        TempData["FlashMessage"] = "Campus created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("schools", new { CampusesSchoolId });
    }

    public async Task<IActionResult> OnPostSaveCampusEditAsync()
    {
        await orgAdmin.UpdateCampusAsync(EditCampusForm.Id, EditCampusForm.Name);
        TempData["FlashMessage"] = "Campus updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("schools", new { CampusesSchoolId });
    }

    // ---- Academic Years & Terms ----

    public async Task<IActionResult> OnPostCreateAcademicYearAsync()
    {
        if (YearsSchoolId == Guid.Empty || string.IsNullOrWhiteSpace(NewYear.Code))
        {
            TempData["FlashMessage"] = "Select a school, and provide a code, start date and end date.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("years", new { YearsSchoolId });
        }

        await orgAdmin.CreateAcademicYearAsync(YearsSchoolId, NewYear.Code, NewYear.Name, NewYear.StartDate, NewYear.EndDate);
        TempData["FlashMessage"] = "Academic year created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("years", new { YearsSchoolId });
    }

    public async Task<IActionResult> OnPostSaveAcademicYearEditAsync()
    {
        await orgAdmin.UpdateAcademicYearAsync(EditYearForm.Id, EditYearForm.Name, EditYearForm.StartDate, EditYearForm.EndDate);
        TempData["FlashMessage"] = "Academic year updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("years", new { YearsSchoolId, TermsYearId });
    }

    public async Task<IActionResult> OnPostCreateTermAsync()
    {
        if (TermsYearId == Guid.Empty || string.IsNullOrWhiteSpace(NewTerm.Code))
        {
            TempData["FlashMessage"] = "Select an academic year, and provide a code, start date and end date.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("years", new { YearsSchoolId, TermsYearId });
        }

        await orgAdmin.CreateTermAsync(TermsYearId, NewTerm.Code, NewTerm.Name, NewTerm.StartDate, NewTerm.EndDate, NewTerm.DisplayOrder);
        TempData["FlashMessage"] = "Term created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("years", new { YearsSchoolId, TermsYearId });
    }

    public async Task<IActionResult> OnPostSaveTermEditAsync()
    {
        await orgAdmin.UpdateTermAsync(EditTermForm.Id, EditTermForm.Name, EditTermForm.StartDate, EditTermForm.EndDate, EditTermForm.DisplayOrder);
        TempData["FlashMessage"] = "Term updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("years", new { YearsSchoolId, TermsYearId });
    }

    // ---- Phases & Key Stages ----

    public async Task<IActionResult> OnPostCreatePhaseAsync()
    {
        if (PhasesSchoolId == Guid.Empty || string.IsNullOrWhiteSpace(NewPhase.Code))
        {
            TempData["FlashMessage"] = "Select a school and provide a code.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("phases", new { PhasesSchoolId });
        }

        await orgAdmin.CreatePhaseAsync(PhasesSchoolId, NewPhase.Code, NewPhase.Name, NewPhase.DisplayOrder);
        TempData["FlashMessage"] = "Phase created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("phases", new { PhasesSchoolId });
    }

    public async Task<IActionResult> OnPostSavePhaseEditAsync()
    {
        await orgAdmin.UpdatePhaseAsync(EditPhaseForm.Id, EditPhaseForm.Name, EditPhaseForm.DisplayOrder);
        TempData["FlashMessage"] = "Phase updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("phases", new { PhasesSchoolId, SelectedPhaseId });
    }

    public async Task<IActionResult> OnPostCreateKeyStageAsync()
    {
        if (PhasesSchoolId == Guid.Empty || SelectedPhaseId == Guid.Empty || string.IsNullOrWhiteSpace(NewKeyStage.Code))
        {
            TempData["FlashMessage"] = "Select a phase and provide a code.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("phases", new { PhasesSchoolId, SelectedPhaseId });
        }

        await orgAdmin.CreateKeyStageAsync(PhasesSchoolId, SelectedPhaseId, NewKeyStage.Code, NewKeyStage.Name, NewKeyStage.DisplayOrder);
        TempData["FlashMessage"] = "Key stage created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("phases", new { PhasesSchoolId, SelectedPhaseId });
    }

    public async Task<IActionResult> OnPostSaveKeyStageEditAsync()
    {
        await orgAdmin.UpdateKeyStageAsync(EditKeyStageForm.Id, EditKeyStageForm.Name, EditKeyStageForm.DisplayOrder);
        TempData["FlashMessage"] = "Key stage updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("phases", new { PhasesSchoolId, SelectedPhaseId });
    }

    // ---- Key Stage Policies ----

    public async Task<IActionResult> OnPostCreateKeyStagePolicyAsync()
    {
        if (PolicyKeyStageId == Guid.Empty || PolicyYearId == Guid.Empty)
        {
            return BackToTab("policies", new { PoliciesSchoolId, PolicyKeyStageId, PolicyYearId });
        }

        try
        {
            await orgAdmin.CreateKeyStagePolicyAsync(PolicyKeyStageId, PolicyYearId, NewPolicy.EvaluationModelCode, null, null, null, null);
            TempData["FlashMessage"] = "Draft key stage policy created.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("policies", new { PoliciesSchoolId, PolicyKeyStageId, PolicyYearId });
    }

    public async Task<IActionResult> OnPostPublishKeyStagePolicyAsync(Guid policyId)
    {
        try
        {
            await orgAdmin.PublishKeyStagePolicyAsync(policyId);
            TempData["FlashMessage"] = "Policy published.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("policies", new { PoliciesSchoolId, PolicyKeyStageId, PolicyYearId });
    }

    // ---- Grades ----

    public async Task<IActionResult> OnPostCreateGradeAsync()
    {
        if (GradesSchoolId == Guid.Empty || string.IsNullOrWhiteSpace(NewGrade.Code))
        {
            TempData["FlashMessage"] = "Select a school and provide a code.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("grades", new { GradesSchoolId });
        }

        await orgAdmin.CreateGradeAsync(GradesSchoolId, NewGrade.Code, NewGrade.Name, NewGrade.DisplayOrder);
        TempData["FlashMessage"] = "Grade created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("grades", new { GradesSchoolId });
    }

    public async Task<IActionResult> OnPostSaveGradeEditAsync()
    {
        await orgAdmin.UpdateGradeAsync(EditGradeForm.Id, EditGradeForm.Name, EditGradeForm.DisplayOrder);
        TempData["FlashMessage"] = "Grade updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("grades", new { GradesSchoolId });
    }

    public async Task<IActionResult> OnPostSetNextGradeAsync(Guid gradeId, Guid nextGradeId)
    {
        try
        {
            await orgAdmin.SetNextGradeAsync(gradeId, nextGradeId == Guid.Empty ? null : nextGradeId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("grades", new { GradesSchoolId });
    }

    // ---- Classes ----

    public async Task<IActionResult> OnPostCreateClassAsync()
    {
        var gradeIds = NewClass.GradeIds ?? [];
        if (ClassesSchoolId == Guid.Empty || ClassesYearId == Guid.Empty || string.IsNullOrWhiteSpace(NewClass.Code) || gradeIds.Count == 0)
        {
            TempData["FlashMessage"] = "Code, name, and at least one grade are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("classes", new { ClassesSchoolId, ClassesYearId });
        }

        await orgAdmin.CreateClassAsync(ClassesSchoolId, null, ClassesYearId, NewClass.Code, NewClass.Name, gradeIds);
        TempData["FlashMessage"] = "Class created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("classes", new { ClassesSchoolId, ClassesYearId });
    }

    public async Task<IActionResult> OnPostSaveClassEditAsync()
    {
        var gradeIds = EditClassForm.GradeIds ?? [];
        if (gradeIds.Count == 0)
        {
            TempData["FlashMessage"] = "A class must have at least one grade.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("classes", new { ClassesSchoolId, ClassesYearId, EditClassId = EditClassForm.Id });
        }

        await orgAdmin.UpdateClassAsync(EditClassForm.Id, EditClassForm.Name, gradeIds);
        TempData["FlashMessage"] = "Class updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("classes", new { ClassesSchoolId, ClassesYearId });
    }

    // ---- Input models ----

    public sealed class NewSchoolInput
    {
        [Required] public string Code { get; set; } = "";
        [Required] public string Name { get; set; } = "";
    }

    public sealed class EditSchoolInput
    {
        public Guid Id { get; set; }
        [Required] public string Name { get; set; } = "";
    }

    public sealed class NewCampusInput
    {
        [Required] public string Code { get; set; } = "";
        [Required] public string Name { get; set; } = "";
    }

    public sealed class EditCampusInput
    {
        public Guid Id { get; set; }
        [Required] public string Name { get; set; } = "";
    }

    public sealed class NewYearInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(1));
    }

    public sealed class EditYearInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }

    public sealed class NewTermInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
        public int DisplayOrder { get; set; } = 1;
    }

    public sealed class EditTermInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class NewPhaseInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; } = 1;
    }

    public sealed class EditPhaseInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
    }

    public sealed class NewKeyStageInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; } = 1;
    }

    public sealed class EditKeyStageInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
    }

    public sealed class NewPolicyInput
    {
        public string EvaluationModelCode { get; set; } = "MASTERY";
    }

    public sealed class NewGradeInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; } = 1;
    }

    public sealed class EditGradeInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
    }

    public sealed class NewClassInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public List<Guid>? GradeIds { get; set; }
    }

    public sealed class EditClassInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public List<Guid>? GradeIds { get; set; }
    }
}

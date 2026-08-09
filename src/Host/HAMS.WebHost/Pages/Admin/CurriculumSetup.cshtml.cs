using System.ComponentModel.DataAnnotations;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class CurriculumSetupModel(
    IOrgAdminService orgAdmin,
    ICurriculumAdminService curriculumAdmin,
    ISyllabusPublishingService syllabusPublishing,
    ICurriculumCsvImportService csvImport) : PageModel
{
    private const long MaxCsvFileSizeBytes = 5 * 1024 * 1024;

    // ---- Tab selection ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "frameworks";

    // ---- Cross-tab pick lists (loaded unconditionally on every request - all 4 tab panes render
    // in one response, same reasoning as OrgStructureModel.LoadAllAsync) ----
    public IReadOnlyList<CurriculumFramework> Frameworks { get; private set; } = [];
    public IReadOnlyList<LearningArea> LearningAreas { get; private set; } = [];
    public IReadOnlyList<DeliveryMode> DeliveryModes { get; private set; } = [];
    public IReadOnlyList<MediumOfInstruction> Mediums { get; private set; } = [];
    public IReadOnlyList<School> Schools { get; private set; } = [];

    // ---- Curriculum Frameworks tab ----
    [BindProperty(SupportsGet = true)]
    public Guid? EditFrameworkId { get; set; }

    // ---- Learning Areas tab ----
    [BindProperty(SupportsGet = true)]
    public Guid? EditLearningAreaId { get; set; }

    // ---- Subjects tab (its own School-selection property - never shared with the Syllabus tab's
    // SyllabusSchoolId below, even though both filter the same Subject list by school. Sharing a
    // single "_selectedSchoolId" is exactly the bug the old Blazor page's MudTabs got away with
    // (only one tab's markup was ever mounted at a time) and that Bootstrap's always-rendered tab
    // panes cannot: a selection made on one tab would silently bleed into the other tab's identical
    // school picker in the same response.) ----
    [BindProperty(SupportsGet = true)]
    public Guid SubjectsSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditSubjectId { get; set; }

    public IReadOnlyList<Subject> Subjects { get; private set; } = [];

    // ---- Syllabus tab ----
    [BindProperty(SupportsGet = true)]
    public Guid SyllabusSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SyllabusSubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SyllabusSelectedId { get; set; }

    public IReadOnlyList<Subject> SyllabusSubjects { get; private set; } = [];
    public IReadOnlyList<Grade> SyllabusGrades { get; private set; } = [];
    public IReadOnlyList<Syllabus> Syllabuses { get; private set; } = [];
    public IReadOnlyList<SyllabusGradeApplicability> GradeApplicabilities { get; private set; } = [];

    // ---- Form inputs (POST bodies) ----
    [BindProperty] public NewFrameworkInput NewFramework { get; set; } = new();
    [BindProperty] public EditFrameworkInput EditFrameworkForm { get; set; } = new();
    [BindProperty] public NewLearningAreaInput NewLearningArea { get; set; } = new();
    [BindProperty] public EditLearningAreaInput EditLearningAreaForm { get; set; } = new();
    [BindProperty] public NewSubjectInput NewSubject { get; set; } = new();
    [BindProperty] public EditSubjectInput EditSubjectForm { get; set; } = new();
    [BindProperty] public Guid NewApplicabilityGradeId { get; set; }
    [BindProperty] public IFormFile? CsvFile { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        Frameworks = await curriculumAdmin.GetCurriculumFrameworksAsync();
        LearningAreas = await curriculumAdmin.GetLearningAreasAsync();
        DeliveryModes = await curriculumAdmin.GetDeliveryModesAsync();
        Mediums = await curriculumAdmin.GetMediumsOfInstructionAsync();
        Schools = await orgAdmin.GetSchoolsAsync();

        if (SubjectsSchoolId != Guid.Empty)
        {
            Subjects = await curriculumAdmin.GetSubjectsAsync(SubjectsSchoolId);
        }

        if (SyllabusSchoolId != Guid.Empty)
        {
            SyllabusSubjects = await curriculumAdmin.GetSubjectsAsync(SyllabusSchoolId);
            SyllabusGrades = await orgAdmin.GetGradesAsync(SyllabusSchoolId);
        }
        if (SyllabusSubjectId != Guid.Empty)
        {
            Syllabuses = await curriculumAdmin.GetSyllabusesForSubjectAsync(SyllabusSubjectId);
        }
        if (SyllabusSelectedId != Guid.Empty)
        {
            GradeApplicabilities = await curriculumAdmin.GetSyllabusGradeApplicabilitiesAsync(SyllabusSelectedId);
        }
    }

    private RedirectToPageResult BackToTab(string tab, object? extraRouteValues = null)
    {
        var routeValues = new RouteValueDictionary(extraRouteValues) { ["tab"] = tab };
        return RedirectToPage(routeValues);
    }

    private RedirectToPageResult BackToSyllabus(Guid? selectedId = null) =>
        RedirectToPage(new
        {
            tab = "syllabus",
            SyllabusSchoolId,
            SyllabusSubjectId,
            SyllabusSelectedId = selectedId ?? SyllabusSelectedId,
        });

    // ---- Curriculum Frameworks ----

    public async Task<IActionResult> OnPostCreateFrameworkAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFramework.Code) || string.IsNullOrWhiteSpace(NewFramework.Name))
        {
            TempData["FlashMessage"] = "Code and name are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("frameworks");
        }

        await curriculumAdmin.CreateCurriculumFrameworkAsync(
            NewFramework.Code, NewFramework.Name, string.IsNullOrWhiteSpace(NewFramework.Description) ? null : NewFramework.Description);
        TempData["FlashMessage"] = "Curriculum framework created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("frameworks");
    }

    public async Task<IActionResult> OnPostSaveFrameworkEditAsync()
    {
        await curriculumAdmin.UpdateCurriculumFrameworkAsync(
            EditFrameworkForm.Id, EditFrameworkForm.Name, string.IsNullOrWhiteSpace(EditFrameworkForm.Description) ? null : EditFrameworkForm.Description);
        TempData["FlashMessage"] = "Curriculum framework updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("frameworks");
    }

    // ---- Learning Areas ----

    public async Task<IActionResult> OnPostCreateLearningAreaAsync()
    {
        if (NewLearningArea.CurriculumFrameworkId == Guid.Empty || string.IsNullOrWhiteSpace(NewLearningArea.Code) || string.IsNullOrWhiteSpace(NewLearningArea.Name))
        {
            TempData["FlashMessage"] = "Select a framework, and provide a code and name.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("learning-areas");
        }

        await curriculumAdmin.CreateLearningAreaAsync(NewLearningArea.CurriculumFrameworkId, NewLearningArea.Code, NewLearningArea.Name, NewLearningArea.DisplayOrder);
        TempData["FlashMessage"] = "Learning area created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("learning-areas");
    }

    public async Task<IActionResult> OnPostSaveLearningAreaEditAsync()
    {
        await curriculumAdmin.UpdateLearningAreaAsync(EditLearningAreaForm.Id, EditLearningAreaForm.Name, EditLearningAreaForm.DisplayOrder);
        TempData["FlashMessage"] = "Learning area updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("learning-areas");
    }

    // ---- Subjects ----

    public async Task<IActionResult> OnPostCreateSubjectAsync()
    {
        if (SubjectsSchoolId == Guid.Empty || NewSubject.LearningAreaId == Guid.Empty || NewSubject.DeliveryModeId == Guid.Empty || NewSubject.MediumId == Guid.Empty ||
            string.IsNullOrWhiteSpace(NewSubject.Code) || string.IsNullOrWhiteSpace(NewSubject.Name))
        {
            TempData["FlashMessage"] = "Select a school, learning area, delivery mode and medium, and provide a code and name.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("subjects", new { SubjectsSchoolId });
        }

        // DeliveryModes/Mediums (the [BindProperty(SupportsGet=true)]-less cross-tab lists) are only
        // ever populated by LoadAllAsync on a GET - a POST handler runs on its own, fresh request, so
        // the codes CreateSubjectAsync needs have to be resolved here directly.
        var deliveryMode = (await curriculumAdmin.GetDeliveryModesAsync()).SingleOrDefault(m => m.Id == NewSubject.DeliveryModeId);
        var medium = (await curriculumAdmin.GetMediumsOfInstructionAsync()).SingleOrDefault(m => m.Id == NewSubject.MediumId);
        if (deliveryMode is null || medium is null)
        {
            TempData["FlashMessage"] = "Select a delivery mode and medium.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("subjects", new { SubjectsSchoolId });
        }

        try
        {
            await curriculumAdmin.CreateSubjectAsync(SubjectsSchoolId, NewSubject.LearningAreaId, NewSubject.Code, NewSubject.Name, deliveryMode.Code, medium.Code, 0);
            TempData["FlashMessage"] = "Subject created.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("subjects", new { SubjectsSchoolId });
    }

    public async Task<IActionResult> OnPostSaveSubjectEditAsync()
    {
        var deliveryMode = (await curriculumAdmin.GetDeliveryModesAsync()).SingleOrDefault(m => m.Id == EditSubjectForm.DeliveryModeId);
        var medium = (await curriculumAdmin.GetMediumsOfInstructionAsync()).SingleOrDefault(m => m.Id == EditSubjectForm.MediumId);
        if (deliveryMode is null || medium is null)
        {
            TempData["FlashMessage"] = "Select a delivery mode and medium.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("subjects", new { SubjectsSchoolId });
        }

        try
        {
            await curriculumAdmin.UpdateSubjectAsync(EditSubjectForm.Id, EditSubjectForm.Name, deliveryMode.Code, medium.Code, EditSubjectForm.DisplayOrder);
            TempData["FlashMessage"] = "Subject updated.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("subjects", new { SubjectsSchoolId });
    }

    // ---- Syllabus ----

    public async Task<IActionResult> OnPostCreateInitialDraftAsync()
    {
        if (SyllabusSubjectId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a subject first.";
            TempData["FlashSeverity"] = "warning";
            return BackToSyllabus();
        }

        var id = await syllabusPublishing.CreateInitialDraftAsync(SyllabusSubjectId);
        TempData["FlashMessage"] = "Initial draft syllabus created.";
        TempData["FlashSeverity"] = "success";
        return BackToSyllabus(id);
    }

    public async Task<IActionResult> OnPostCreateDraftRevisionAsync(Guid existingSyllabusId)
    {
        var id = await syllabusPublishing.CreateDraftRevisionAsync(existingSyllabusId);
        TempData["FlashMessage"] = "Draft revision created.";
        TempData["FlashSeverity"] = "success";
        return BackToSyllabus(id);
    }

    public async Task<IActionResult> OnPostPublishSyllabusAsync(Guid syllabusId)
    {
        try
        {
            await syllabusPublishing.PublishAsync(syllabusId);
            TempData["FlashMessage"] = "Syllabus published.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToSyllabus();
    }

    public async Task<IActionResult> OnPostAddGradeApplicabilityAsync()
    {
        if (SyllabusSelectedId == Guid.Empty || NewApplicabilityGradeId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a syllabus and a grade first.";
            TempData["FlashSeverity"] = "warning";
            return BackToSyllabus();
        }

        await curriculumAdmin.AddSyllabusGradeApplicabilityAsync(SyllabusSelectedId, NewApplicabilityGradeId);
        TempData["FlashMessage"] = "Grade applicability added.";
        TempData["FlashSeverity"] = "success";
        return BackToSyllabus();
    }

    public async Task<IActionResult> OnPostImportCsvAsync()
    {
        if (SyllabusSelectedId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a syllabus first.";
            TempData["FlashSeverity"] = "warning";
            return BackToSyllabus();
        }

        if (CsvFile is null || CsvFile.Length == 0)
        {
            TempData["FlashMessage"] = "Choose a CSV file first.";
            TempData["FlashSeverity"] = "warning";
            return BackToSyllabus();
        }

        if (CsvFile.Length > MaxCsvFileSizeBytes)
        {
            TempData["FlashMessage"] = "CSV file is too large (max 5 MB).";
            TempData["FlashSeverity"] = "warning";
            return BackToSyllabus();
        }

        try
        {
            await using var stream = CsvFile.OpenReadStream();
            var result = await csvImport.ImportAsync(SyllabusSelectedId, stream);
            TempData["FlashMessage"] =
                $"Imported {result.StrandsCreated} strands, {result.SubStrandsCreated} sub-strands, {result.OutcomesCreated} outcomes, {result.IndicatorsCreated} indicators.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToSyllabus();
    }

    // ---- Input models ----

    public sealed class NewFrameworkInput
    {
        [Required] public string Code { get; set; } = "";
        [Required] public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public sealed class EditFrameworkInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public sealed class NewLearningAreaInput
    {
        public Guid CurriculumFrameworkId { get; set; }
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
    }

    public sealed class EditLearningAreaInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int DisplayOrder { get; set; }
    }

    public sealed class NewSubjectInput
    {
        public Guid LearningAreaId { get; set; }
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid DeliveryModeId { get; set; }
        public Guid MediumId { get; set; }
    }

    public sealed class EditSubjectInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public Guid DeliveryModeId { get; set; }
        public Guid MediumId { get; set; }
        public int DisplayOrder { get; set; }
    }
}

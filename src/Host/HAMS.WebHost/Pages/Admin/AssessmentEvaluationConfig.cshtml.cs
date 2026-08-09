using System.ComponentModel.DataAnnotations;
using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class AssessmentEvaluationConfigModel(
    IAssessmentConfigAdminService assessmentConfig,
    IOrgAdminService orgAdmin,
    ICurriculumAdminService curriculumAdmin) : PageModel
{
    // ---- Cross-tab lookup data ----
    public IReadOnlyList<AssessmentCategory> Categories { get; private set; } = [];
    public IReadOnlyList<ResultAggregationRule> AggregationRules { get; private set; } = [];
    public IReadOnlyList<ExternalExaminationBoard> ExamBoards { get; private set; } = [];
    public IReadOnlyList<School> Schools { get; private set; } = [];

    // ---- Tab selection (which tab shows as active after a full-page reload) ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "schemes";

    // ---- Assessment Schemes tab ----
    [BindProperty(SupportsGet = true)]
    public Guid SelectedSchemeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditSchemeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditComponentId { get; set; }

    public IReadOnlyList<AssessmentScheme> Schemes { get; private set; } = [];
    public IReadOnlyList<AssessmentSchemeComponent> SchemeComponents { get; private set; } = [];

    // ---- Grade Scales tab ----
    [BindProperty(SupportsGet = true)]
    public Guid SelectedGradeScaleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditScaleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditBandId { get; set; }

    public IReadOnlyList<GradeScale> GradeScales { get; private set; } = [];
    public IReadOnlyList<GradeBand> GradeBands { get; private set; } = [];

    // ---- School stays ONE shared field across the Evaluation Periods and Assessments tabs -
    // both tabs' cascades hang off the same OrgCurriculum School, and a single top-level
    // school <select> (its own <form>, with no downstream hidden fields) naturally resets
    // BOTH tabs' downstream selections when it changes, since none of those fields ride along
    // in that form's submission. Academic Year deliberately does NOT follow suit - Evaluation
    // Periods and Assessments load different dependent data from their academic year (periods
    // directly; assessments via a further Term cascade), so sharing one field would make
    // whichever tab is visited second look like it already had a year picked while its own
    // dependent list was never loaded. Each tab therefore gets its own AcademicYearId* property.
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    public IReadOnlyList<AcademicYear> AcademicYears { get; private set; } = [];
    public IReadOnlyList<Grade> Grades { get; private set; } = [];
    public IReadOnlyList<Subject> Subjects { get; private set; } = [];

    // ---- Evaluation Periods tab ----
    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearIdForPeriods { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditPeriodId { get; set; }

    public IReadOnlyList<EvaluationPeriod> EvaluationPeriods { get; private set; } = [];

    // ---- Promotion Policies tab ----
    [BindProperty(SupportsGet = true)]
    public Guid? EditPolicyId { get; set; }

    public IReadOnlyList<PromotionPolicy> PromotionPolicies { get; private set; } = [];

    // ---- Assessments tab ----
    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearIdForAssessments { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid TermId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid GradeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditAssessmentId { get; set; }

    public IReadOnlyList<Term> Terms { get; private set; } = [];
    public IReadOnlyList<Assessment> Assessments { get; private set; } = [];

    // ---- Form inputs (POST bodies) ----
    [BindProperty] public NewSchemeInput NewScheme { get; set; } = new();
    [BindProperty] public EditSchemeInput EditSchemeForm { get; set; } = new();
    [BindProperty] public NewComponentInput NewComponent { get; set; } = new();
    [BindProperty] public EditComponentInput EditComponentForm { get; set; } = new();
    [BindProperty] public NewScaleInput NewScale { get; set; } = new();
    [BindProperty] public EditScaleInput EditScaleForm { get; set; } = new();
    [BindProperty] public NewBandInput NewBand { get; set; } = new();
    [BindProperty] public EditBandInput EditBandForm { get; set; } = new();
    [BindProperty] public NewPeriodInput NewPeriod { get; set; } = new();
    [BindProperty] public EditPeriodInput EditPeriodForm { get; set; } = new();
    [BindProperty] public NewPolicyInput NewPolicy { get; set; } = new();
    [BindProperty] public EditPolicyInput EditPolicyForm { get; set; } = new();
    [BindProperty] public NewAssessmentInput NewAssessment { get; set; } = new();
    [BindProperty] public EditAssessmentInput EditAssessmentForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    // Every tab's data is loaded unconditionally on every request (not just the active tab) since
    // Bootstrap's tabs are just CSS show/hide - all tab content lives in one server-rendered
    // response, unlike MudTabs' lazy per-panel rendering.
    private async Task LoadAllAsync()
    {
        Categories = await assessmentConfig.GetAssessmentCategoriesAsync();
        AggregationRules = await assessmentConfig.GetResultAggregationRulesAsync();
        ExamBoards = await assessmentConfig.GetExternalExaminationBoardsAsync();
        Schools = await orgAdmin.GetSchoolsAsync();

        Schemes = await assessmentConfig.GetAssessmentSchemesAsync();
        if (SelectedSchemeId != Guid.Empty)
        {
            SchemeComponents = await assessmentConfig.GetAssessmentSchemeComponentsAsync(SelectedSchemeId);
        }

        GradeScales = await assessmentConfig.GetGradeScalesAsync();
        if (SelectedGradeScaleId != Guid.Empty)
        {
            GradeBands = await assessmentConfig.GetGradeBandsAsync(SelectedGradeScaleId);
        }

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgAdmin.GetAcademicYearsAsync(SchoolId);
            Grades = await orgAdmin.GetGradesAsync(SchoolId);
            Subjects = await curriculumAdmin.GetSubjectsAsync(SchoolId);
        }

        if (AcademicYearIdForPeriods != Guid.Empty)
        {
            EvaluationPeriods = await assessmentConfig.GetEvaluationPeriodsAsync(AcademicYearIdForPeriods);
        }

        PromotionPolicies = await assessmentConfig.GetPromotionPoliciesAsync();

        if (AcademicYearIdForAssessments != Guid.Empty)
        {
            Terms = await orgAdmin.GetTermsAsync(AcademicYearIdForAssessments);
        }
        if (TermId != Guid.Empty && GradeId != Guid.Empty && SubjectId != Guid.Empty)
        {
            Assessments = await assessmentConfig.GetAssessmentsAsync(SubjectId, GradeId, TermId);
        }
    }

    private RedirectToPageResult BackToTab(string tab, object? extraRouteValues = null)
    {
        var routeValues = new RouteValueDictionary(extraRouteValues) { ["tab"] = tab };
        return RedirectToPage(routeValues);
    }

    // ---- Assessment Schemes ----

    public async Task<IActionResult> OnPostCreateSchemeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewScheme.Code) || string.IsNullOrWhiteSpace(NewScheme.Name))
        {
            TempData["FlashMessage"] = "Code and name are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("schemes");
        }

        await assessmentConfig.CreateAssessmentSchemeAsync(NewScheme.Code, NewScheme.Name);
        TempData["FlashMessage"] = "Assessment scheme created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("schemes");
    }

    public async Task<IActionResult> OnPostSaveSchemeEditAsync()
    {
        var current = (await assessmentConfig.GetAssessmentSchemesAsync()).SingleOrDefault(s => s.Id == EditSchemeForm.Id);
        await assessmentConfig.UpdateAssessmentSchemeAsync(EditSchemeForm.Id, EditSchemeForm.Name, current?.DisplayOrder ?? 0);
        TempData["FlashMessage"] = "Assessment scheme updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("schemes", new { SelectedSchemeId });
    }

    public async Task<IActionResult> OnPostAddComponentAsync()
    {
        if (string.IsNullOrEmpty(NewComponent.AssessmentCategoryCode) || string.IsNullOrEmpty(NewComponent.ResultAggregationRuleCode))
        {
            TempData["FlashMessage"] = "Select a category and an aggregation rule.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("schemes", new { SelectedSchemeId });
        }

        try
        {
            await assessmentConfig.AddAssessmentSchemeComponentAsync(
                SelectedSchemeId, NewComponent.AssessmentCategoryCode, NewComponent.ResultAggregationRuleCode,
                NewComponent.WeightPercentage, NewComponent.DisplayOrder);
            TempData["FlashMessage"] = "Component added.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("schemes", new { SelectedSchemeId });
    }

    public async Task<IActionResult> OnPostSaveComponentEditAsync()
    {
        await assessmentConfig.UpdateAssessmentSchemeComponentAsync(EditComponentForm.Id, EditComponentForm.WeightPercentage, EditComponentForm.DisplayOrder);
        TempData["FlashMessage"] = "Component updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("schemes", new { SelectedSchemeId });
    }

    // ---- Grade Scales ----

    public async Task<IActionResult> OnPostCreateScaleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewScale.Code) || string.IsNullOrWhiteSpace(NewScale.Name))
        {
            TempData["FlashMessage"] = "Code and name are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("scales");
        }

        await assessmentConfig.CreateGradeScaleAsync(NewScale.Code, NewScale.Name);
        TempData["FlashMessage"] = "Grade scale created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("scales");
    }

    public async Task<IActionResult> OnPostSaveScaleEditAsync()
    {
        var current = (await assessmentConfig.GetGradeScalesAsync()).SingleOrDefault(s => s.Id == EditScaleForm.Id);
        await assessmentConfig.UpdateGradeScaleAsync(EditScaleForm.Id, EditScaleForm.Name, current?.DisplayOrder ?? 0);
        TempData["FlashMessage"] = "Grade scale updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("scales", new { SelectedGradeScaleId });
    }

    public async Task<IActionResult> OnPostAddBandAsync()
    {
        var existingBands = await assessmentConfig.GetGradeBandsAsync(SelectedGradeScaleId);
        await assessmentConfig.AddGradeBandAsync(
            SelectedGradeScaleId, NewBand.Code, NewBand.Name, NewBand.MinPercentage, NewBand.MaxPercentage, NewBand.Rank, existingBands.Count + 1);
        TempData["FlashMessage"] = "Grade band added.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("scales", new { SelectedGradeScaleId });
    }

    public async Task<IActionResult> OnPostSaveBandEditAsync()
    {
        var current = (await assessmentConfig.GetGradeBandsAsync(SelectedGradeScaleId)).SingleOrDefault(b => b.Id == EditBandForm.Id);
        await assessmentConfig.UpdateGradeBandAsync(
            EditBandForm.Id, EditBandForm.Name, EditBandForm.MinPercentage, EditBandForm.MaxPercentage, EditBandForm.Rank, current?.DisplayOrder ?? 0);
        TempData["FlashMessage"] = "Grade band updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("scales", new { SelectedGradeScaleId });
    }

    // ---- Evaluation Periods ----

    public async Task<IActionResult> OnPostCreatePeriodAsync()
    {
        if (AcademicYearIdForPeriods == Guid.Empty || string.IsNullOrWhiteSpace(NewPeriod.Code))
        {
            TempData["FlashMessage"] = "Select an academic year, and provide a code.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("periods", new { SchoolId, AcademicYearIdForPeriods });
        }

        await assessmentConfig.CreateEvaluationPeriodAsync(
            AcademicYearIdForPeriods, NewPeriod.Code, NewPeriod.Name, NewPeriod.StartDate, NewPeriod.EndDate, NewPeriod.DisplayOrder);
        TempData["FlashMessage"] = "Evaluation period created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("periods", new { SchoolId, AcademicYearIdForPeriods });
    }

    public async Task<IActionResult> OnPostSavePeriodEditAsync()
    {
        await assessmentConfig.UpdateEvaluationPeriodAsync(
            EditPeriodForm.Id, EditPeriodForm.Name, EditPeriodForm.StartDate, EditPeriodForm.EndDate, EditPeriodForm.DisplayOrder);
        TempData["FlashMessage"] = "Evaluation period updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("periods", new { SchoolId, AcademicYearIdForPeriods });
    }

    // ---- Promotion Policies ----

    public async Task<IActionResult> OnPostCreatePolicyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPolicy.Code) || string.IsNullOrWhiteSpace(NewPolicy.Name))
        {
            TempData["FlashMessage"] = "Code and name are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("policies");
        }

        await assessmentConfig.CreatePromotionPolicyAsync(NewPolicy.Code, NewPolicy.Name, NewPolicy.MinimumRank, NewPolicy.MinimumSubjects);
        TempData["FlashMessage"] = "Promotion policy created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("policies");
    }

    public async Task<IActionResult> OnPostSavePolicyEditAsync()
    {
        await assessmentConfig.UpdatePromotionPolicyAsync(EditPolicyForm.Id, EditPolicyForm.Name, EditPolicyForm.MinimumRank, EditPolicyForm.MinimumSubjects);
        TempData["FlashMessage"] = "Promotion policy updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("policies");
    }

    // ---- Assessments ----

    public async Task<IActionResult> OnPostCreateAssessmentAsync()
    {
        if (string.IsNullOrEmpty(NewAssessment.AssessmentCategoryCode) || SubjectId == Guid.Empty || GradeId == Guid.Empty || TermId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a subject, grade, term, and category.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("assessments", new { SchoolId, AcademicYearIdForAssessments, TermId, GradeId, SubjectId });
        }

        try
        {
            await assessmentConfig.CreateAssessmentAsync(
                SubjectId, GradeId, TermId, AcademicYearIdForAssessments, NewAssessment.AssessmentCategoryCode, NewAssessment.Title,
                NewAssessment.MaxMarks, NewAssessment.DurationMinutes, NewAssessment.ExternalExaminationBoardCode,
                NewAssessment.ExternalSyllabusCode, NewAssessment.ScheduledDate);
            TempData["FlashMessage"] = "Assessment created.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("assessments", new { SchoolId, AcademicYearIdForAssessments, TermId, GradeId, SubjectId });
    }

    public async Task<IActionResult> OnPostSaveAssessmentEditAsync()
    {
        try
        {
            // Category is init-only and not editable; exam board / external syllabus code aren't
            // shown in the compact inline edit row either - both are preserved unchanged from
            // whatever the assessment already had, since UpdateAssessmentAsync still needs them.
            var current = (await assessmentConfig.GetAssessmentsAsync(SubjectId, GradeId, TermId)).SingleOrDefault(a => a.Id == EditAssessmentForm.Id);
            var examBoardCode = current?.ExternalExaminationBoardId is { } boardId
                ? ExamBoards.SingleOrDefault(b => b.Id == boardId)?.Code
                : null;

            await assessmentConfig.UpdateAssessmentAsync(
                EditAssessmentForm.Id, EditAssessmentForm.Title, EditAssessmentForm.MaxMarks, EditAssessmentForm.DurationMinutes,
                examBoardCode, current?.ExternalSyllabusCode, EditAssessmentForm.ScheduledDate);
            TempData["FlashMessage"] = "Assessment updated.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("assessments", new { SchoolId, AcademicYearIdForAssessments, TermId, GradeId, SubjectId });
    }

    // ---- Input models ----

    public sealed class NewSchemeInput
    {
        [Required] public string Code { get; set; } = "";
        [Required] public string Name { get; set; } = "";
    }

    public sealed class EditSchemeInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class NewComponentInput
    {
        public string AssessmentCategoryCode { get; set; } = "";
        public string ResultAggregationRuleCode { get; set; } = "";
        public decimal WeightPercentage { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class EditComponentInput
    {
        public Guid Id { get; set; }
        public decimal WeightPercentage { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class NewScaleInput
    {
        [Required] public string Code { get; set; } = "";
        [Required] public string Name { get; set; } = "";
    }

    public sealed class EditScaleInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class NewBandInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal MinPercentage { get; set; }
        public decimal MaxPercentage { get; set; }
        public int Rank { get; set; }
    }

    public sealed class EditBandInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public decimal MinPercentage { get; set; }
        public decimal MaxPercentage { get; set; }
        public int Rank { get; set; }
    }

    public sealed class NewPeriodInput
    {
        [Required] public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
        public int DisplayOrder { get; set; }
    }

    public sealed class EditPeriodInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class NewPolicyInput
    {
        [Required] public string Code { get; set; } = "";
        [Required] public string Name { get; set; } = "";
        public int MinimumRank { get; set; }
        public int MinimumSubjects { get; set; }
    }

    public sealed class EditPolicyInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int MinimumRank { get; set; }
        public int MinimumSubjects { get; set; }
    }

    public sealed class NewAssessmentInput
    {
        public string AssessmentCategoryCode { get; set; } = "";
        public string Title { get; set; } = "";
        public decimal MaxMarks { get; set; }
        public int? DurationMinutes { get; set; }
        public string? ExternalExaminationBoardCode { get; set; }
        public string? ExternalSyllabusCode { get; set; }
        public DateOnly ScheduledDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    }

    public sealed class EditAssessmentInput
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public decimal MaxMarks { get; set; }
        public int? DurationMinutes { get; set; }
        public DateOnly ScheduledDate { get; set; }
    }
}

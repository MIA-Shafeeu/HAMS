using System.Security.Claims;
using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.ReportingAnalyticsAudit.Application;
using HAMS.ReportingAnalyticsAudit.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Staff;

[Authorize(Policy = StaffPolicy.Name)]
public sealed class PromotionReportCardsModel(
    IOrgAdminService orgAdmin,
    IAssessmentConfigAdminService assessmentConfig,
    IPromotionService promotionService,
    IReportCardService reportCardService,
    IRoleMembershipQuery roleMembershipQuery,
    IClock clock) : PageModel
{
    // ---- Shared scope cascade: School -> Academic Year -> Grade -> Evaluation Period. Deliberately
    // page-level (not per-tab) - mirrors the original Blazor page's own design, where both the
    // Promotion Decisions and Report Cards tabs hang off ONE set of MudSelects rendered above the
    // MudTabs, not two independent cascades. ----
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid GradeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid EvaluationPeriodId { get; set; }

    // ---- Which tab shows as active after a full-page reload ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "decisions";

    public IReadOnlyList<School> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYear> AcademicYears { get; private set; } = [];
    public IReadOnlyList<Grade> Grades { get; private set; } = [];
    public IReadOnlyList<EvaluationPeriod> EvaluationPeriods { get; private set; } = [];

    // ---- Promotion Decisions tab ----
    [BindProperty(SupportsGet = true)]
    public Guid DecisionStudentId { get; set; }

    public IReadOnlyList<ClassRosterEntry> StudentsNeedingDecision { get; private set; } = [];
    public PromotionEligibilityResult? Eligibility { get; private set; }
    public string? EligibilityError { get; private set; }
    public IReadOnlyList<PromotionDecision> PastDecisions { get; private set; } = [];

    [BindProperty]
    public DecisionInput Decision { get; set; } = new();

    // ---- Report Cards tab ----
    [BindProperty(SupportsGet = true)]
    public Guid ReportCardStudentId { get; set; }

    // The report card currently being tracked through its Draft -> Submitted -> UnderReview ->
    // Approved/Rejected/Returned workflow - set once PrepareReportCard succeeds, and threaded
    // through every subsequent transition redirect. Per IReportCardService.GetStudentsNeedingReportCardAsync's
    // own doc comment, a student with an already-prepared report card drops off that worklist, so
    // there is no OTHER way to find your way back to an in-progress card after a full-page reload.
    [BindProperty(SupportsGet = true)]
    public Guid? ReportCardId { get; set; }

    public IReadOnlyList<ClassRosterEntry> StudentsNeedingReportCard { get; private set; } = [];
    public ReportCard? PreparedReportCard { get; private set; }

    [BindProperty]
    public PrepareReportCardInput PrepareForm { get; set; } = new();

    private Guid? CurrentPersonId => Guid.TryParse(User.FindFirstValue(HamsClaimTypes.PersonId), out var id) ? id : null;

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

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgAdmin.GetAcademicYearsAsync(SchoolId);
            Grades = await orgAdmin.GetGradesAsync(SchoolId);
        }

        if (AcademicYearId != Guid.Empty)
        {
            EvaluationPeriods = await assessmentConfig.GetEvaluationPeriodsAsync(AcademicYearId);
        }

        var today = clock.TodayUtc;

        if (GradeId != Guid.Empty && AcademicYearId != Guid.Empty)
        {
            StudentsNeedingDecision = await promotionService.GetStudentsNeedingDecisionAsync(GradeId, AcademicYearId, today);

            StudentsNeedingReportCard = EvaluationPeriodId == Guid.Empty
                ? []
                : await reportCardService.GetStudentsNeedingReportCardAsync(GradeId, AcademicYearId, EvaluationPeriodId, today);
        }

        if (DecisionStudentId != Guid.Empty)
        {
            PastDecisions = await promotionService.GetDecisionsForStudentAsync(DecisionStudentId);

            if (EvaluationPeriodId != Guid.Empty)
            {
                try
                {
                    Eligibility = await promotionService.EvaluateEligibilityAsync(DecisionStudentId, AcademicYearId, EvaluationPeriodId, today);
                }
                catch (InvalidOperationException ex)
                {
                    EligibilityError = ex.Message;
                }
            }
            else
            {
                EligibilityError = "Select an evaluation period above to check eligibility.";
            }
        }

        if (ReportCardId is { } reportCardId)
        {
            PreparedReportCard = await reportCardService.GetAsync(reportCardId);
        }
    }

    private RedirectToPageResult BackToDecisions() =>
        RedirectToPage(new { SchoolId, AcademicYearId, GradeId, EvaluationPeriodId, DecisionStudentId, Tab = "decisions" });

    private RedirectToPageResult BackToReportCards() =>
        RedirectToPage(new { SchoolId, AcademicYearId, GradeId, EvaluationPeriodId, ReportCardStudentId, ReportCardId, Tab = "reportcards" });

    // ---- Promotion Decisions ----

    public async Task<IActionResult> OnPostRecordDecisionAsync()
    {
        if (CurrentPersonId is not { } decidedBy || Decision.DecisionDate is null)
        {
            TempData["FlashMessage"] = "Set a decision date.";
            TempData["FlashSeverity"] = "warning";
            return BackToDecisions();
        }

        if (!await roleMembershipQuery.IsSystemOrSchoolAdminAsync(decidedBy, clock.TodayUtc))
        {
            TempData["FlashMessage"] = "Only a System or School Administrator may record a promotion decision.";
            TempData["FlashSeverity"] = "danger";
            return BackToDecisions();
        }

        try
        {
            await promotionService.RecordDecisionAsync(
                DecisionStudentId, AcademicYearId, Decision.Promoted,
                Decision.NextGradeId == Guid.Empty ? null : Decision.NextGradeId, decidedBy, Decision.DecisionDate.Value, Decision.Notes);

            TempData["FlashMessage"] = "Promotion decision recorded.";
            TempData["FlashSeverity"] = "success";

            // Deliberately drop DecisionStudentId - back to the worklist, matching the original's
            // own post-success reset (_selectedDecisionStudentId = Guid.Empty).
            return RedirectToPage(new { SchoolId, AcademicYearId, GradeId, EvaluationPeriodId, Tab = "decisions" });
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
            return BackToDecisions();
        }
    }

    // ---- Report Cards ----

    public async Task<IActionResult> OnPostPrepareReportCardAsync()
    {
        if (CurrentPersonId is not { } preparedBy)
        {
            TempData["FlashMessage"] = "Could not resolve the current staff member.";
            TempData["FlashSeverity"] = "danger";
            return BackToReportCards();
        }

        try
        {
            var request = new PrepareReportCardRequest(
                ReportCardStudentId, AcademicYearId, EvaluationPeriodId,
                PrepareForm.NarrativeEn, PrepareForm.NarrativeDv, PrepareForm.NextStepsEn, PrepareForm.NextStepsDv, preparedBy);
            ReportCardId = await reportCardService.PrepareAsync(request);

            TempData["FlashMessage"] = "Report card prepared as Draft.";
            TempData["FlashSeverity"] = "success";
            return BackToReportCards();
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
            return BackToReportCards();
        }
    }

    public Task<IActionResult> OnPostSubmitReportCardAsync(Guid reportCardId) =>
        RunTransitionAsync(() => reportCardService.SubmitAsync(reportCardId), "Report card submitted.");

    public Task<IActionResult> OnPostBeginReviewReportCardAsync(Guid reportCardId) =>
        RunTransitionAsync(() => reportCardService.BeginReviewAsync(reportCardId), "Review started.");

    public Task<IActionResult> OnPostApproveReportCardAsync(Guid reportCardId) =>
        RunTransitionAsync(() => reportCardService.ApproveAsync(reportCardId), "Report card approved and published.");

    public Task<IActionResult> OnPostRejectReportCardAsync(Guid reportCardId) =>
        RunTransitionAsync(() => reportCardService.RejectAsync(reportCardId), "Report card rejected.");

    public Task<IActionResult> OnPostReturnReportCardAsync(Guid reportCardId) =>
        RunTransitionAsync(() => reportCardService.ReturnAsync(reportCardId), "Report card returned.");

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

        return BackToReportCards();
    }

    public sealed class DecisionInput
    {
        public bool Promoted { get; set; }
        public Guid NextGradeId { get; set; }
        public DateOnly? DecisionDate { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class PrepareReportCardInput
    {
        public string NarrativeEn { get; set; } = "";
        public string NarrativeDv { get; set; } = "";
        public string NextStepsEn { get; set; } = "";
        public string NextStepsDv { get; set; } = "";
    }
}

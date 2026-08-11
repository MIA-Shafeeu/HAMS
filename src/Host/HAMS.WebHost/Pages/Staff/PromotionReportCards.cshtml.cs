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
using HAMS.TeachingTimetable.Application;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Staff;

[Authorize(Policy = StaffPolicy.Name)]
public sealed class PromotionReportCardsModel(
    IOrgAdminService orgAdmin,
    IOrgStructureLookup orgLookup,
    IAssessmentConfigAdminService assessmentConfig,
    IPromotionService promotionService,
    IReportCardService reportCardService,
    IStudentEnrollmentService enrollmentService,
    IRoleMembershipQuery roleMembershipQuery,
    IStaffAccessScopeQuery scopeQuery,
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

    /// <summary>False when <see cref="GradeId"/> is set but isn't one of the caller's assigned
    /// grades (e.g. a stale link, or a directly-edited query string) - neither tab's grade-scoped
    /// content is loaded at all in that case, and the page shows an access-denied message instead
    /// of silently rendering nothing. Always true once <see cref="GradeId"/> is empty (nothing
    /// selected yet to deny). Page-level (not per-tab), mirroring the shared School -> Academic Year
    /// -> Grade -> Evaluation Period cascade above.</summary>
    public bool GradeAccessAuthorized { get; private set; } = true;

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
        var personId = CurrentPersonId ?? Guid.Empty;

        // Resolved twice, deliberately: once against just the caller's accessible Schools (before
        // an Academic Year is even chosen - GetScopeAsync's own null-schoolId shortcut skips the
        // OrgCurriculum/SubjectTeachingAssignment joins entirely for this cheap first pass), then
        // again scoped to whichever School+Year end up selected, once Grades need filtering too.
        var schoolScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId: null, academicYearId: null);

        var allSchools = await orgAdmin.GetSchoolsAsync();
        Schools = schoolScope.HasUnrestrictedAccess ? allSchools : [.. allSchools.Where(s => schoolScope.CanAccessSchool(s.Id))];

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgAdmin.GetAcademicYearsAsync(SchoolId);
            Grades = await orgAdmin.GetGradesAsync(SchoolId);
        }

        if (AcademicYearId != Guid.Empty)
        {
            EvaluationPeriods = await assessmentConfig.GetEvaluationPeriodsAsync(AcademicYearId);
        }

        StaffAccessScope? fullScope = null;
        if (SchoolId != Guid.Empty && AcademicYearId != Guid.Empty)
        {
            fullScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, SchoolId, AcademicYearId);
            Grades = fullScope.HasUnrestrictedAccess ? Grades : [.. Grades.Where(g => fullScope.CanAccessGrade(g.Id))];
        }

        var today = clock.TodayUtc;

        if (GradeId != Guid.Empty)
        {
            GradeAccessAuthorized = fullScope?.CanAccessGrade(GradeId) ?? false;
        }

        if (GradeAccessAuthorized && GradeId != Guid.Empty && AcademicYearId != Guid.Empty)
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

        // Re-derived from a fresh scope check, never trusted from the posted GradeId alone - a
        // Razor Page POST handler is a directly-callable HTTP endpoint, so the grade picker only
        // being filtered client-side would not be enough on its own to stop a tampered GradeId.
        var scope = await scopeQuery.GetScopeAsync(decidedBy, clock.TodayUtc, SchoolId, AcademicYearId);
        if (!scope.CanAccessGrade(GradeId))
        {
            TempData["FlashMessage"] = "You do not have access to this grade.";
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

        // Re-derived from a fresh scope check, never trusted from the posted GradeId alone - a
        // Razor Page POST handler is a directly-callable HTTP endpoint, so the grade picker only
        // being filtered client-side would not be enough on its own to stop a tampered GradeId.
        var scope = await scopeQuery.GetScopeAsync(preparedBy, clock.TodayUtc, SchoolId, AcademicYearId);
        if (!scope.CanAccessGrade(GradeId))
        {
            TempData["FlashMessage"] = "You do not have access to this grade.";
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

    public async Task<IActionResult> OnPostSubmitReportCardAsync(Guid reportCardId)
    {
        if (!await IsAuthorizedForReportCardAsync(reportCardId))
        {
            return AccessDeniedToReportCard();
        }

        return await RunTransitionAsync(() => reportCardService.SubmitAsync(reportCardId), "Report card submitted.");
    }

    public async Task<IActionResult> OnPostBeginReviewReportCardAsync(Guid reportCardId)
    {
        if (!await IsAuthorizedForReportCardAsync(reportCardId))
        {
            return AccessDeniedToReportCard();
        }

        return await RunTransitionAsync(() => reportCardService.BeginReviewAsync(reportCardId), "Review started.");
    }

    public async Task<IActionResult> OnPostApproveReportCardAsync(Guid reportCardId)
    {
        if (!await IsAuthorizedForReportCardAsync(reportCardId))
        {
            return AccessDeniedToReportCard();
        }

        return await RunTransitionAsync(() => reportCardService.ApproveAsync(reportCardId), "Report card approved and published.");
    }

    public async Task<IActionResult> OnPostRejectReportCardAsync(Guid reportCardId)
    {
        if (!await IsAuthorizedForReportCardAsync(reportCardId))
        {
            return AccessDeniedToReportCard();
        }

        return await RunTransitionAsync(() => reportCardService.RejectAsync(reportCardId), "Report card rejected.");
    }

    public async Task<IActionResult> OnPostReturnReportCardAsync(Guid reportCardId)
    {
        if (!await IsAuthorizedForReportCardAsync(reportCardId))
        {
            return AccessDeniedToReportCard();
        }

        return await RunTransitionAsync(() => reportCardService.ReturnAsync(reportCardId), "Report card returned.");
    }

    /// <summary>
    /// Re-derives the caller's access to an EXISTING report card from scratch - never trusted from
    /// whatever the page happened to have loaded for a DIFFERENT grade, since the posted
    /// <paramref name="reportCardId"/> is an independent input a caller could point at any report
    /// card system-wide. A <see cref="ReportCard"/> carries no GradeId/SchoolId of its own (only
    /// <c>StudentPersonId</c>+<c>AcademicYearId</c>+<c>EvaluationPeriodId</c>), so its grade is
    /// resolved the same way the page's own worklist is built: the student's active enrolment for
    /// that year.
    /// </summary>
    private async Task<bool> IsAuthorizedForReportCardAsync(Guid reportCardId)
    {
        if (CurrentPersonId is not { } personId)
        {
            return false;
        }

        var reportCard = await reportCardService.GetAsync(reportCardId);
        if (reportCard is null)
        {
            return false;
        }

        var enrollment = await enrollmentService.GetActiveEnrollmentAsync(reportCard.StudentPersonId, reportCard.AcademicYearId, clock.TodayUtc);
        if (enrollment is null || await orgLookup.GetGradeSchoolIdAsync(enrollment.GradeId) is not { } schoolId)
        {
            return false;
        }

        var scope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId, reportCard.AcademicYearId);
        return scope.CanAccessGrade(enrollment.GradeId);
    }

    private IActionResult AccessDeniedToReportCard()
    {
        TempData["FlashMessage"] = "You do not have access to this report card.";
        TempData["FlashSeverity"] = "danger";
        return BackToReportCards();
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

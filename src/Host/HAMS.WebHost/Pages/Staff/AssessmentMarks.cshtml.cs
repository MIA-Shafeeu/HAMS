using HAMS.AssessmentEvaluation.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Staff;

[Authorize(Policy = StaffPolicy.Name)]
public sealed class AssessmentMarksModel(
    IOrgStructureLookup orgLookup,
    IStudentEnrollmentService enrollmentService,
    IKeyStagePolicyResolver policyResolver,
    IAssessmentLookup assessmentLookup,
    IAssessmentModerationService moderationService,
    IRoleMembershipQuery roleMembershipQuery,
    ICurrentUser currentUser,
    IClock clock) : PageModel
{
    // ---- Scope cascade: School -> Year -> {Grade, Term, Subject} (parallel) -> Assessment. All six
    // live in one <form method="get"> (see the .cshtml) since Grade/Term/Subject each depend only on
    // School/Year, never on each other - same "several independent selects feeding one dependent view"
    // shape as OrgStructureModel's Classes tab, just one level deeper.
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid GradeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid TermId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SubjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AssessmentId { get; set; }

    public IReadOnlyList<SchoolOption> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYearOption> AcademicYears { get; private set; } = [];
    public IReadOnlyList<GradeOption> Grades { get; private set; } = [];
    public IReadOnlyList<TermOption> Terms { get; private set; } = [];
    public IReadOnlyList<SubjectOption> Subjects { get; private set; } = [];
    public IReadOnlyList<AssessmentOption> Assessments { get; private set; } = [];

    /// <summary>Null once an assessment is selected but its grade has no published key-stage policy for this academic year yet - marks cannot be recorded in that state.</summary>
    public Guid? PolicyId { get; private set; }

    /// <summary>
    /// Live per-request equivalent of the old Blazor page's circuit-lifetime <c>_isAdmin</c> field -
    /// recomputed on every GET/POST rather than cached, since nothing here persists between requests
    /// (build plan §4: always a live <see cref="IRoleMembershipQuery"/> query, never a cached value).
    /// </summary>
    public bool IsAdmin { get; private set; }

    public List<MarkRowView> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Schools = await orgLookup.GetSchoolsAsync();

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);
            Grades = await orgLookup.GetGradesAsync(SchoolId);
            Subjects = await orgLookup.GetSubjectsAsync(SchoolId);
        }

        if (AcademicYearId != Guid.Empty)
        {
            Terms = await orgLookup.GetTermsAsync(AcademicYearId);
        }

        if (GradeId != Guid.Empty && TermId != Guid.Empty && SubjectId != Guid.Empty)
        {
            Assessments = await assessmentLookup.GetAssessmentsAsync(SubjectId, GradeId, TermId);
        }

        IsAdmin = await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock);

        if (AssessmentId == Guid.Empty)
        {
            return;
        }

        var policy = await policyResolver.ResolveAsync(GradeId, AcademicYearId, clock.TodayUtc);
        PolicyId = policy?.Id;
        if (PolicyId is null)
        {
            return;
        }

        var roster = await enrollmentService.GetActiveRosterForGradeAsync(GradeId, AcademicYearId, clock.TodayUtc);
        var results = await assessmentLookup.GetResultsForAssessmentAsync(AssessmentId);
        var resultsByStudent = results.ToDictionary(r => r.StudentPersonId);

        Rows = roster
            .Select(r =>
            {
                resultsByStudent.TryGetValue(r.StudentPersonId, out var result);
                return new MarkRowView(
                    r.StudentPersonId,
                    r.NameEn,
                    result?.Id,
                    result?.RawMark,
                    result?.AdjustedMark,
                    result?.ModeratedMark,
                    result?.FinalMark,
                    result?.ModerationStatus ?? WorkflowStatus.Draft);
            })
            .ToList();
    }

    private RedirectToPageResult BackToMarks() =>
        RedirectToPage(new { SchoolId, AcademicYearId, GradeId, TermId, SubjectId, AssessmentId });

    // Raw marks are entered inline, one student at a time, exactly like the old Blazor page's
    // per-row RecordMarkAsync - never a single bulk save for the whole roster.
    public async Task<IActionResult> OnPostRecordMarkAsync(Guid studentPersonId, decimal? rawMark)
    {
        if (currentUser.PersonId is not { } recordedBy)
        {
            return BackToMarks();
        }

        if (rawMark is null)
        {
            TempData["FlashMessage"] = "Enter a raw mark before recording.";
            TempData["FlashSeverity"] = "warning";
            return BackToMarks();
        }

        var policy = await policyResolver.ResolveAsync(GradeId, AcademicYearId, clock.TodayUtc);
        if (policy is null)
        {
            TempData["FlashMessage"] = "This grade has no published key-stage policy for this academic year.";
            TempData["FlashSeverity"] = "danger";
            return BackToMarks();
        }

        try
        {
            await moderationService.RecordRawMarkAsync(AssessmentId, studentPersonId, policy.Id, rawMark, null, recordedBy);
            TempData["FlashMessage"] = "Mark recorded.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToMarks();
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid resultId)
    {
        await RunTransitionAsync(() => moderationService.SubmitAsync(resultId));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostBeginReviewAsync(Guid resultId)
    {
        await RunTransitionAsync(() => moderationService.BeginReviewAsync(resultId, null));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid resultId)
    {
        if (!await CanDecideAsync(resultId))
        {
            TempData["FlashMessage"] = "Only a System/School Administrator may decide an escalated result.";
            TempData["FlashSeverity"] = "danger";
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.ApproveAsync(resultId, null));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid resultId)
    {
        if (!await CanDecideAsync(resultId))
        {
            TempData["FlashMessage"] = "Only a System/School Administrator may decide an escalated result.";
            TempData["FlashSeverity"] = "danger";
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.RejectAsync(resultId));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostReturnAsync(Guid resultId)
    {
        await RunTransitionAsync(() => moderationService.ReturnAsync(resultId));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostEscalateAsync(Guid resultId)
    {
        if (currentUser.PersonId is not { } escalatedBy)
        {
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.EscalateAsync(
            resultId, escalatedBy, "Escalated for a senior decision from the marks-entry screen."));
        return BackToMarks();
    }

    /// <summary>
    /// Re-derives whether the caller may decide THIS result from a fresh read, never from a
    /// client-supplied hidden field - the escalation gate only matters at all when the result is
    /// currently Escalated, and unlike the old Blazor page (whose <c>row.Status</c> lived in
    /// server-side circuit memory the browser could never tamper with), a Razor Page POST has no
    /// server-trusted state of its own between requests.
    /// </summary>
    private async Task<bool> CanDecideAsync(Guid resultId)
    {
        var results = await assessmentLookup.GetResultsForAssessmentAsync(AssessmentId);
        var current = results.FirstOrDefault(r => r.Id == resultId);
        if (current is null || current.ModerationStatus != WorkflowStatus.Escalated)
        {
            return true;
        }

        return await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock);
    }

    private async Task RunTransitionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }
    }

    public sealed record MarkRowView(
        Guid StudentPersonId,
        string StudentName,
        Guid? ResultId,
        decimal? RawMark,
        decimal? AdjustedMark,
        decimal? ModeratedMark,
        decimal? FinalMark,
        WorkflowStatus Status);
}

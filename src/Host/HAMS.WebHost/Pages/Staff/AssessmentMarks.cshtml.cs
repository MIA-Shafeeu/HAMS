using HAMS.AssessmentEvaluation.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;
using HAMS.TeachingTimetable.Application;
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
    IStaffAccessScopeQuery scopeQuery,
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

    /// <summary>False when <see cref="GradeId"/> is set but isn't one of the caller's assigned
    /// grades (e.g. a stale link, or a directly-edited query string) - the mark rows are then not
    /// loaded at all, and the page shows an access-denied message instead of silently rendering
    /// nothing. Always true once <see cref="GradeId"/> is empty (nothing selected yet to deny).</summary>
    public bool GradeAccessAuthorized { get; private set; } = true;

    /// <summary>
    /// Live per-request equivalent of the old Blazor page's circuit-lifetime <c>_isAdmin</c> field -
    /// recomputed on every GET/POST rather than cached, since nothing here persists between requests
    /// (build plan §4: always a live <see cref="IRoleMembershipQuery"/> query, never a cached value).
    /// </summary>
    public bool IsAdmin { get; private set; }

    public List<MarkRowView> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var personId = currentUser.PersonId ?? Guid.Empty;

        // Resolved twice, deliberately: once against just the caller's accessible Schools (before
        // an Academic Year is even chosen - GetScopeAsync's own null-schoolId shortcut skips the
        // OrgCurriculum/SubjectTeachingAssignment joins entirely for this cheap first pass), then
        // again scoped to whichever School+Year end up selected, once Grades need filtering too.
        // Same pattern as Attendance.cshtml.cs, just for Grade instead of Class.
        var schoolScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId: null, academicYearId: null);

        var allSchools = await orgLookup.GetSchoolsAsync();
        Schools = schoolScope.HasUnrestrictedAccess ? allSchools : [.. allSchools.Where(s => schoolScope.CanAccessSchool(s.Id))];

        StaffAccessScope? fullScope = null;
        if (SchoolId != Guid.Empty && AcademicYearId != Guid.Empty)
        {
            fullScope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, SchoolId, AcademicYearId);
        }

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgLookup.GetAcademicYearsAsync(SchoolId);

            var allGrades = await orgLookup.GetGradesAsync(SchoolId);
            Grades = fullScope is null || fullScope.HasUnrestrictedAccess
                ? allGrades
                : [.. allGrades.Where(g => fullScope.CanAccessGrade(g.Id))];

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

        if (GradeId != Guid.Empty)
        {
            GradeAccessAuthorized = fullScope?.CanAccessGrade(GradeId) ?? false;
        }

        if (AssessmentId == Guid.Empty)
        {
            return;
        }

        if (!GradeAccessAuthorized)
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

        // Re-derived from a fresh scope check, never trusted from the posted GradeId alone - a
        // Razor Page POST handler is a directly-callable HTTP endpoint, so the grade picker only
        // being filtered client-side (and server-side on the preceding GET) would not be enough on
        // its own to stop a tampered GradeId. Same pattern as Attendance's OnPostSaveAttendanceAsync.
        var scope = await scopeQuery.GetScopeAsync(recordedBy, clock.TodayUtc, SchoolId, AcademicYearId);
        if (!scope.CanAccessGrade(GradeId))
        {
            TempData["FlashMessage"] = "You do not have access to this grade.";
            TempData["FlashSeverity"] = "danger";
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
        if (!await TryAuthorizeResultTransitionAsync(resultId))
        {
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.SubmitAsync(resultId));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostBeginReviewAsync(Guid resultId)
    {
        if (!await TryAuthorizeResultTransitionAsync(resultId))
        {
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.BeginReviewAsync(resultId, null));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid resultId)
    {
        if (!await TryAuthorizeResultTransitionAsync(resultId))
        {
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.ApproveAsync(resultId, null));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid resultId)
    {
        if (!await TryAuthorizeResultTransitionAsync(resultId))
        {
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.RejectAsync(resultId));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostReturnAsync(Guid resultId)
    {
        if (!await TryAuthorizeResultTransitionAsync(resultId))
        {
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.ReturnAsync(resultId));
        return BackToMarks();
    }

    public async Task<IActionResult> OnPostEscalateAsync(Guid resultId)
    {
        if (currentUser.PersonId is not { } escalatedBy || !await TryAuthorizeResultTransitionAsync(resultId))
        {
            return BackToMarks();
        }

        await RunTransitionAsync(() => moderationService.EscalateAsync(
            resultId, escalatedBy, "Escalated for a senior decision from the marks-entry screen."));
        return BackToMarks();
    }

    private enum ResultTransitionAuthorization { Authorized, NotFound, NoGradeAccess, RequiresAdminForEscalated }

    /// <summary>
    /// Re-derives whether the caller may act on THIS result from a fresh read, never from a
    /// client-supplied hidden field or whatever the page's own currently-selected Grade/Assessment
    /// happens to be - <paramref name="resultId"/> is an independent POST input a caller could point
    /// at any result system-wide. Two independent gates, both re-checked here: (1) teaching-scope
    /// access to the result's own Grade (resolved via its Assessment, since neither
    /// <see cref="AssessmentResult"/> nor even <see cref="Assessment"/> carry a School directly), and
    /// (2) the pre-existing "an Escalated result needs a System/School Administrator" rule, now
    /// applied to every transition action rather than only Approve/Reject.
    /// </summary>
    private async Task<ResultTransitionAuthorization> AuthorizeResultTransitionAsync(Guid resultId)
    {
        if (currentUser.PersonId is not { } personId)
        {
            return ResultTransitionAuthorization.NotFound;
        }

        var result = await moderationService.GetAsync(resultId);
        var assessment = result is null ? null : await assessmentLookup.GetAssessmentAsync(result.AssessmentId);
        if (result is null || assessment is null || await orgLookup.GetGradeSchoolIdAsync(assessment.GradeId) is not { } schoolId)
        {
            return ResultTransitionAuthorization.NotFound;
        }

        var scope = await scopeQuery.GetScopeAsync(personId, clock.TodayUtc, schoolId, assessment.AcademicYearId);
        if (!scope.CanAccessGrade(assessment.GradeId))
        {
            return ResultTransitionAuthorization.NoGradeAccess;
        }

        if (result.ModerationStatus == WorkflowStatus.Escalated && !await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock))
        {
            return ResultTransitionAuthorization.RequiresAdminForEscalated;
        }

        return ResultTransitionAuthorization.Authorized;
    }

    private async Task<bool> TryAuthorizeResultTransitionAsync(Guid resultId)
    {
        var authorization = await AuthorizeResultTransitionAsync(resultId);
        if (authorization == ResultTransitionAuthorization.Authorized)
        {
            return true;
        }

        TempData["FlashMessage"] = authorization switch
        {
            ResultTransitionAuthorization.NoGradeAccess => "You do not have access to this grade.",
            ResultTransitionAuthorization.RequiresAdminForEscalated => "Only a System/School Administrator may decide an escalated result.",
            _ => "That result no longer exists.",
        };
        TempData["FlashSeverity"] = "danger";
        return false;
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

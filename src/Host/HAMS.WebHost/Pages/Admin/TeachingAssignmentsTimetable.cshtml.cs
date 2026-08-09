using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class TeachingAssignmentsTimetableModel(
    IOrgAdminService orgAdmin,
    ICurriculumAdminService curriculumAdmin,
    IPeopleAdminService peopleAdmin,
    ISubjectTeachingAssignmentService subjectTeachingAssignments,
    IClassTeacherAssignmentService classTeacherAssignments,
    ILeadingTeacherAssignmentService leadingTeacherAssignments,
    ISubstitutionService substitutions,
    IPeriodAdminService periodAdmin,
    ITimetableService timetableService) : PageModel
{
    // ---- Tab selection (which tab shows as active after a full-page reload) ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "subject";

    // ---- Page-level School -> Academic Year cascade (shared by all 5 tabs) ----
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    public IReadOnlyList<School> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYear> AcademicYears { get; private set; } = [];
    public IReadOnlyList<Class> Classes { get; private set; } = [];
    public IReadOnlyList<Subject> Subjects { get; private set; } = [];
    public IReadOnlyList<StaffProfileSummary> Staff { get; private set; } = [];
    public IReadOnlyList<Period> Periods { get; private set; } = [];

    // ---- Subject Teaching tab ----
    // Deliberately separate from ClassTeacherClassId/TimetableClassId below - Subject Teaching,
    // Class Teacher and Timetable each have their OWN Class dropdown. Sharing one property was a
    // repeatedly-hit bug in the original Blazor page: switching tabs made a class look already
    // "selected" while that tab's own dependent table/list was never (re)loaded for it.
    [BindProperty(SupportsGet = true)]
    public Guid SubjectTeachingClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid SubstitutionTargetAssignmentId { get; set; }

    public IReadOnlyList<SubjectTeachingAssignment> SubjectTeachingAssignments { get; private set; } = [];

    [BindProperty] public NewSubjectTeachingInput NewSubjectTeaching { get; set; } = new();
    [BindProperty] public NewSubstitutionInput NewSubstitution { get; set; } = new();

    // ---- Class Teacher tab ----
    [BindProperty(SupportsGet = true)]
    public Guid ClassTeacherClassId { get; set; }

    public IReadOnlyList<ClassTeacherAssignment> ClassTeacherAssignments { get; private set; } = [];

    [BindProperty] public NewClassTeacherInput NewClassTeacher { get; set; } = new();

    // ---- Leading Teacher tab ----
    [BindProperty(SupportsGet = true)]
    public Guid LeadingTeacherSubjectId { get; set; }

    public IReadOnlyList<LeadingTeacherAssignment> LeadingTeacherAssignments { get; private set; } = [];

    [BindProperty] public NewLeadingTeacherInput NewLeadingTeacher { get; set; } = new();

    // ---- Periods tab ----
    [BindProperty(SupportsGet = true)]
    public Guid? EditPeriodId { get; set; }

    [BindProperty] public NewPeriodInput NewPeriod { get; set; } = new();
    [BindProperty] public EditPeriodInput EditPeriodForm { get; set; } = new();

    // ---- Timetable tab ----
    [BindProperty(SupportsGet = true)]
    public Guid TimetableClassId { get; set; }

    public IReadOnlyList<TimetableEntry> TimetableEntries { get; private set; } = [];

    // Separate from SubjectTeachingAssignments (the Subject Teaching tab's own list) above - both
    // tabs can have a DIFFERENT class selected at once; sharing one list meant loading the
    // Timetable tab's class silently overwrote the data the Subject Teaching tab's table was
    // showing (same class of bug as the Class dropdowns, see comment above).
    public IReadOnlyList<SubjectTeachingAssignment> TimetableSubjectTeachingAssignments { get; private set; } = [];

    [BindProperty] public NewTimetableEntryInput NewTimetableEntry { get; set; } = new();

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
        Staff = await peopleAdmin.GetStaffProfilesAsync();

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgAdmin.GetAcademicYearsAsync(SchoolId);
            Subjects = await curriculumAdmin.GetSubjectsAsync(SchoolId);
            Periods = await periodAdmin.GetPeriodsAsync(SchoolId);
        }

        if (AcademicYearId != Guid.Empty)
        {
            Classes = await orgAdmin.GetClassesAsync(AcademicYearId);
        }

        if (AcademicYearId != Guid.Empty && SubjectTeachingClassId != Guid.Empty)
        {
            SubjectTeachingAssignments = await subjectTeachingAssignments.GetAssignmentsForClassAsync(SubjectTeachingClassId, AcademicYearId);
        }

        if (AcademicYearId != Guid.Empty && ClassTeacherClassId != Guid.Empty)
        {
            ClassTeacherAssignments = await classTeacherAssignments.GetAssignmentsForClassAsync(ClassTeacherClassId, AcademicYearId);
        }

        if (AcademicYearId != Guid.Empty && LeadingTeacherSubjectId != Guid.Empty)
        {
            LeadingTeacherAssignments = await leadingTeacherAssignments.GetAssignmentsForSubjectAsync(LeadingTeacherSubjectId, AcademicYearId);
        }

        if (AcademicYearId != Guid.Empty && TimetableClassId != Guid.Empty)
        {
            TimetableEntries = await timetableService.GetEntriesForClassAsync(TimetableClassId, AcademicYearId);
            TimetableSubjectTeachingAssignments = await subjectTeachingAssignments.GetAssignmentsForClassAsync(TimetableClassId, AcademicYearId);
        }
    }

    public string StaffName(Guid personId) => Staff.SingleOrDefault(s => s.PersonId == personId)?.NameEn ?? personId.ToString();
    public string SubjectName(Guid subjectId) => Subjects.SingleOrDefault(s => s.Id == subjectId)?.Name ?? subjectId.ToString();
    public string PeriodName(Guid periodId) => Periods.SingleOrDefault(p => p.Id == periodId)?.Name ?? periodId.ToString();

    private RedirectToPageResult BackToTab(string tab, object? extraRouteValues = null)
    {
        var routeValues = new RouteValueDictionary(extraRouteValues) { ["tab"] = tab, ["SchoolId"] = SchoolId, ["AcademicYearId"] = AcademicYearId };
        return RedirectToPage(routeValues);
    }

    // ---- Subject Teaching ----

    public async Task<IActionResult> OnPostAssignSubjectTeachingAsync()
    {
        if (SubjectTeachingClassId == Guid.Empty || NewSubjectTeaching.StaffPersonId == Guid.Empty
            || NewSubjectTeaching.SubjectId == Guid.Empty || NewSubjectTeaching.EffectiveFrom is null)
        {
            TempData["FlashMessage"] = "Select staff, subject and an effective date.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("subject", new { SubjectTeachingClassId });
        }

        await subjectTeachingAssignments.AssignAsync(
            NewSubjectTeaching.StaffPersonId, NewSubjectTeaching.SubjectId, SubjectTeachingClassId, AcademicYearId, SchoolId,
            NewSubjectTeaching.EffectiveFrom.Value, null);

        TempData["FlashMessage"] = "Subject teaching assignment created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("subject", new { SubjectTeachingClassId });
    }

    public async Task<IActionResult> OnPostEndSubjectTeachingAsync(Guid assignmentId)
    {
        await subjectTeachingAssignments.EndAsync(assignmentId, DateOnly.FromDateTime(DateTime.Today));
        TempData["FlashMessage"] = "Subject teaching assignment ended.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("subject", new { SubjectTeachingClassId });
    }

    public async Task<IActionResult> OnPostCreateSubstitutionAsync()
    {
        if (NewSubstitution.SubstituteStaffPersonId == Guid.Empty || NewSubstitution.SubstitutionDate is null)
        {
            TempData["FlashMessage"] = "Select a substitute staff member and date.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("subject", new { SubjectTeachingClassId, SubstitutionTargetAssignmentId });
        }

        try
        {
            await substitutions.CreateSubstitutionAsync(
                SubstitutionTargetAssignmentId, NewSubstitution.SubstituteStaffPersonId, NewSubstitution.SubstitutionDate.Value,
                SchoolId, NewSubstitution.Reason);
            TempData["FlashMessage"] = "Substitution created.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
            return BackToTab("subject", new { SubjectTeachingClassId, SubstitutionTargetAssignmentId });
        }

        return BackToTab("subject", new { SubjectTeachingClassId });
    }

    // ---- Class Teacher ----

    public async Task<IActionResult> OnPostAssignClassTeacherAsync()
    {
        if (ClassTeacherClassId == Guid.Empty || NewClassTeacher.StaffPersonId == Guid.Empty || NewClassTeacher.EffectiveFrom is null)
        {
            TempData["FlashMessage"] = "Select staff and an effective date.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("classTeacher", new { ClassTeacherClassId });
        }

        await classTeacherAssignments.AssignAsync(
            NewClassTeacher.StaffPersonId, ClassTeacherClassId, AcademicYearId, SchoolId, NewClassTeacher.EffectiveFrom.Value, null);

        TempData["FlashMessage"] = "Class teacher assignment created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("classTeacher", new { ClassTeacherClassId });
    }

    public async Task<IActionResult> OnPostEndClassTeacherAsync(Guid assignmentId)
    {
        await classTeacherAssignments.EndAsync(assignmentId, DateOnly.FromDateTime(DateTime.Today));
        TempData["FlashMessage"] = "Class teacher assignment ended.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("classTeacher", new { ClassTeacherClassId });
    }

    // ---- Leading Teacher ----

    public async Task<IActionResult> OnPostAssignLeadingTeacherAsync()
    {
        if (LeadingTeacherSubjectId == Guid.Empty || NewLeadingTeacher.StaffPersonId == Guid.Empty || NewLeadingTeacher.EffectiveFrom is null)
        {
            TempData["FlashMessage"] = "Select staff and an effective date.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("leadingTeacher", new { LeadingTeacherSubjectId });
        }

        await leadingTeacherAssignments.AssignAsync(
            NewLeadingTeacher.StaffPersonId, LeadingTeacherSubjectId, AcademicYearId, SchoolId, NewLeadingTeacher.EffectiveFrom.Value, null);

        TempData["FlashMessage"] = "Leading teacher assignment created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("leadingTeacher", new { LeadingTeacherSubjectId });
    }

    public async Task<IActionResult> OnPostEndLeadingTeacherAsync(Guid assignmentId)
    {
        await leadingTeacherAssignments.EndAsync(assignmentId, DateOnly.FromDateTime(DateTime.Today));
        TempData["FlashMessage"] = "Leading teacher assignment ended.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("leadingTeacher", new { LeadingTeacherSubjectId });
    }

    // ---- Periods ----

    public async Task<IActionResult> OnPostCreatePeriodAsync()
    {
        if (SchoolId == Guid.Empty || string.IsNullOrWhiteSpace(NewPeriod.Code))
        {
            TempData["FlashMessage"] = "Select a school and provide a code.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("periods");
        }

        await periodAdmin.CreatePeriodAsync(SchoolId, NewPeriod.Code, NewPeriod.Name, NewPeriod.StartTime, NewPeriod.EndTime, NewPeriod.DisplayOrder);
        TempData["FlashMessage"] = "Period created.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("periods");
    }

    public async Task<IActionResult> OnPostSavePeriodEditAsync()
    {
        await periodAdmin.UpdatePeriodAsync(EditPeriodForm.Id, EditPeriodForm.Name, EditPeriodForm.StartTime, EditPeriodForm.EndTime, EditPeriodForm.DisplayOrder);
        TempData["FlashMessage"] = "Period updated.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("periods");
    }

    // ---- Timetable ----

    public async Task<IActionResult> OnPostScheduleTimetableEntryAsync()
    {
        if (TimetableClassId == Guid.Empty || NewTimetableEntry.PeriodId == Guid.Empty || NewTimetableEntry.TeachingAssignmentId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a period and a subject teaching assignment.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("timetable", new { TimetableClassId });
        }

        var candidateAssignments = await subjectTeachingAssignments.GetAssignmentsForClassAsync(TimetableClassId, AcademicYearId);
        var assignment = candidateAssignments.SingleOrDefault(a => a.Id == NewTimetableEntry.TeachingAssignmentId);
        if (assignment is null)
        {
            TempData["FlashMessage"] = "That teaching assignment could not be found for this class.";
            TempData["FlashSeverity"] = "danger";
            return BackToTab("timetable", new { TimetableClassId });
        }

        try
        {
            await timetableService.ScheduleAsync(
                SchoolId, TimetableClassId, assignment.SubjectId, NewTimetableEntry.TeachingAssignmentId, AcademicYearId,
                NewTimetableEntry.DayOfWeek, NewTimetableEntry.PeriodId);
            TempData["FlashMessage"] = "Timetable entry scheduled.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("timetable", new { TimetableClassId });
    }

    public async Task<IActionResult> OnPostRemoveTimetableEntryAsync(Guid timetableEntryId)
    {
        await timetableService.RemoveAsync(timetableEntryId);
        TempData["FlashMessage"] = "Timetable entry removed.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("timetable", new { TimetableClassId });
    }

    // ---- Input models ----

    public sealed class NewSubjectTeachingInput
    {
        public Guid StaffPersonId { get; set; }
        public Guid SubjectId { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
    }

    public sealed class NewSubstitutionInput
    {
        public Guid SubstituteStaffPersonId { get; set; }
        public DateOnly? SubstitutionDate { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class NewClassTeacherInput
    {
        public Guid StaffPersonId { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
    }

    public sealed class NewLeadingTeacherInput
    {
        public Guid StaffPersonId { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
    }

    public sealed class NewPeriodInput
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class EditPeriodInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class NewTimetableEntryInput
    {
        public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
        public Guid PeriodId { get; set; }
        public Guid TeachingAssignmentId { get; set; }
    }
}

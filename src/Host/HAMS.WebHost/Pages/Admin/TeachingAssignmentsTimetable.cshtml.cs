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
    ITimetableService timetableService) : PageModel
{
    // ---- Tab selection (which tab shows as active after a full-page reload) ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "subject";

    // ---- Page-level School -> Academic Year cascade (shared by all 4 tabs) ----
    [BindProperty(SupportsGet = true)]
    public Guid SchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid AcademicYearId { get; set; }

    public IReadOnlyList<School> Schools { get; private set; } = [];
    public IReadOnlyList<AcademicYear> AcademicYears { get; private set; } = [];
    public IReadOnlyList<Class> Classes { get; private set; } = [];
    public IReadOnlyList<Subject> Subjects { get; private set; } = [];
    public IReadOnlyList<StaffProfileSummary> Staff { get; private set; } = [];

    // ---- Subject Teaching tab ----
    // Deliberately separate from ClassTeacherClassId below - Subject Teaching and Class Teacher
    // each have their OWN Class dropdown. Sharing one property was a repeatedly-hit bug in the
    // original Blazor page: switching tabs made a class look already "selected" while that tab's
    // own dependent table/list was never (re)loaded for it.
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

    // ---- Timetable tab (whole-school calendar — replaced the old per-class Timetable tab and the
    // standalone Periods tab; Periods are now an internal detail ITimetableService.ScheduleAsync
    // finds-or-creates itself from whatever start/end time is scheduled) ----
    [BindProperty] public NewTimetableEntryInput NewTimetableEntry { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    // Every tab's data is loaded unconditionally on every request (not just the active tab) since
    // Bootstrap's tabs are just CSS show/hide - all tab content lives in one server-rendered
    // response, unlike MudTabs' lazy per-panel rendering. The JSON handlers below call this too, so
    // the same School/AcademicYear-scoped Staff/Subjects lists are always available for name
    // resolution without duplicating that loading logic.
    private async Task LoadAllAsync()
    {
        Schools = await orgAdmin.GetSchoolsAsync();
        Staff = await peopleAdmin.GetStaffProfilesAsync();

        if (SchoolId != Guid.Empty)
        {
            AcademicYears = await orgAdmin.GetAcademicYearsAsync(SchoolId);
            Subjects = await curriculumAdmin.GetSubjectsAsync(SchoolId);
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
    }

    public string StaffName(Guid personId) => Staff.SingleOrDefault(s => s.PersonId == personId)?.NameEn ?? personId.ToString();
    public string SubjectName(Guid subjectId) => Subjects.SingleOrDefault(s => s.Id == subjectId)?.Name ?? subjectId.ToString();

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

    // ---- Timetable (whole-school calendar) ----

    // JSON handler behind FullCalendar's event feed — every class at once, colored per class.
    // Recurring day-of-week events (no start/end date) since a TimetableEntry has no calendar-date
    // dimension: BCL DayOfWeek (Sunday=0..Saturday=6) is numerically identical to FullCalendar's own
    // daysOfWeek convention, so (int)dayOfWeek needs no translation.
    public async Task<JsonResult> OnGetWeekEventsAsync()
    {
        await LoadAllAsync();

        if (SchoolId == Guid.Empty || AcademicYearId == Guid.Empty)
        {
            return new JsonResult(Array.Empty<object>());
        }

        var entries = await timetableService.GetEntriesForSchoolAsync(SchoolId, AcademicYearId);
        var events = entries.Select(e => new
        {
            id = e.Id,
            title = $"{e.ClassName} — {e.SubjectName}",
            daysOfWeek = new[] { (int)e.DayOfWeek },
            startTime = e.StartTime.ToString("HH:mm:ss"),
            endTime = e.EndTime.ToString("HH:mm:ss"),
            backgroundColor = e.ColorHex,
            borderColor = e.ColorHex,
            extendedProps = new
            {
                timetableEntryId = e.Id,
                classId = e.ClassId,
                className = e.ClassName,
                colorHex = e.ColorHex,
                subjectName = e.SubjectName,
                teacherName = StaffName(e.StaffPersonId),
                dayOfWeek = (int)e.DayOfWeek,
                startTime = e.StartTime.ToString("HH:mm"),
                endTime = e.EndTime.ToString("HH:mm"),
            },
        });
        return new JsonResult(events);
    }

    // JSON handler behind the create-modal's Class -> Subject/Teacher cascade (hams-site.js's
    // data-hams-cascade-* helper) — mirrors the existing "Subject — Staff" display convention every
    // other tab's teaching-assignment dropdown already uses.
    public async Task<JsonResult> OnGetClassAssignmentsAsync(Guid classId)
    {
        await LoadAllAsync();

        if (classId == Guid.Empty || AcademicYearId == Guid.Empty)
        {
            return new JsonResult(Array.Empty<object>());
        }

        var assignments = await subjectTeachingAssignments.GetAssignmentsForClassAsync(classId, AcademicYearId);
        var options = assignments
            .Where(a => a.EffectiveTo is null)
            .Select(a => new { value = a.Id, text = $"{SubjectName(a.SubjectId)} — {StaffName(a.StaffPersonId)}" });
        return new JsonResult(options);
    }

    // JSON handler for the create-modal's working-day gate (client-side hint only — ScheduleAsync
    // itself is the actual enforcement).
    public async Task<JsonResult> OnGetWorkingDaysAsync()
    {
        if (SchoolId == Guid.Empty)
        {
            return new JsonResult(Array.Empty<int>());
        }

        var workingDays = await orgAdmin.GetWorkingDaysAsync(SchoolId);
        return new JsonResult(workingDays.Select(d => (int)d));
    }

    public async Task<IActionResult> OnPostScheduleEntryAsync()
    {
        if (NewTimetableEntry.ClassId == Guid.Empty || NewTimetableEntry.TeachingAssignmentId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a class and a subject.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("timetable");
        }

        var candidateAssignments = await subjectTeachingAssignments.GetAssignmentsForClassAsync(NewTimetableEntry.ClassId, AcademicYearId);
        var assignment = candidateAssignments.SingleOrDefault(a => a.Id == NewTimetableEntry.TeachingAssignmentId);
        if (assignment is null)
        {
            TempData["FlashMessage"] = "That teaching assignment could not be found for this class.";
            TempData["FlashSeverity"] = "danger";
            return BackToTab("timetable");
        }

        try
        {
            await timetableService.ScheduleAsync(
                SchoolId, NewTimetableEntry.ClassId, assignment.SubjectId, NewTimetableEntry.TeachingAssignmentId, AcademicYearId,
                NewTimetableEntry.DayOfWeek, NewTimetableEntry.StartTime, NewTimetableEntry.EndTime);
            TempData["FlashMessage"] = "Timetable entry scheduled.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("timetable");
    }

    public async Task<IActionResult> OnPostRemoveEntryAsync(Guid timetableEntryId)
    {
        await timetableService.RemoveAsync(timetableEntryId);
        TempData["FlashMessage"] = "Timetable entry removed.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("timetable");
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

    public sealed class NewTimetableEntryInput
    {
        public Guid ClassId { get; set; }
        public Guid TeachingAssignmentId { get; set; }
        public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}

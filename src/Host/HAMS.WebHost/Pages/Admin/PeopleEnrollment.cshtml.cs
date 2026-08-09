using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.WebHost.Components.Account;
using HAMS.WebHost.Pages.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class PeopleEnrollmentModel(
    IPeopleAdminService peopleAdmin,
    IStudentEnrollmentService enrollmentService,
    IGuardianRelationshipService guardianRelationshipService,
    IOrgAdminService orgAdmin) : PageModel
{
    // ---- Tab selection ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "atolls";

    // ---- Atolls & Islands tab ----
    [BindProperty(SupportsGet = true)]
    public Guid AtollsSelectedAtollId { get; set; }

    public IReadOnlyList<Island> Islands { get; private set; } = [];

    // ---- Students tab (each piece of "which X is selected" state is its own property, never
    // shared with Staff/Guardians below - this exact page's Blazor history is why: MudTabs only
    // ever rendered one tab's markup at a time, so one shared "_editingPersonId"-style field was
    // safe there, but Bootstrap tabs render every pane's markup in the same response regardless of
    // which is visible, so a field shared across tabs would make an edit/selection started on one
    // tab bleed into another tab's identically-shaped section).
    [BindProperty(SupportsGet = true)]
    public Guid? StudentsEditPersonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid StudentsSelectedPersonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid StudentsEnrollSchoolId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid StudentsEnrollAcademicYearId { get; set; }

    public IReadOnlyList<StudentEnrollment> Enrollments { get; private set; } = [];
    public IReadOnlyList<GuardianStudentRelationship> GuardianRelationships { get; private set; } = [];
    public IReadOnlyList<AcademicYear> YearsForEnroll { get; private set; } = [];
    public IReadOnlyList<Grade> GradesForEnroll { get; private set; } = [];
    public IReadOnlyList<Class> ClassesForEnroll { get; private set; } = [];

    // ---- Staff tab ----
    [BindProperty(SupportsGet = true)]
    public Guid? StaffEditPersonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid StaffSelectedPersonId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid StaffSelectedProfileId { get; set; }

    public IReadOnlyList<StaffQualification> Qualifications { get; private set; } = [];

    // ---- Guardians tab ----
    [BindProperty(SupportsGet = true)]
    public Guid? GuardiansEditPersonId { get; set; }

    // ---- Cross-tab data (loaded unconditionally on every request, same reasoning as
    // OrgStructureModel.LoadAllAsync - all 4 tab panes render in one response) ----
    public IReadOnlyList<Atoll> Atolls { get; private set; } = [];
    public IReadOnlyList<StudentProfileSummary> Students { get; private set; } = [];
    public IReadOnlyList<StaffProfileSummary> Staff { get; private set; } = [];
    public IReadOnlyList<GuardianProfileSummary> Guardians { get; private set; } = [];
    public IReadOnlyList<EmploymentStatus> EmploymentStatuses { get; private set; } = [];
    public IReadOnlyList<RelationshipType> RelationshipTypes { get; private set; } = [];
    public IReadOnlyList<School> Schools { get; private set; } = [];

    // ---- Form inputs (POST bodies) ----
    [BindProperty] public NewAtollInput NewAtoll { get; set; } = new();
    [BindProperty] public NewIslandInput NewIsland { get; set; } = new();

    [BindProperty] public PersonFieldsInput NewStudentPerson { get; set; } = new();
    [BindProperty] public string NewStudentAdmissionNumber { get; set; } = "";
    [BindProperty] public DateOnly NewStudentAdmissionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public PersonFieldsInput EditStudentPersonForm { get; set; } = new();

    [BindProperty] public Guid EnrollGradeId { get; set; }
    [BindProperty] public Guid EnrollClassId { get; set; }
    [BindProperty] public DateOnly EnrollEffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [BindProperty] public NewRelationshipInput NewRelationship { get; set; } = new();

    [BindProperty] public PersonFieldsInput NewStaffPerson { get; set; } = new();
    [BindProperty] public string NewStaffEmployeeNumber { get; set; } = "";
    [BindProperty] public DateOnly NewStaffHireDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public Guid NewStaffEmploymentStatusId { get; set; }
    [BindProperty] public PersonFieldsInput EditStaffPersonForm { get; set; } = new();

    [BindProperty] public NewQualificationInput NewQualification { get; set; } = new();

    [BindProperty] public PersonFieldsInput NewGuardianPerson { get; set; } = new();
    [BindProperty] public PersonFieldsInput EditGuardianPersonForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    /// <summary>
    /// The hams-site.js cascading-select AJAX handler for every Atoll -&gt; Island pair on this page
    /// (all 6 PersonFieldsInput instances point at the same handler - it's a pure function of
    /// atollId, no other state needed). Unlike the Enroll section's School -&gt; Academic Year
    /// cascade below (a plain full-page GET reload, fine there since nothing else on that row is
    /// mid-typed), a full reload here would blow away every other field already filled in on the
    /// same Create/Edit-person form.
    /// </summary>
    public async Task<JsonResult> OnGetIslandsAsync(Guid atollId)
    {
        if (atollId == Guid.Empty)
        {
            return new JsonResult(Array.Empty<object>());
        }

        var islands = await peopleAdmin.GetIslandsAsync(atollId);
        return new JsonResult(islands.Select(i => new { value = i.Id, text = i.NameEn }));
    }

    private async Task LoadAllAsync()
    {
        Atolls = await peopleAdmin.GetAtollsAsync();
        Students = await peopleAdmin.GetStudentProfilesAsync();
        Staff = await peopleAdmin.GetStaffProfilesAsync();
        Guardians = await peopleAdmin.GetGuardianProfilesAsync();
        EmploymentStatuses = await peopleAdmin.GetEmploymentStatusesAsync();
        RelationshipTypes = await peopleAdmin.GetRelationshipTypesAsync();
        Schools = await orgAdmin.GetSchoolsAsync();

        foreach (var form in new[] { NewStudentPerson, EditStudentPersonForm, NewStaffPerson, EditStaffPersonForm, NewGuardianPerson, EditGuardianPersonForm })
        {
            form.Atolls = Atolls;
        }

        if (AtollsSelectedAtollId != Guid.Empty)
        {
            Islands = await peopleAdmin.GetIslandsAsync(AtollsSelectedAtollId);
        }

        if (StudentsEditPersonId is { } studentEditPersonId)
        {
            await LoadPersonEditFormAsync(studentEditPersonId, EditStudentPersonForm);
        }

        if (StudentsSelectedPersonId != Guid.Empty)
        {
            Enrollments = await peopleAdmin.GetEnrollmentsForStudentAsync(StudentsSelectedPersonId);
            GuardianRelationships = await peopleAdmin.GetGuardianRelationshipsForStudentAsync(StudentsSelectedPersonId);
        }
        if (StudentsEnrollSchoolId != Guid.Empty)
        {
            YearsForEnroll = await orgAdmin.GetAcademicYearsAsync(StudentsEnrollSchoolId);
            GradesForEnroll = await orgAdmin.GetGradesAsync(StudentsEnrollSchoolId);
        }
        if (StudentsEnrollAcademicYearId != Guid.Empty)
        {
            ClassesForEnroll = await orgAdmin.GetClassesAsync(StudentsEnrollAcademicYearId);
        }

        if (StaffEditPersonId is { } staffEditPersonId)
        {
            await LoadPersonEditFormAsync(staffEditPersonId, EditStaffPersonForm);
        }
        if (StaffSelectedProfileId != Guid.Empty)
        {
            Qualifications = await peopleAdmin.GetStaffQualificationsAsync(StaffSelectedProfileId);
        }

        if (GuardiansEditPersonId is { } guardiansEditPersonId)
        {
            await LoadPersonEditFormAsync(guardiansEditPersonId, EditGuardianPersonForm);
        }
    }

    // Mirrors the old Blazor StartPersonEditAsync: resolves the island's atoll so the Atoll/Island
    // cascade can pre-populate correctly, since _islands (here, form.Islands) otherwise only ever
    // gets populated by the user manually re-touching the Atoll dropdown.
    private async Task LoadPersonEditFormAsync(Guid personId, PersonFieldsInput form)
    {
        var person = await peopleAdmin.GetPersonAsync(personId);
        if (person is null)
        {
            return;
        }

        var island = await peopleAdmin.GetIslandAsync(person.Address.IslandId);
        var atollId = island?.AtollId ?? Guid.Empty;

        form.NameEn = person.NameEn;
        form.NameDv = person.NameDv;
        form.DateOfBirth = person.DateOfBirth;
        form.AtollId = atollId;
        form.IslandId = person.Address.IslandId;
        form.RoadEn = person.Address.RoadEn;
        form.RoadDv = person.Address.RoadDv;
        form.HouseNameEn = person.Address.HouseNameEn;
        form.HouseNameDv = person.Address.HouseNameDv;
        form.BuildingEn = person.Address.BuildingEn;
        form.BuildingDv = person.Address.BuildingDv;
        form.Floor = person.Address.Floor;
        form.Apartment = person.Address.Apartment;
        form.PhoneNumber = person.PhoneNumber;
        form.Email = person.Email;
        form.Islands = atollId == Guid.Empty ? [] : await peopleAdmin.GetIslandsAsync(atollId);
    }

    private RedirectToPageResult BackToAtolls() =>
        RedirectToPage(new { Tab = "atolls", AtollsSelectedAtollId });

    private RedirectToPageResult BackToStudents(Guid? editPersonId = null) =>
        RedirectToPage(new
        {
            Tab = "students",
            StudentsEditPersonId = editPersonId,
            StudentsSelectedPersonId,
            StudentsEnrollSchoolId,
            StudentsEnrollAcademicYearId,
        });

    private RedirectToPageResult BackToStaff(Guid? editPersonId = null) =>
        RedirectToPage(new
        {
            Tab = "staff",
            StaffEditPersonId = editPersonId,
            StaffSelectedPersonId,
            StaffSelectedProfileId,
        });

    private RedirectToPageResult BackToGuardians(Guid? editPersonId = null) =>
        RedirectToPage(new { Tab = "guardians", GuardiansEditPersonId = editPersonId });

    // ---- Atolls & Islands ----

    public async Task<IActionResult> OnPostCreateAtollAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAtoll.Code) || string.IsNullOrWhiteSpace(NewAtoll.NameEn))
        {
            TempData["FlashMessage"] = "Code and name (English) are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToAtolls();
        }

        await peopleAdmin.CreateAtollAsync(NewAtoll.Code, NewAtoll.NameEn, NewAtoll.NameDv, NewAtoll.DisplayOrder);
        TempData["FlashMessage"] = "Atoll created.";
        TempData["FlashSeverity"] = "success";
        return BackToAtolls();
    }

    public async Task<IActionResult> OnPostCreateIslandAsync()
    {
        if (AtollsSelectedAtollId == Guid.Empty || string.IsNullOrWhiteSpace(NewIsland.Code) || string.IsNullOrWhiteSpace(NewIsland.NameEn))
        {
            TempData["FlashMessage"] = "Select an atoll, and provide a code and name (English).";
            TempData["FlashSeverity"] = "warning";
            return BackToAtolls();
        }

        await peopleAdmin.CreateIslandAsync(AtollsSelectedAtollId, NewIsland.Code, NewIsland.NameEn, NewIsland.NameDv, NewIsland.DisplayOrder);
        TempData["FlashMessage"] = "Island created.";
        TempData["FlashSeverity"] = "success";
        return BackToAtolls();
    }

    // ---- Students ----

    public async Task<IActionResult> OnPostCreateStudentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStudentPerson.NameEn) || string.IsNullOrWhiteSpace(NewStudentPerson.NameDv) || string.IsNullOrWhiteSpace(NewStudentAdmissionNumber))
        {
            TempData["FlashMessage"] = "Name (English/Dhivehi) and admission number are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToStudents();
        }

        var personId = await peopleAdmin.CreatePersonAsync(
            NewStudentPerson.NameEn, NewStudentPerson.NameDv, NewStudentPerson.DateOfBirth,
            NewStudentPerson.ToAddress(), NewStudentPerson.PhoneNumber, NewStudentPerson.Email);
        await peopleAdmin.CreateStudentProfileAsync(personId, NewStudentAdmissionNumber, NewStudentAdmissionDate);

        TempData["FlashMessage"] = "Student created.";
        TempData["FlashSeverity"] = "success";
        return BackToStudents();
    }

    public async Task<IActionResult> OnPostSaveStudentPersonEditAsync()
    {
        if (StudentsEditPersonId is not { } personId)
        {
            return BackToStudents();
        }

        await peopleAdmin.UpdatePersonAsync(
            personId, EditStudentPersonForm.NameEn, EditStudentPersonForm.NameDv, EditStudentPersonForm.DateOfBirth,
            EditStudentPersonForm.ToAddress(), EditStudentPersonForm.PhoneNumber, EditStudentPersonForm.Email);

        TempData["FlashMessage"] = "Person updated.";
        TempData["FlashSeverity"] = "success";
        return BackToStudents();
    }

    public async Task<IActionResult> OnPostEnrollAsync()
    {
        if (StudentsSelectedPersonId == Guid.Empty || StudentsEnrollAcademicYearId == Guid.Empty || EnrollGradeId == Guid.Empty || EnrollClassId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select an academic year, grade, class and effective date.";
            TempData["FlashSeverity"] = "warning";
            return BackToStudents();
        }

        try
        {
            await enrollmentService.EnrollAsync(StudentsSelectedPersonId, EnrollGradeId, EnrollClassId, StudentsEnrollAcademicYearId, EnrollEffectiveFrom);
            TempData["FlashMessage"] = "Student enrolled.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToStudents();
    }

    public async Task<IActionResult> OnPostEndEnrollmentAsync(Guid enrollmentId)
    {
        try
        {
            await enrollmentService.EndEnrollmentAsync(enrollmentId, DateOnly.FromDateTime(DateTime.Today));
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToStudents();
    }

    public async Task<IActionResult> OnPostEstablishRelationshipAsync()
    {
        if (StudentsSelectedPersonId == Guid.Empty || NewRelationship.GuardianPersonId == Guid.Empty || NewRelationship.RelationshipTypeId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Select a guardian, relationship type and effective date.";
            TempData["FlashSeverity"] = "warning";
            return BackToStudents();
        }

        var request = new EstablishGuardianRelationshipRequest(
            NewRelationship.GuardianPersonId, StudentsSelectedPersonId, NewRelationship.RelationshipTypeId, NewRelationship.HasLegalAuthority,
            NewRelationship.CanViewAcademic, NewRelationship.CanViewAttendance, NewRelationship.CanViewBehaviour, NewRelationship.CanViewIntervention,
            NewRelationship.CanReceiveNotifications, null, NewRelationship.EffectiveFrom);

        await guardianRelationshipService.EstablishAsync(request);

        TempData["FlashMessage"] = "Guardian relationship established.";
        TempData["FlashSeverity"] = "success";
        return BackToStudents();
    }

    public async Task<IActionResult> OnPostVerifyRelationshipAsync(Guid relationshipId)
    {
        try
        {
            await guardianRelationshipService.VerifyAsync(relationshipId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToStudents();
    }

    public async Task<IActionResult> OnPostRejectRelationshipAsync(Guid relationshipId)
    {
        try
        {
            await guardianRelationshipService.RejectAsync(relationshipId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToStudents();
    }

    public async Task<IActionResult> OnPostCloseRelationshipAsync(Guid relationshipId)
    {
        await guardianRelationshipService.CloseAsync(relationshipId, DateOnly.FromDateTime(DateTime.Today));
        return BackToStudents();
    }

    // ---- Staff ----

    public async Task<IActionResult> OnPostCreateStaffAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStaffPerson.NameEn) || string.IsNullOrWhiteSpace(NewStaffPerson.NameDv) ||
            string.IsNullOrWhiteSpace(NewStaffEmployeeNumber) || NewStaffEmploymentStatusId == Guid.Empty)
        {
            TempData["FlashMessage"] = "Name (English/Dhivehi), employee number and employment status are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToStaff();
        }

        // EmploymentStatuses (the [BindProperty(SupportsGet=true)]-less cross-tab list) is only ever
        // populated by LoadAllAsync on a GET - a POST handler runs on its own, fresh request, so the
        // employment status code needed by CreateStaffProfileAsync has to be resolved here directly.
        var employmentStatuses = await peopleAdmin.GetEmploymentStatusesAsync();
        var employmentStatus = employmentStatuses.SingleOrDefault(s => s.Id == NewStaffEmploymentStatusId);
        if (employmentStatus is null)
        {
            TempData["FlashMessage"] = "Select an employment status.";
            TempData["FlashSeverity"] = "warning";
            return BackToStaff();
        }

        var personId = await peopleAdmin.CreatePersonAsync(
            NewStaffPerson.NameEn, NewStaffPerson.NameDv, NewStaffPerson.DateOfBirth,
            NewStaffPerson.ToAddress(), NewStaffPerson.PhoneNumber, NewStaffPerson.Email);

        try
        {
            await peopleAdmin.CreateStaffProfileAsync(personId, NewStaffEmployeeNumber, NewStaffHireDate, employmentStatus.Code);
            TempData["FlashMessage"] = "Staff member created.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToStaff();
    }

    public async Task<IActionResult> OnPostSaveStaffPersonEditAsync()
    {
        if (StaffEditPersonId is not { } personId)
        {
            return BackToStaff();
        }

        await peopleAdmin.UpdatePersonAsync(
            personId, EditStaffPersonForm.NameEn, EditStaffPersonForm.NameDv, EditStaffPersonForm.DateOfBirth,
            EditStaffPersonForm.ToAddress(), EditStaffPersonForm.PhoneNumber, EditStaffPersonForm.Email);

        TempData["FlashMessage"] = "Person updated.";
        TempData["FlashSeverity"] = "success";
        return BackToStaff();
    }

    public async Task<IActionResult> OnPostAddQualificationAsync()
    {
        if (StaffSelectedProfileId == Guid.Empty || string.IsNullOrWhiteSpace(NewQualification.Title))
        {
            TempData["FlashMessage"] = "Select a staff member and provide a qualification title.";
            TempData["FlashSeverity"] = "warning";
            return BackToStaff();
        }

        await peopleAdmin.AddStaffQualificationAsync(StaffSelectedProfileId, NewQualification.Title, NewQualification.Institution, NewQualification.YearAwarded);
        TempData["FlashMessage"] = "Qualification added.";
        TempData["FlashSeverity"] = "success";
        return BackToStaff();
    }

    // ---- Guardians ----

    public async Task<IActionResult> OnPostCreateGuardianAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGuardianPerson.NameEn) || string.IsNullOrWhiteSpace(NewGuardianPerson.NameDv))
        {
            TempData["FlashMessage"] = "Name (English/Dhivehi) is required.";
            TempData["FlashSeverity"] = "warning";
            return BackToGuardians();
        }

        var personId = await peopleAdmin.CreatePersonAsync(
            NewGuardianPerson.NameEn, NewGuardianPerson.NameDv, NewGuardianPerson.DateOfBirth,
            NewGuardianPerson.ToAddress(), NewGuardianPerson.PhoneNumber, NewGuardianPerson.Email);
        await peopleAdmin.CreateGuardianProfileAsync(personId);

        TempData["FlashMessage"] = "Guardian created.";
        TempData["FlashSeverity"] = "success";
        return BackToGuardians();
    }

    public async Task<IActionResult> OnPostSaveGuardianPersonEditAsync()
    {
        if (GuardiansEditPersonId is not { } personId)
        {
            return BackToGuardians();
        }

        await peopleAdmin.UpdatePersonAsync(
            personId, EditGuardianPersonForm.NameEn, EditGuardianPersonForm.NameDv, EditGuardianPersonForm.DateOfBirth,
            EditGuardianPersonForm.ToAddress(), EditGuardianPersonForm.PhoneNumber, EditGuardianPersonForm.Email);

        TempData["FlashMessage"] = "Person updated.";
        TempData["FlashSeverity"] = "success";
        return BackToGuardians();
    }

    // ---- Input models ----

    public sealed class NewAtollInput
    {
        public string Code { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string? NameDv { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class NewIslandInput
    {
        public string Code { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string? NameDv { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class NewRelationshipInput
    {
        public Guid GuardianPersonId { get; set; }
        public Guid RelationshipTypeId { get; set; }
        public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public bool HasLegalAuthority { get; set; }
        public bool CanViewAcademic { get; set; }
        public bool CanViewAttendance { get; set; }
        public bool CanViewBehaviour { get; set; }
        public bool CanViewIntervention { get; set; }
        public bool CanReceiveNotifications { get; set; }
    }

    public sealed class NewQualificationInput
    {
        public string Title { get; set; } = "";
        public string? Institution { get; set; }
        public int? YearAwarded { get; set; }
    }
}

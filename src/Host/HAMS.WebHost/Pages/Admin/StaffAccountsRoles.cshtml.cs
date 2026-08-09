using System.ComponentModel.DataAnnotations;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Domain;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class StaffAccountsRolesModel(
    IPeopleAdminService peopleAdmin,
    IStaffAccountService staffAccounts,
    IPersonRoleAssignmentService roleAssignments,
    IOrgAdminService orgAdmin) : PageModel
{
    // ---- Tab selection (which tab shows as active after a full-page reload) ----
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "accounts";

    // ---- Cross-tab data ----
    public IReadOnlyList<StaffProfileSummary> Staff { get; private set; } = [];
    public Dictionary<Guid, StaffAccountSummary> AccountsByPersonId { get; private set; } = [];
    public IReadOnlyList<Role> Roles { get; private set; } = [];
    public IReadOnlyList<School> Schools { get; private set; } = [];

    // ---- Staff Accounts tab ----
    [BindProperty(SupportsGet = true)]
    public Guid AccountsPersonId { get; set; }

    // ---- Roles tab ----
    [BindProperty(SupportsGet = true)]
    public Guid RolesPersonId { get; set; }

    public IReadOnlyList<PersonRoleAssignment> Assignments { get; private set; } = [];

    // ---- Form inputs (POST bodies) ----
    [BindProperty] public NewAccountInput NewAccount { get; set; } = new();
    [BindProperty] public string ResetPassword { get; set; } = "";
    [BindProperty] public NewRoleAssignmentInput NewRoleAssignment { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAllAsync();
    }

    // Every tab's data is loaded unconditionally on every request (not just the active tab) since
    // Bootstrap's tabs are just CSS show/hide - all tab content lives in one server-rendered
    // response, unlike MudTabs' lazy per-panel rendering.
    private async Task LoadAllAsync()
    {
        Staff = await peopleAdmin.GetStaffProfilesAsync();

        var accounts = await staffAccounts.GetAccountsAsync();
        AccountsByPersonId = accounts.ToDictionary(a => a.PersonId);

        Roles = await roleAssignments.GetRolesAsync();
        Schools = await orgAdmin.GetSchoolsAsync();

        if (RolesPersonId != Guid.Empty)
        {
            Assignments = await roleAssignments.GetAssignmentsForPersonAsync(RolesPersonId);
        }
    }

    private RedirectToPageResult BackToTab(string tab, object? extraRouteValues = null)
    {
        var routeValues = new RouteValueDictionary(extraRouteValues) { ["tab"] = tab };
        return RedirectToPage(routeValues);
    }

    // ---- Staff Accounts ----

    public async Task<IActionResult> OnPostCreateAccountAsync()
    {
        if (AccountsPersonId == Guid.Empty || string.IsNullOrWhiteSpace(NewAccount.UserName) || string.IsNullOrWhiteSpace(NewAccount.Password))
        {
            TempData["FlashMessage"] = "Username and initial password are required.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("accounts", new { AccountsPersonId });
        }

        try
        {
            await staffAccounts.CreateAccountAsync(AccountsPersonId, NewAccount.UserName, NewAccount.Email, NewAccount.Password);
            TempData["FlashMessage"] = "Staff account created.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("accounts", new { AccountsPersonId });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        var account = await staffAccounts.GetAccountByPersonIdAsync(AccountsPersonId);
        if (account is null)
        {
            TempData["FlashMessage"] = "No login account exists for this staff member.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("accounts", new { AccountsPersonId });
        }

        try
        {
            await staffAccounts.ResetPasswordAsync(account.UserId, ResetPassword);
            TempData["FlashMessage"] = "Password reset.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("accounts", new { AccountsPersonId });
    }

    public async Task<IActionResult> OnPostSetAccountStatusAsync(AccountStatus status)
    {
        var account = await staffAccounts.GetAccountByPersonIdAsync(AccountsPersonId);
        if (account is not null)
        {
            await staffAccounts.SetAccountStatusAsync(account.UserId, status);
            TempData["FlashMessage"] = $"Account status set to {status}.";
            TempData["FlashSeverity"] = "success";
        }

        return BackToTab("accounts", new { AccountsPersonId });
    }

    // ---- Roles ----

    public async Task<IActionResult> OnPostAssignRoleAsync()
    {
        if (RolesPersonId == Guid.Empty || NewRoleAssignment.RoleId == Guid.Empty || NewRoleAssignment.EffectiveFrom is null)
        {
            TempData["FlashMessage"] = "Select a staff member, a role, and an effective date.";
            TempData["FlashSeverity"] = "warning";
            return BackToTab("roles", new { RolesPersonId });
        }

        var role = (await roleAssignments.GetRolesAsync()).SingleOrDefault(r => r.Id == NewRoleAssignment.RoleId);
        if (role is null)
        {
            TempData["FlashMessage"] = "Selected role was not found.";
            TempData["FlashSeverity"] = "danger";
            return BackToTab("roles", new { RolesPersonId });
        }

        var schoolId = NewRoleAssignment.SchoolId == Guid.Empty ? (Guid?)null : NewRoleAssignment.SchoolId;

        try
        {
            await roleAssignments.AssignRoleAsync(RolesPersonId, role.Code, schoolId, NewRoleAssignment.EffectiveFrom.Value, null);
            TempData["FlashMessage"] = "Role assigned.";
            TempData["FlashSeverity"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FlashMessage"] = ex.Message;
            TempData["FlashSeverity"] = "danger";
        }

        return BackToTab("roles", new { RolesPersonId });
    }

    public async Task<IActionResult> OnPostRevokeRoleAsync(Guid assignmentId)
    {
        await roleAssignments.RevokeRoleAsync(assignmentId, DateOnly.FromDateTime(DateTime.Today));
        TempData["FlashMessage"] = "Role revoked.";
        TempData["FlashSeverity"] = "success";
        return BackToTab("roles", new { RolesPersonId });
    }

    // ---- Input models ----

    public sealed class NewAccountInput
    {
        [Required] public string UserName { get; set; } = "";
        public string? Email { get; set; }
        [Required] public string Password { get; set; } = "";
    }

    public sealed class NewRoleAssignmentInput
    {
        public Guid RoleId { get; set; }
        public Guid SchoolId { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
    }
}

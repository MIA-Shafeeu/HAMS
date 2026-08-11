using System.ComponentModel.DataAnnotations;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Account;

public sealed class LoginMfaModel(IStaffAuthenticationService staffAuth, IRoleMembershipQuery roleMembershipQuery, IClock clock) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public MfaInput Mfa { get; set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await staffAuth.VerifyMfaAsync(
            new StaffMfaVerifyRequest(Token, Mfa.Code, "HAMS Web"),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!result.Succeeded)
        {
            Error = result.Error ?? "Invalid authentication code.";
            return Page();
        }

        var principal = await StaffCookieSignIn.SignInAsync(HttpContext, result);

        // "/dashboard" is System/School-Administrator-only (SystemOrSchoolAdminPolicy) - see
        // Login.cshtml.cs's ResolveRedirectTargetAsync for why every other role needs a different
        // default (their own nav menu never even shows them a Dashboard link).
        var safeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.StartsWith('/') && !ReturnUrl.StartsWith("//") ? ReturnUrl : null;
        var personId = Guid.TryParse(principal.FindFirst(HamsClaimTypes.PersonId)?.Value, out var pid) ? pid : Guid.Empty;
        var target = safeReturnUrl ?? (await roleMembershipQuery.IsSystemOrSchoolAdminAsync(personId, clock.TodayUtc) ? "/dashboard" : "/attendance");
        return Redirect(target);
    }

    public sealed class MfaInput
    {
        [Required(ErrorMessage = "Enter the 6-digit code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Enter all 6 digits.")]
        public string Code { get; set; } = "";
    }
}

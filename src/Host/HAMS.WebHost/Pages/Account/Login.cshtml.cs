using System.ComponentModel.DataAnnotations;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Application.Jwt;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Account;

public sealed class LoginModel(IStaffAuthenticationService staffAuth, IRoleMembershipQuery roleMembershipQuery, IClock clock) : PageModel
{
    [BindProperty]
    public CredentialsInput Credentials { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var personId = Guid.TryParse(User.FindFirst(HamsClaimTypes.PersonId)?.Value, out var id) ? id : Guid.Empty;
            return Redirect(await ResolveRedirectTargetAsync(personId));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await staffAuth.LoginAsync(
            new StaffLoginRequest(Credentials.UsernameOrEmail, Credentials.Password, "HAMS Web"),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (result.MfaRequired && result.MfaToken is not null)
        {
            // MFA isn't verified yet, so there's no signed-in identity to resolve a role-based
            // default from - just carry the explicit ReturnUrl (if any) through unchanged. LoginMfa
            // resolves the actual default landing page itself once sign-in actually happens.
            return RedirectToPage("./LoginMfa", new { token = result.MfaToken, returnUrl = SafeReturnUrlOrNull() });
        }

        if (!result.Succeeded)
        {
            Error = result.Error ?? "Invalid username or password.";
            return Page();
        }

        var principal = await StaffCookieSignIn.SignInAsync(HttpContext, result);
        var signedInPersonId = Guid.TryParse(principal.FindFirst(HamsClaimTypes.PersonId)?.Value, out var pid) ? pid : Guid.Empty;
        return Redirect(await ResolveRedirectTargetAsync(signedInPersonId));
    }

    private string? SafeReturnUrlOrNull()
        => !string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.StartsWith('/') && !ReturnUrl.StartsWith("//")
            ? ReturnUrl
            : null;

    // "/dashboard" is System/School-Administrator-only (SystemOrSchoolAdminPolicy) - defaulting
    // every login there regardless of role sent every other staff member straight into an access-
    // denied-then-back-to-login redirect loop. Everyone else's default landing page is "/attendance",
    // the first link every staff member's nav menu actually shows them (see NavMenuViewComponent).
    private async Task<string> ResolveRedirectTargetAsync(Guid personId)
        => SafeReturnUrlOrNull()
            ?? (await roleMembershipQuery.IsSystemOrSchoolAdminAsync(personId, clock.TodayUtc) ? "/dashboard" : "/attendance");

    public sealed class CredentialsInput
    {
        [Required(ErrorMessage = "Enter your username or email.")]
        public string UsernameOrEmail { get; set; } = "";

        [Required(ErrorMessage = "Enter your password.")]
        public string Password { get; set; } = "";
    }
}

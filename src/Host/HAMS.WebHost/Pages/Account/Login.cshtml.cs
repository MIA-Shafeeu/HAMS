using System.ComponentModel.DataAnnotations;
using HAMS.IdentityAccess.Application.Auth;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Account;

public sealed class LoginModel(IStaffAuthenticationService staffAuth) : PageModel
{
    [BindProperty]
    public CredentialsInput Credentials { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(SafeReturnUrl());
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
            return RedirectToPage("./LoginMfa", new { token = result.MfaToken, returnUrl = SafeReturnUrl() });
        }

        if (!result.Succeeded)
        {
            Error = result.Error ?? "Invalid username or password.";
            return Page();
        }

        await StaffCookieSignIn.SignInAsync(HttpContext, result);
        return Redirect(SafeReturnUrl());
    }

    private string SafeReturnUrl()
        => !string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.StartsWith('/') && !ReturnUrl.StartsWith("//")
            ? ReturnUrl
            : "/dashboard";

    public sealed class CredentialsInput
    {
        [Required(ErrorMessage = "Enter your username or email.")]
        public string UsernameOrEmail { get; set; } = "";

        [Required(ErrorMessage = "Enter your password.")]
        public string Password { get; set; } = "";
    }
}

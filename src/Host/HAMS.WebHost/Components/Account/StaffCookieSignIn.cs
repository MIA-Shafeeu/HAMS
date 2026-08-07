using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HAMS.IdentityAccess.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HAMS.WebHost.Components.Account;

/// <summary>
/// Bridges a successful <see cref="AuthResult"/> — the exact same token-issuing path every JWT API
/// client uses — into the Cookie identity Blazor Server's admin UI needs (Phase 12). Copies the
/// JWT's own claims verbatim rather than re-deriving them, so the cookie principal is claim-for-claim
/// identical to what a bearer-token request would see (same <c>HamsClaimTypes</c>, same
/// <see cref="ClaimTypes.NameIdentifier"/>) — <c>ICurrentUser</c> keeps working unchanged for any
/// code that runs during the request that establishes a circuit. WebHost-only glue: IdentityAccess
/// itself stays completely unaware that cookies exist.
/// </summary>
internal static class StaffCookieSignIn
{
    public static async Task SignInAsync(HttpContext httpContext, AuthResult authResult)
    {
        if (!authResult.Succeeded || authResult.AccessToken is null)
        {
            throw new InvalidOperationException("Cannot sign in from a failed or MFA-pending auth result.");
        }

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(authResult.AccessToken);
        var identity = new ClaimsIdentity(jwt.Claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.NameIdentifier, null);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties { IsPersistent = true };
        if (authResult.RefreshToken is not null)
        {
            // Kept only so a real Logout can revoke the underlying session server-side (not just
            // clear the cookie) — AuthenticationProperties.Items lives inside the encrypted cookie
            // ticket, never exposed to script or sent as its own visible claim.
            properties.Items["refresh_token"] = authResult.RefreshToken;
        }

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}

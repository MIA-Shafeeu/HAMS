using HAMS.IdentityAccess.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HAMS.WebHost.Components.Account;

/// <summary>Sign-out is a plain minimal-API POST, not a Razor component — there's no page to render, just a side effect.</summary>
internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/logout", async (HttpContext context, IStaffAuthenticationService staffAuth) =>
        {
            var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (authenticateResult.Properties?.Items.TryGetValue("refresh_token", out var refreshToken) == true && refreshToken is not null)
            {
                await staffAuth.LogoutAsync(refreshToken);
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.LocalRedirect("/account/login");
        });

        return endpoints;
    }
}

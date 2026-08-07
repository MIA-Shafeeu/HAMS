using System.Security.Claims;
using HAMS.IdentityAccess.Application.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.IdentityAccess.Endpoints;

/// <summary>Staff login/session/MFA endpoints (build plan Phase 1 scope — guardian OTP and student auth are deferred).</summary>
internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/staff/login", async (StaffLoginRequest request, IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(request, RemoteIp(http), ct);
            return result.Succeeded || result.MfaRequired ? Results.Ok(result) : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/staff/login/mfa", async (StaffMfaVerifyRequest request, IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            var result = await auth.VerifyMfaAsync(request, RemoteIp(http), ct);
            return result.Succeeded ? Results.Ok(result) : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest request, IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(request, RemoteIp(http), ct);
            return result.Succeeded ? Results.Ok(result) : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/logout", async (RefreshRequest request, IStaffAuthenticationService auth, CancellationToken ct) =>
        {
            await auth.LogoutAsync(request.RefreshToken, ct);
            return Results.NoContent();
        }).AllowAnonymous();

        group.MapGet("/sessions", async (IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            if (GetUserId(http.User) is not { } userId)
            {
                return Results.Unauthorized();
            }

            var currentRefreshToken = http.Request.Headers["X-Refresh-Token"].FirstOrDefault();
            var sessions = await auth.ListSessionsAsync(userId, currentRefreshToken, ct);
            return Results.Ok(sessions);
        }).RequireAuthorization();

        group.MapDelete("/sessions/{sessionId:guid}", async (Guid sessionId, IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            if (GetUserId(http.User) is not { } userId)
            {
                return Results.Unauthorized();
            }

            await auth.RevokeSessionAsync(userId, sessionId, ct);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/mfa/setup", async (IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            if (GetUserId(http.User) is not { } userId)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(await auth.BeginMfaSetupAsync(userId, ct));
        }).RequireAuthorization();

        group.MapPost("/mfa/enable", async (MfaEnableRequest request, IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            if (GetUserId(http.User) is not { } userId)
            {
                return Results.Unauthorized();
            }

            var enabled = await auth.EnableMfaAsync(userId, request.Code, ct);
            return enabled ? Results.NoContent() : Results.BadRequest("Invalid authentication code.");
        }).RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordRequest request, IStaffAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            if (GetUserId(http.User) is not { } userId)
            {
                return Results.Unauthorized();
            }

            var changed = await auth.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
            return changed ? Results.NoContent() : Results.BadRequest("Could not change password.");
        }).RequireAuthorization();

        return endpoints;
    }

    private static string? RemoteIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();

    private static Guid? GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

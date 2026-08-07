using HAMS.IdentityAccess.Application.Auth;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.IdentityAccess.Endpoints;

public sealed record SetStudentPinRequest(string Pin);

/// <summary>Student ID+PIN login surface (build plan Phase 10 scope). Setting/resetting a PIN is a staff/admin action — logging in with one isn't.</summary>
internal static class StudentAuthEndpoints
{
    public static IEndpointRouteBuilder MapStudentAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth/student").WithTags("StudentAuth");

        group.MapPost("/login", async (StudentLoginRequest request, IStudentAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(request, RemoteIp(http), ct);
            return result.Succeeded ? Results.Ok(result) : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/{studentPersonId:guid}/set-pin", async (
            Guid studentPersonId, SetStudentPinRequest request, IStudentAuthenticationService auth,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await auth.SetPinAsync(studentPersonId, request.Pin, ct);
            return Results.NoContent();
        }).RequireAuthorization();

        return endpoints;
    }

    private static string? RemoteIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();
}

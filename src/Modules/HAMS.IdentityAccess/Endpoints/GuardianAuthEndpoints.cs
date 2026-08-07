using HAMS.IdentityAccess.Application.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.IdentityAccess.Endpoints;

public sealed record RequestGuardianOtpRequest(string PhoneNumber);
public sealed record VerifyGuardianOtpRequest(Guid ChallengeId, string Code, string? DeviceLabel);

/// <summary>Guardian OTP login surface (build plan Phase 10 scope). Session refresh/logout/listing reuse the existing generic <c>/api/v1/auth/*</c> endpoints — nothing about them is staff-specific.</summary>
internal static class GuardianAuthEndpoints
{
    public static IEndpointRouteBuilder MapGuardianAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth/guardian").WithTags("GuardianAuth");

        group.MapPost("/otp/request", async (RequestGuardianOtpRequest request, IGuardianAuthenticationService auth, CancellationToken ct) =>
        {
            var result = await auth.RequestOtpAsync(request.PhoneNumber, ct);
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result.Error);
        }).AllowAnonymous();

        group.MapPost("/otp/verify", async (VerifyGuardianOtpRequest request, IGuardianAuthenticationService auth, HttpContext http, CancellationToken ct) =>
        {
            var result = await auth.VerifyOtpAsync(request.ChallengeId, request.Code, request.DeviceLabel, RemoteIp(http), ct);
            return result.Succeeded ? Results.Ok(result) : Results.Unauthorized();
        }).AllowAnonymous();

        return endpoints;
    }

    private static string? RemoteIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();
}

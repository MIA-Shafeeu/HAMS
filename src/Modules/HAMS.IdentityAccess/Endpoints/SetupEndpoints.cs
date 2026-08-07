using HAMS.IdentityAccess.Application.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.IdentityAccess.Endpoints;

public sealed record BootstrapAdminRequest(string Username, string Password);

/// <summary>
/// One-time production bootstrap surface (deploy plan): creates the very first System
/// Administrator account without needing direct server/database access. Deliberately anonymous —
/// there is no admin yet to authenticate as — but <see cref="ISetupService"/> permanently refuses
/// once a System Administrator already exists, so leaving this reachable after first use is not a
/// standing risk. Everything after this first account goes through the authenticated, staff-only
/// <c>IStaffAccountService</c> instead.
/// </summary>
internal static class SetupEndpoints
{
    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/setup").WithTags("Setup");

        group.MapGet("/bootstrap-needed", async (ISetupService service, CancellationToken ct) =>
            Results.Ok(new { needed = await service.IsBootstrapNeededAsync(ct) })).AllowAnonymous();

        group.MapPost("/bootstrap-admin", async (BootstrapAdminRequest request, ISetupService service, CancellationToken ct) =>
        {
            if (!await service.IsBootstrapNeededAsync(ct))
            {
                return Results.Conflict("A System Administrator already exists - this one-time setup endpoint permanently refuses after the first use.");
            }

            try
            {
                var userId = await service.BootstrapFirstAdminAsync(request.Username, request.Password, ct);
                return Results.Ok(new { userId });
            }
            catch (InvalidOperationException ex)
            {
                // Not "already bootstrapped" (checked above) - this is a genuine input problem
                // (weak password, malformed username, etc.), so it's a 400, not a 409.
                return Results.BadRequest(ex.Message);
            }
        }).AllowAnonymous();

        return endpoints;
    }
}

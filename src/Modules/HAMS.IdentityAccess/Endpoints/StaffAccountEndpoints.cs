using HAMS.IdentityAccess.Application.Auth;
using HAMS.IdentityAccess.Domain;
using HAMS.Platform.Access;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.IdentityAccess.Endpoints;

public sealed record CreateStaffAccountRequest(Guid PersonId, string UserName, string? Email, string InitialPassword);
public sealed record ResetStaffAccountPasswordRequest(string NewPassword);
public sealed record SetStaffAccountStatusRequest(AccountStatus Status);

/// <summary>Staff account administration (build plan Phase A4 scope) — every mutation requires a live School/System Administrator check, matching <c>IdentityAdminEndpoints</c>' role-assignment routes.</summary>
internal static class StaffAccountEndpoints
{
    public static IEndpointRouteBuilder MapStaffAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity/staff-accounts").WithTags("StaffAccounts").RequireAuthorization();

        group.MapGet("/", async (IStaffAccountService service, CancellationToken ct) =>
            Results.Ok(await service.GetAccountsAsync(ct)));

        group.MapGet("/by-person/{personId:guid}", async (Guid personId, IStaffAccountService service, CancellationToken ct) =>
        {
            var account = await service.GetAccountByPersonIdAsync(personId, ct);
            return account is null ? Results.NotFound() : Results.Ok(account);
        });

        group.MapPost("/", async (
            CreateStaffAccountRequest request, IStaffAccountService service,
            IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var userId = await service.CreateAccountAsync(request.PersonId, request.UserName, request.Email, request.InitialPassword, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Create, nameof(ApplicationUser), userId.ToString(), user.PersonId, $"Staff account created for person {request.PersonId}.", cancellationToken: ct);
                return Results.Created($"/api/v1/identity/staff-accounts/by-person/{request.PersonId}", new { id = userId });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{userId:guid}/reset-password", async (
            Guid userId, ResetStaffAccountPasswordRequest request, IStaffAccountService service,
            IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.ResetPasswordAsync(userId, request.NewPassword, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ApplicationUser), userId.ToString(), user.PersonId, "Staff account password reset by administrator.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{userId:guid}/status", async (
            Guid userId, SetStaffAccountStatusRequest request, IStaffAccountService service,
            IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetAccountStatusAsync(userId, request.Status, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ApplicationUser), userId.ToString(), user.PersonId, $"Staff account status set to {request.Status}.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }
}

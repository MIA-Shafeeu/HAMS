using HAMS.Platform.Access;
using HAMS.Platform.Access.Domain;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.IdentityAccess.Endpoints;

public sealed record AssignRoleRequest(Guid PersonId, string RoleCode, Guid? SchoolId, DateOnly EffectiveFrom, DateOnly? EffectiveTo);

public sealed record CreateRoleRequest(string Code, string Name, string? Description, int DisplayOrder);

public sealed record SetRoleActiveRequest(bool IsActive);

public sealed record CreateConfidentialityTierRequest(string Code, string Name, string? Description, int Rank, int DisplayOrder);

public sealed record SetConfidentialityTierActiveRequest(bool IsActive);

/// <summary>
/// Role-assignment administration. Gated by a live role check (via <see cref="IRoleMembershipQuery"/>)
/// rather than the coarse <c>IsSystemAdmin</c> JWT claim — that claim is UI-shell-only and can be
/// stale for up to the access token's lifetime, which is unacceptable for an actual authorization
/// decision (build plan §4). Both actions write an audit row: granting or revoking a role changes
/// what a person can access system-wide, exactly the kind of event build plan §1.4 requires every
/// module to record.
/// </summary>
internal static class IdentityAdminEndpoints
{
    public static IEndpointRouteBuilder MapIdentityAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("IdentityAdmin").RequireAuthorization();

        group.MapGet("/roles", async (IPersonRoleAssignmentService assignmentService, CancellationToken ct) =>
            Results.Ok(await assignmentService.GetRolesAsync(ct)));

        group.MapGet("/roles/all", async (IPersonRoleAssignmentService assignmentService, CancellationToken ct) =>
            Results.Ok(await assignmentService.GetAllRolesAsync(ct)));

        group.MapPost("/roles", async (
            CreateRoleRequest request,
            IPersonRoleAssignmentService assignmentService,
            IRoleMembershipQuery roleMembershipQuery,
            IAuditLogWriter audit,
            ICurrentUser currentUser,
            IClock clock,
            CancellationToken ct) =>
        {
            if (!await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock, ct))
            {
                return Results.Forbid();
            }

            var id = await assignmentService.CreateRoleAsync(request.Code, request.Name, request.Description, request.DisplayOrder, ct);

            await audit.WriteEntryAsync(
                clock.UtcNow, AuditAction.Create, nameof(Role), id.ToString(), currentUser.PersonId,
                $"Role '{request.Code}' created.", cancellationToken: ct);

            return Results.Created($"/api/v1/identity/roles/{id}", new { id });
        });

        group.MapPost("/roles/{roleId:guid}/status", async (
            Guid roleId,
            SetRoleActiveRequest request,
            IPersonRoleAssignmentService assignmentService,
            IRoleMembershipQuery roleMembershipQuery,
            IAuditLogWriter audit,
            ICurrentUser currentUser,
            IClock clock,
            CancellationToken ct) =>
        {
            if (!await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock, ct))
            {
                return Results.Forbid();
            }

            try
            {
                await assignmentService.SetRoleActiveAsync(roleId, request.IsActive, ct);

                await audit.WriteEntryAsync(
                    clock.UtcNow, AuditAction.Update, nameof(Role), roleId.ToString(), currentUser.PersonId,
                    $"Role active status set to {request.IsActive}.", cancellationToken: ct);

                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/confidentiality-tiers", async (IPersonRoleAssignmentService assignmentService, CancellationToken ct) =>
            Results.Ok(await assignmentService.GetConfidentialityTiersAsync(ct)));

        group.MapPost("/confidentiality-tiers", async (
            CreateConfidentialityTierRequest request,
            IPersonRoleAssignmentService assignmentService,
            IRoleMembershipQuery roleMembershipQuery,
            IAuditLogWriter audit,
            ICurrentUser currentUser,
            IClock clock,
            CancellationToken ct) =>
        {
            if (!await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock, ct))
            {
                return Results.Forbid();
            }

            var id = await assignmentService.CreateConfidentialityTierAsync(
                request.Code, request.Name, request.Description, request.Rank, request.DisplayOrder, ct);

            await audit.WriteEntryAsync(
                clock.UtcNow, AuditAction.Create, nameof(ConfidentialityTier), id.ToString(), currentUser.PersonId,
                $"Confidentiality tier '{request.Code}' created.", cancellationToken: ct);

            return Results.Created($"/api/v1/identity/confidentiality-tiers/{id}", new { id });
        });

        group.MapPost("/confidentiality-tiers/{tierId:guid}/status", async (
            Guid tierId,
            SetConfidentialityTierActiveRequest request,
            IPersonRoleAssignmentService assignmentService,
            IRoleMembershipQuery roleMembershipQuery,
            IAuditLogWriter audit,
            ICurrentUser currentUser,
            IClock clock,
            CancellationToken ct) =>
        {
            if (!await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock, ct))
            {
                return Results.Forbid();
            }

            try
            {
                await assignmentService.SetConfidentialityTierActiveAsync(tierId, request.IsActive, ct);

                await audit.WriteEntryAsync(
                    clock.UtcNow, AuditAction.Update, nameof(ConfidentialityTier), tierId.ToString(), currentUser.PersonId,
                    $"Confidentiality tier active status set to {request.IsActive}.", cancellationToken: ct);

                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/role-assignments", async (Guid personId, IPersonRoleAssignmentService assignmentService, CancellationToken ct) =>
            Results.Ok(await assignmentService.GetAssignmentsForPersonAsync(personId, ct)));

        group.MapPost("/role-assignments", async (
            AssignRoleRequest request,
            IPersonRoleAssignmentService assignmentService,
            IRoleMembershipQuery roleMembershipQuery,
            IAuditLogWriter audit,
            ICurrentUser currentUser,
            IClock clock,
            CancellationToken ct) =>
        {
            if (!await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock, ct))
            {
                return Results.Forbid();
            }

            try
            {
                var id = await assignmentService.AssignRoleAsync(
                    request.PersonId, request.RoleCode, request.SchoolId, request.EffectiveFrom, request.EffectiveTo, ct);

                await audit.WriteEntryAsync(
                    clock.UtcNow, AuditAction.Create, nameof(PersonRoleAssignment), id.ToString(), currentUser.PersonId,
                    $"Role '{request.RoleCode}' granted to person {request.PersonId}.", cancellationToken: ct);

                return Results.Created($"/api/v1/identity/role-assignments/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/role-assignments/{assignmentId:guid}", async (
            Guid assignmentId,
            IPersonRoleAssignmentService assignmentService,
            IRoleMembershipQuery roleMembershipQuery,
            IAuditLogWriter audit,
            ICurrentUser currentUser,
            IClock clock,
            CancellationToken ct) =>
        {
            if (!await roleMembershipQuery.IsSystemOrSchoolAdminAsync(currentUser, clock, ct))
            {
                return Results.Forbid();
            }

            try
            {
                await assignmentService.RevokeRoleAsync(assignmentId, clock.TodayUtc, ct);

                await audit.WriteEntryAsync(
                    clock.UtcNow, AuditAction.Update, nameof(PersonRoleAssignment), assignmentId.ToString(), currentUser.PersonId,
                    "Role assignment revoked.", cancellationToken: ct);

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

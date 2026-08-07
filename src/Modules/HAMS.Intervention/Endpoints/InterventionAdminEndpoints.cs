using HAMS.Intervention.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.Intervention.Endpoints;

public sealed record CreateBehaviourCategoryRequest(string Code, string Name, bool IsPositive, int DisplayOrder);
public sealed record CreateInterventionTypeRequest(string Code, string Name, int DisplayOrder);
public sealed record SetActiveRequest(bool IsActive);

/// <summary>
/// Behaviour-category / intervention-type admin surface (build plan §1.6 configurable-lookup rule) —
/// the same "extract inline lookup query into a dedicated admin service" pattern already applied to
/// <c>OrgEndpoints</c>/<c>PeopleEndpoints</c>. Kept as its own file rather than folded into
/// <see cref="BehaviourIncidentEndpoints"/>/<see cref="InterventionCaseEndpoints"/>: those files' own
/// inline single-row lookups resolve one code to one id for a single incident/case being created — a
/// different concern from listing/creating/toggling the lookup rows themselves, which this file owns.
/// </summary>
internal static class InterventionAdminEndpoints
{
    public static IEndpointRouteBuilder MapInterventionAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/intervention").WithTags("InterventionAdmin").RequireAuthorization();

        group.MapGet("/behaviour-categories", async (IInterventionAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetBehaviourCategoriesAsync(ct)));

        group.MapPost("/behaviour-categories", async (
            CreateBehaviourCategoryRequest request, IInterventionAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateBehaviourCategoryAsync(request.Code, request.Name, request.IsPositive, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/intervention/behaviour-categories/{id}", new { id });
        });

        group.MapPost("/behaviour-categories/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IInterventionAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetBehaviourCategoryActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/intervention-types", async (IInterventionAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetInterventionTypesAsync(ct)));

        group.MapPost("/intervention-types", async (
            CreateInterventionTypeRequest request, IInterventionAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateInterventionTypeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/intervention/intervention-types/{id}", new { id });
        });

        group.MapPost("/intervention-types/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IInterventionAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetInterventionTypeActiveAsync(id, request.IsActive, ct);
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

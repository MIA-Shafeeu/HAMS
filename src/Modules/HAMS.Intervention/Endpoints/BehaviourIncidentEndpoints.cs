using HAMS.Intervention.Application;
using HAMS.Intervention.Domain;
using HAMS.Intervention.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Endpoints;

public sealed record RecordBehaviourIncidentRequest(
    Guid StudentPersonId, string BehaviourCategoryCode, Guid? SubjectId, Guid AcademicYearId, string Description,
    string ConfidentialityTierCode, DateOnly OccurredDate);

public sealed record ApproveBehaviourIncidentRequest(string? ActionTaken, string? ReviewNotes);
public sealed record RejectOrReturnBehaviourIncidentRequest(string? ReviewNotes);

/// <summary>
/// Behaviour/pastoral surface (build plan Phase 13 scope, 7.18). Recording/submitting is routine
/// case-worker-style work needing only authentication, matching <see cref="InterventionCaseEndpoints"/>'s
/// exact precedent — but every single-incident read goes through <see cref="IConfidentialRecordAccessor"/>,
/// the same confidentiality chokepoint <c>InterventionCase</c> established in Phase 9, since the build
/// plan explicitly calls both domains' records confidential sub-records (§2).
/// </summary>
internal static class BehaviourIncidentEndpoints
{
    public static IEndpointRouteBuilder MapBehaviourIncidentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/intervention/behaviour-incidents").WithTags("BehaviourIncidents").RequireAuthorization();

        group.MapGet("/categories", async (InterventionDbContext db, CancellationToken ct) =>
            Results.Ok(await db.BehaviourCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(ct)));

        group.MapPost("/", async (
            RecordBehaviourIncidentRequest request, IBehaviourIncidentService service, InterventionDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            var category = await db.BehaviourCategories.SingleOrDefaultAsync(c => c.Code == request.BehaviourCategoryCode && c.IsActive, ct);
            if (category is null) return Results.BadRequest($"No active behaviour category with code '{request.BehaviourCategoryCode}'.");

            var id = await service.RecordAsync(
                request.StudentPersonId, category.Id, request.SubjectId, request.AcademicYearId, request.Description,
                request.ConfidentialityTierCode, recordedBy, request.OccurredDate, ct);
            return Results.Created($"/api/v1/intervention/behaviour-incidents/{id}", new { id });
        });

        group.MapGet("/{incidentId:guid}", async (
            Guid incidentId, IBehaviourIncidentService service, IConfidentialRecordAccessor accessor, HttpContext http, CancellationToken ct) =>
        {
            var incident = await service.GetAsync(incidentId, ct);
            if (incident is null) return Results.NotFound();

            var authorized = await accessor.CanAccessAsync(http.User, incident, nameof(BehaviourIncident), incidentId.ToString(), ct);
            return authorized ? Results.Ok(incident) : Results.Forbid();
        });

        group.MapPost("/{incidentId:guid}/submit", async (Guid incidentId, IBehaviourIncidentService service, CancellationToken ct) =>
        {
            try
            {
                await service.SubmitAsync(incidentId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{incidentId:guid}/begin-review", async (
            Guid incidentId, IBehaviourIncidentService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.BeginReviewAsync(incidentId, reviewedBy, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{incidentId:guid}/approve", async (
            Guid incidentId, ApproveBehaviourIncidentRequest request, IBehaviourIncidentService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.ApproveAsync(incidentId, reviewedBy, request.ActionTaken, request.ReviewNotes, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{incidentId:guid}/reject", async (
            Guid incidentId, RejectOrReturnBehaviourIncidentRequest request, IBehaviourIncidentService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.RejectAsync(incidentId, reviewedBy, request.ReviewNotes, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{incidentId:guid}/return", async (
            Guid incidentId, RejectOrReturnBehaviourIncidentRequest request, IBehaviourIncidentService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.ReturnAsync(incidentId, reviewedBy, request.ReviewNotes, ct);
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

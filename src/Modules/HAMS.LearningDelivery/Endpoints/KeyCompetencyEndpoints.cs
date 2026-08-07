using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Endpoints;

public sealed record CreateKeyCompetencyIndicatorRequest(Guid KeyCompetencyId, Guid KeyStageId, string Code, string DescriptionEn, string DescriptionDv, int DisplayOrder);
public sealed record RecordKeyCompetencyEvidenceRequest(
    Guid StudentPersonId, Guid KeyCompetencyIndicatorId, string EvidenceTypeCode, int? RatingScore, DateOnly RecordedDate, string? Notes);

/// <summary>Key Competency evidence surface (build plan §3/Phase 6 scope) — the parallel, lighter-weight evidence track alongside subject-outcome mastery.</summary>
internal static class KeyCompetencyEndpoints
{
    public static IEndpointRouteBuilder MapKeyCompetencyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/learning").WithTags("KeyCompetencies").RequireAuthorization();

        group.MapGet("/key-competencies", async (LearningDeliveryDbContext db, CancellationToken ct) =>
            Results.Ok(await db.KeyCompetencies.OrderBy(k => k.DisplayOrder).ToListAsync(ct)));

        group.MapGet("/key-competency-indicators", async (Guid keyStageId, LearningDeliveryDbContext db, CancellationToken ct) =>
            Results.Ok(await db.KeyCompetencyIndicators.Where(i => i.KeyStageId == keyStageId).OrderBy(i => i.DisplayOrder).ToListAsync(ct)));

        group.MapPost("/key-competency-indicators", async (
            CreateKeyCompetencyIndicatorRequest request, LearningDeliveryDbContext db, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var indicator = new KeyCompetencyIndicator
            {
                Id = Guid.NewGuid(), KeyCompetencyId = request.KeyCompetencyId, KeyStageId = request.KeyStageId, Code = request.Code,
                DescriptionEn = request.DescriptionEn, DescriptionDv = request.DescriptionDv, DisplayOrder = request.DisplayOrder,
            };
            db.KeyCompetencyIndicators.Add(indicator);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/learning/key-competency-indicators/{indicator.Id}", indicator);
        });

        group.MapPost("/key-competency-evidence", async (
            RecordKeyCompetencyEvidenceRequest request, IKeyCompetencyEvidenceService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            try
            {
                var id = await service.RecordAsync(
                    request.StudentPersonId, request.KeyCompetencyIndicatorId, request.EvidenceTypeCode, request.RatingScore,
                    request.RecordedDate, recordedBy, request.Notes, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/key-competency-evidence", async (
            Guid studentPersonId, Guid keyCompetencyIndicatorId, LearningDeliveryDbContext db, CancellationToken ct) =>
            Results.Ok(await db.KeyCompetencyEvidences
                .Where(e => e.StudentPersonId == studentPersonId && e.KeyCompetencyIndicatorId == keyCompetencyIndicatorId)
                .OrderBy(e => e.RecordedDate)
                .ToListAsync(ct)));

        return endpoints;
    }
}

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

public sealed record CreateAchievementScaleRequest(string Code, string Name, int MinimumEvidenceCount);
public sealed record AddAchievementLevelRequest(string Code, string Name, int Rank, int DisplayOrder);
public sealed record RecordLearningEvidenceRequest(
    Guid StudentPersonId, Guid LearningOutcomeId, Guid? LessonSessionId, string EvidenceTypeCode,
    Guid AchievementLevelId, DateOnly RecordedDate, string? Notes);
public sealed record RecordMasteryEvaluationRequest(
    Guid StudentPersonId, Guid LearningOutcomeId, Guid KeyStagePolicyId, Guid AchievementScaleId, Guid? ManualAchievementLevelId);

/// <summary>Achievement scale/evidence/mastery-evaluation surface (build plan Phase 6 scope). Configuration mutations require a live School/System Administrator check; day-to-day evidence/evaluation recording only requires authentication, matching Attendance's precedent — any authenticated staff member records these routinely.</summary>
internal static class MasteryEndpoints
{
    public static IEndpointRouteBuilder MapMasteryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/learning").WithTags("Mastery").RequireAuthorization();

        group.MapGet("/evidence-types", async (ILessonPlanningService service, CancellationToken ct) =>
            Results.Ok(await service.GetEvidenceTypesAsync(ct)));

        group.MapPost("/evidence-types", async (
            CreateSimpleLookupRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateEvidenceTypeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/learning/evidence-types/{id}", new { id });
        });

        group.MapPost("/evidence-types/{id:guid}/status", async (
            Guid id, SetActiveRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetEvidenceTypeActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/achievement-scales", async (LearningDeliveryDbContext db, CancellationToken ct) =>
            Results.Ok(await db.AchievementScales.OrderBy(s => s.DisplayOrder).ToListAsync(ct)));

        group.MapPost("/achievement-scales", async (
            CreateAchievementScaleRequest request, LearningDeliveryDbContext db, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var scale = new AchievementScale { Id = Guid.NewGuid(), Code = request.Code, Name = request.Name, MinimumEvidenceCount = request.MinimumEvidenceCount };
            db.AchievementScales.Add(scale);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/learning/achievement-scales/{scale.Id}", scale);
        });

        group.MapGet("/achievement-scales/{scaleId:guid}/levels", async (Guid scaleId, LearningDeliveryDbContext db, CancellationToken ct) =>
            Results.Ok(await db.AchievementLevels.Where(l => l.AchievementScaleId == scaleId).OrderBy(l => l.DisplayOrder).ToListAsync(ct)));

        group.MapPost("/achievement-scales/{scaleId:guid}/levels", async (
            Guid scaleId, AddAchievementLevelRequest request, LearningDeliveryDbContext db, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var level = new AchievementLevel
            {
                Id = Guid.NewGuid(), AchievementScaleId = scaleId, Code = request.Code, Name = request.Name,
                Rank = request.Rank, DisplayOrder = request.DisplayOrder,
            };
            db.AchievementLevels.Add(level);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/learning/achievement-scales/{scaleId}/levels/{level.Id}", level);
        });

        group.MapPost("/evidence", async (
            RecordLearningEvidenceRequest request, ILearningEvidenceService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            try
            {
                var id = await service.RecordAsync(
                    request.StudentPersonId, request.LearningOutcomeId, request.LessonSessionId, request.EvidenceTypeCode,
                    request.AchievementLevelId, request.RecordedDate, recordedBy, request.Notes, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/evidence", async (Guid studentPersonId, Guid learningOutcomeId, LearningDeliveryDbContext db, CancellationToken ct) =>
            Results.Ok(await db.LearningEvidences
                .Where(e => e.StudentPersonId == studentPersonId && e.LearningOutcomeId == learningOutcomeId)
                .OrderBy(e => e.RecordedDate)
                .ToListAsync(ct)));

        group.MapGet("/mastery-evaluations/recommend", async (
            Guid studentPersonId, Guid learningOutcomeId, Guid achievementScaleId, IRecommendedLevelEngine engine, CancellationToken ct) =>
            Results.Ok(await engine.RecommendAsync(studentPersonId, learningOutcomeId, achievementScaleId, ct)));

        group.MapPost("/mastery-evaluations", async (
            RecordMasteryEvaluationRequest request, IMasteryEvaluationService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            try
            {
                var id = await service.RecordEvaluationAsync(
                    request.StudentPersonId, request.LearningOutcomeId, request.KeyStagePolicyId, request.AchievementScaleId,
                    recordedBy, request.ManualAchievementLevelId, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/mastery-evaluations/current", async (
            Guid studentPersonId, Guid learningOutcomeId, IMasteryEvaluationService service, CancellationToken ct) =>
        {
            var evaluation = await service.GetCurrentAsync(studentPersonId, learningOutcomeId, ct);
            return evaluation is null ? Results.NotFound() : Results.Ok(evaluation);
        });

        return endpoints;
    }
}

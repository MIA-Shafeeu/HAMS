using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Endpoints;

public sealed record ScheduleLessonSessionRequest(Guid LessonPlanId, Guid ClassId, DateOnly ActualDate, Guid PeriodId);
public sealed record CompleteLessonSessionRequest(IReadOnlyList<Guid> CoveredOutcomeIds);

/// <summary>Lesson-session and coverage-comparison surface (build plan Phase 5 scope). Mutations require a live School/System Administrator check.</summary>
internal static class LessonSessionEndpoints
{
    public static IEndpointRouteBuilder MapLessonSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/learning").WithTags("LessonSessions").RequireAuthorization();

        group.MapPost("/lesson-sessions", async (
            ScheduleLessonSessionRequest request, ILessonSessionService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.ScheduleAsync(request.LessonPlanId, request.ClassId, request.ActualDate, request.PeriodId, ct);
            return Results.Created($"/api/v1/learning/lesson-sessions/{id}", new { id });
        });

        group.MapPost("/lesson-sessions/{sessionId:guid}/complete", async (
            Guid sessionId, CompleteLessonSessionRequest request, ILessonSessionService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.CompleteAsync(sessionId, request.CoveredOutcomeIds, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/lesson-sessions/{sessionId:guid}/cancel", async (
            Guid sessionId, ILessonSessionService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.CancelAsync(sessionId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/lesson-sessions", async (Guid classId, DateOnly date, LearningDeliveryDbContext db, CancellationToken ct) =>
            Results.Ok(await db.LessonSessions.Where(s => s.ClassId == classId && s.ActualDate == date).ToListAsync(ct)));

        group.MapGet("/schemes-of-work/{schemeOfWorkId:guid}/coverage", async (
            Guid schemeOfWorkId, ICoverageComparisonService coverage, CancellationToken ct) =>
            Results.Ok(await coverage.CompareAsync(schemeOfWorkId, ct)));

        return endpoints;
    }
}

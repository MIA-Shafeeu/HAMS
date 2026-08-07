using HAMS.Intervention.Application;
using HAMS.Intervention.Domain;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.Intervention.Endpoints;

public sealed record RequestTopicClosureRequest(Guid TeachingTopicId);

public sealed record ReviewTopicClosureRequest(string? ReviewNotes);

public sealed record ApproveTopicClosureRequest(string? ReviewNotes, IReadOnlyCollection<Guid> StudentPersonIdsWithGaps);

/// <summary>
/// Topic-closure workflow surface (build plan Phase 9 scope) — the second real
/// <c>Platform.Workflow</c> consumer, reusing the exact Draft→Submitted→UnderReview→Approved/
/// Rejected/Returned pipeline built for Phase 7's assessment moderation. Requesting/actioning a
/// closure only requires authentication, matching that precedent — this is routine teaching
/// workflow, not an admin configuration change. Every workflow transition writes an audit row here
/// at the endpoint layer (see <c>AssessmentResultEndpoints</c>'s remarks for why).
/// </summary>
internal static class TopicClosureEndpoints
{
    public static IEndpointRouteBuilder MapTopicClosureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/intervention").WithTags("TopicClosures").RequireAuthorization();

        group.MapPost("/topic-closures", async (
            RequestTopicClosureRequest request, ITopicClosureService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } requestedBy) return Results.Unauthorized();

            var id = await service.RequestClosureAsync(request.TeachingTopicId, requestedBy, ct);
            return Results.Created($"/api/v1/intervention/topic-closures/{id}", new { id });
        });

        group.MapGet("/topic-closures/current", async (Guid teachingTopicId, ITopicClosureService service, CancellationToken ct) =>
        {
            var closure = await service.GetCurrentAsync(teachingTopicId, ct);
            return closure is null ? Results.NotFound() : Results.Ok(closure);
        });

        group.MapPost("/topic-closures/{closureId:guid}/submit", async (
            Guid closureId, ITopicClosureService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            try
            {
                await service.SubmitAsync(closureId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(TopicClosure), closureId.ToString(), user.PersonId, "Topic closure submitted for review.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/topic-closures/{closureId:guid}/begin-review", async (
            Guid closureId, ITopicClosureService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.BeginReviewAsync(closureId, reviewedBy, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(TopicClosure), closureId.ToString(), reviewedBy, "Topic closure review began.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/topic-closures/{closureId:guid}/approve", async (
            Guid closureId, ApproveTopicClosureRequest request, ITopicClosureService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.ApproveAsync(closureId, reviewedBy, request.ReviewNotes, request.StudentPersonIdsWithGaps, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(TopicClosure), closureId.ToString(), reviewedBy, "Topic closure approved.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/topic-closures/{closureId:guid}/reject", async (
            Guid closureId, ReviewTopicClosureRequest request, ITopicClosureService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.RejectAsync(closureId, reviewedBy, request.ReviewNotes, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(TopicClosure), closureId.ToString(), reviewedBy, "Topic closure rejected.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/topic-closures/{closureId:guid}/return", async (
            Guid closureId, ReviewTopicClosureRequest request, ITopicClosureService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (user.PersonId is not { } reviewedBy) return Results.Unauthorized();

            try
            {
                await service.ReturnAsync(closureId, reviewedBy, request.ReviewNotes, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(TopicClosure), closureId.ToString(), reviewedBy, "Topic closure returned for correction.", cancellationToken: ct);
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

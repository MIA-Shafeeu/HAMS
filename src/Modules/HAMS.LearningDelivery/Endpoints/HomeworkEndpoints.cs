using HAMS.LearningDelivery.Application;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.LearningDelivery.Endpoints;

public sealed record CreateHomeworkRequest(
    Guid ClassId, Guid SubjectId, Guid? TeachingTopicId, string TitleEn, string TitleDv,
    string InstructionsEn, string InstructionsDv, DateOnly AssignedDate, DateOnly DueDate, int? MaxScore);
public sealed record GradeHomeworkSubmissionRequest(int? Score, string? FeedbackText);

/// <summary>
/// Homework/assignment surface (build plan Phase 13 scope, 7.17). Deliberately gated on plain
/// <see cref="ICurrentUser"/> staff identity, NOT the <c>IsSystemOrSchoolAdminAsync</c> admin check
/// every other LearningDelivery endpoint uses (SchemeOfWork/LessonPlan/Resources) — assigning and
/// grading homework is routine, day-to-day subject-teacher work, the same class of action as
/// <c>AttendanceService</c> marking attendance (Phase 5), not curriculum-structure administration.
/// A real per-teacher "is this actually their class" scope check would be the more precise gate, but
/// <c>PlatformAccessPolicies.Scope</c> has zero consumers anywhere in this codebase yet (Phase 9) —
/// wiring it up is out of this phase's stated scope, not a gap introduced here.
/// </summary>
internal static class HomeworkEndpoints
{
    public static IEndpointRouteBuilder MapHomeworkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/learning/homework").WithTags("Homework").RequireAuthorization();

        group.MapPost("/", async (CreateHomeworkRequest request, IHomeworkService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff || user.PersonId is not { } assignedBy) return Results.Forbid();

            try
            {
                var id = await service.CreateAsync(
                    request.ClassId, request.SubjectId, request.TeachingTopicId, request.TitleEn, request.TitleDv,
                    request.InstructionsEn, request.InstructionsDv, request.AssignedDate, request.DueDate, request.MaxScore, assignedBy, ct);
                return Results.Created($"/api/v1/learning/homework/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/{homeworkId:guid}", async (Guid homeworkId, IHomeworkService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            var homework = await service.GetAsync(homeworkId, ct);
            return homework is null ? Results.NotFound() : Results.Ok(homework);
        });

        group.MapGet("/", async (Guid classId, IHomeworkService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            return Results.Ok(await service.ListForClassAsync(classId, ct));
        });

        group.MapGet("/{homeworkId:guid}/submissions", async (
            Guid homeworkId, IHomeworkSubmissionService submissions, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            return Results.Ok(await submissions.ListForHomeworkAsync(homeworkId, ct));
        });

        group.MapPost("/submissions/{submissionId:guid}/grade", async (
            Guid submissionId, GradeHomeworkSubmissionRequest request, IHomeworkSubmissionService submissions, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff || user.PersonId is not { } gradedBy) return Results.Forbid();

            try
            {
                await submissions.GradeAsync(submissionId, request.Score, request.FeedbackText, gradedBy, ct);
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

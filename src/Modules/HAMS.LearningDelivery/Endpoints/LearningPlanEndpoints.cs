using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.LearningDelivery.Endpoints;

public sealed record CreateSimpleLookupRequest(string Code, string Name, int DisplayOrder);
public sealed record SetActiveRequest(bool IsActive);
public sealed record CreateSchemeOfWorkRequest(Guid SubjectId, Guid GradeId, Guid AcademicYearId, string Title);
public sealed record AddSchemeOfWorkItemRequest(Guid LearningOutcomeId, int PlannedWeekNumber, int DisplayOrder);
public sealed record CreateTeachingTopicRequest(Guid SchemeOfWorkItemId, string NameEn, string NameDv, int DisplayOrder);
public sealed record CreateLessonPlanRequest(Guid TeachingTopicId, Guid StaffPersonId, DateOnly PlannedDate, string Objectives);
public sealed record AddResourceRequest(Guid TeachingTopicId, string TitleEn, string TitleDv, string ResourceTypeCode, string FileReference, Guid UploadedByPersonId);

/// <summary>
/// Scheme of Work / Lesson Planning / Resources admin surface (build plan Phase 5 scope). Mutations require a live School/System Administrator check.
/// Delegates all reads/writes to <see cref="ILessonPlanningService"/> — the single implementation
/// this endpoint group and any Blazor page both share, rather than duplicating EF queries in both places.
/// </summary>
internal static class LearningPlanEndpoints
{
    public static IEndpointRouteBuilder MapLearningPlanEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/learning").WithTags("LearningDelivery").RequireAuthorization();

        group.MapGet("/schemes-of-work", async (Guid subjectId, Guid gradeId, Guid academicYearId, ILessonPlanningService service, CancellationToken ct) =>
            Results.Ok(await service.GetSchemesOfWorkAsync(subjectId, gradeId, academicYearId, ct)));

        group.MapPost("/schemes-of-work", async (
            CreateSchemeOfWorkRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateSchemeOfWorkAsync(request.SubjectId, request.GradeId, request.AcademicYearId, request.Title, ct);
            return Results.Created($"/api/v1/learning/schemes-of-work/{id}", new { id });
        });

        group.MapPost("/schemes-of-work/{schemeOfWorkId:guid}/items", async (
            Guid schemeOfWorkId, AddSchemeOfWorkItemRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.AddSchemeOfWorkItemAsync(schemeOfWorkId, request.LearningOutcomeId, request.PlannedWeekNumber, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/learning/schemes-of-work/{schemeOfWorkId}/items/{id}", new { id });
        });

        group.MapGet("/schemes-of-work/{schemeOfWorkId:guid}/items", async (Guid schemeOfWorkId, ILessonPlanningService service, CancellationToken ct) =>
            Results.Ok(await service.GetSchemeOfWorkItemsAsync(schemeOfWorkId, ct)));

        group.MapPost("/teaching-topics", async (
            CreateTeachingTopicRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateTeachingTopicAsync(request.SchemeOfWorkItemId, request.NameEn, request.NameDv, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/learning/teaching-topics/{id}", new { id });
        });

        group.MapPost("/lesson-plans", async (
            CreateLessonPlanRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateLessonPlanAsync(request.TeachingTopicId, request.StaffPersonId, request.PlannedDate, request.Objectives, ct);
            return Results.Created($"/api/v1/learning/lesson-plans/{id}", new { id });
        });

        group.MapGet("/lesson-plans", async (Guid teachingTopicId, ILessonPlanningService service, CancellationToken ct) =>
            Results.Ok(await service.GetLessonPlansAsync(teachingTopicId, ct)));

        group.MapGet("/resource-types", async (ILessonPlanningService service, CancellationToken ct) =>
            Results.Ok(await service.GetResourceTypesAsync(ct)));

        group.MapPost("/resource-types", async (
            CreateSimpleLookupRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateResourceTypeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/learning/resource-types/{id}", new { id });
        });

        group.MapPost("/resource-types/{id:guid}/status", async (
            Guid id, SetActiveRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetResourceTypeActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapPost("/resources", async (
            AddResourceRequest request, ILessonPlanningService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.AddResourceAsync(
                    request.TeachingTopicId, request.TitleEn, request.TitleDv, request.ResourceTypeCode, request.FileReference, request.UploadedByPersonId, ct);
                return Results.Created($"/api/v1/learning/resources/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/resources", async (Guid teachingTopicId, ILessonPlanningService service, CancellationToken ct) =>
            Results.Ok(await service.GetResourcesAsync(teachingTopicId, ct)));

        return endpoints;
    }
}

using HAMS.AssessmentEvaluation.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.AssessmentEvaluation.Endpoints;

public sealed record CreateSimpleLookupRequest(string Code, string Name, int DisplayOrder);
public sealed record SetActiveRequest(bool IsActive);
public sealed record CreateAssessmentSchemeRequest(string Code, string Name);
public sealed record AddAssessmentSchemeComponentRequest(string AssessmentCategoryCode, string ResultAggregationRuleCode, decimal WeightPercentage, int DisplayOrder);
public sealed record CreateGradeScaleRequest(string Code, string Name);
public sealed record AddGradeBandRequest(string Code, string Name, decimal MinPercentage, decimal MaxPercentage, int Rank, int DisplayOrder);
public sealed record CreateAssessmentRequest(
    Guid SubjectId, Guid GradeId, Guid TermId, Guid AcademicYearId, string AssessmentCategoryCode, string Title,
    decimal MaxMarks, int? DurationMinutes, string? ExternalExaminationBoardCode, string? ExternalSyllabusCode, DateOnly ScheduledDate);

/// <summary>Assessment scheme/grade-scale/assessment configuration surface (build plan Phase 7 scope). Mutations require a live School/System Administrator check.</summary>
internal static class AssessmentConfigEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentConfigEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/assessment").WithTags("AssessmentConfig").RequireAuthorization();

        group.MapGet("/categories", async (IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAssessmentCategoriesAsync(ct)));

        group.MapPost("/categories", async (
            CreateSimpleLookupRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateAssessmentCategoryAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/assessment/categories/{id}", new { id });
        });

        group.MapPost("/categories/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetAssessmentCategoryActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/external-examination-boards", async (IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetExternalExaminationBoardsAsync(ct)));

        group.MapPost("/external-examination-boards", async (
            CreateSimpleLookupRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateExternalExaminationBoardAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/assessment/external-examination-boards/{id}", new { id });
        });

        group.MapPost("/external-examination-boards/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetExternalExaminationBoardActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/special-result-states", async (IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetSpecialResultStatesAsync(ct)));

        group.MapPost("/special-result-states", async (
            CreateSimpleLookupRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateSpecialResultStateAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/assessment/special-result-states/{id}", new { id });
        });

        group.MapPost("/special-result-states/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetSpecialResultStateActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/result-aggregation-rules", async (IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetResultAggregationRulesAsync(ct)));

        group.MapPost("/result-aggregation-rules", async (
            CreateSimpleLookupRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateResultAggregationRuleAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/assessment/result-aggregation-rules/{id}", new { id });
        });

        group.MapPost("/result-aggregation-rules/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetResultAggregationRuleActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/schemes", async (IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAssessmentSchemesAsync(ct)));

        group.MapPost("/schemes", async (
            CreateAssessmentSchemeRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateAssessmentSchemeAsync(request.Code, request.Name, ct);
            return Results.Created($"/api/v1/assessment/schemes/{id}", new { id });
        });

        group.MapGet("/schemes/{schemeId:guid}/components", async (Guid schemeId, IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAssessmentSchemeComponentsAsync(schemeId, ct)));

        group.MapPost("/schemes/{schemeId:guid}/components", async (
            Guid schemeId, AddAssessmentSchemeComponentRequest request, IAssessmentConfigAdminService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.AddAssessmentSchemeComponentAsync(
                    schemeId, request.AssessmentCategoryCode, request.ResultAggregationRuleCode, request.WeightPercentage, request.DisplayOrder, ct);
                return Results.Created($"/api/v1/assessment/schemes/{schemeId}/components/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/grade-scales", async (IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetGradeScalesAsync(ct)));

        group.MapPost("/grade-scales", async (
            CreateGradeScaleRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateGradeScaleAsync(request.Code, request.Name, ct);
            return Results.Created($"/api/v1/assessment/grade-scales/{id}", new { id });
        });

        group.MapGet("/grade-scales/{scaleId:guid}/bands", async (Guid scaleId, IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetGradeBandsAsync(scaleId, ct)));

        group.MapPost("/grade-scales/{scaleId:guid}/bands", async (
            Guid scaleId, AddGradeBandRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.AddGradeBandAsync(scaleId, request.Code, request.Name, request.MinPercentage, request.MaxPercentage, request.Rank, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/assessment/grade-scales/{scaleId}/bands/{id}", new { id });
        });

        group.MapGet("/assessments", async (Guid subjectId, Guid gradeId, Guid termId, IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAssessmentsAsync(subjectId, gradeId, termId, ct)));

        group.MapPost("/assessments", async (
            CreateAssessmentRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.CreateAssessmentAsync(
                    request.SubjectId, request.GradeId, request.TermId, request.AcademicYearId, request.AssessmentCategoryCode, request.Title,
                    request.MaxMarks, request.DurationMinutes, request.ExternalExaminationBoardCode, request.ExternalSyllabusCode, request.ScheduledDate, ct);
                return Results.Created($"/api/v1/assessment/assessments/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }
}

using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.AssessmentEvaluation.Endpoints;

public sealed record CreateEvaluationPeriodRequest(Guid AcademicYearId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int DisplayOrder);
public sealed record TriggerEvaluationRequest(Guid StudentPersonId, Guid SubjectId, Guid AcademicYearId, Guid EvaluationPeriodId, DateOnly AsOf);

/// <summary>Key-Stage Evaluation Engine surface (build plan Phase 8 scope). Period configuration is admin-gated; running/reading an evaluation only requires authentication, matching the Assessment-result-recording precedent — evaluating a student isn't a configuration change.</summary>
internal static class EvaluationEndpoints
{
    public static IEndpointRouteBuilder MapEvaluationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/assessment").WithTags("Evaluation").RequireAuthorization();

        group.MapGet("/evaluation-periods", async (Guid academicYearId, IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetEvaluationPeriodsAsync(academicYearId, ct)));

        group.MapPost("/evaluation-periods", async (
            CreateEvaluationPeriodRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateEvaluationPeriodAsync(request.AcademicYearId, request.Code, request.Name, request.StartDate, request.EndDate, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/assessment/evaluation-periods/{id}", new { id });
        });

        group.MapPost("/key-stage-evaluations", async (
            TriggerEvaluationRequest request, IKeyStageEvaluationService service, CancellationToken ct) =>
        {
            try
            {
                var id = await service.EvaluateAsync(
                    request.StudentPersonId, request.SubjectId, request.AcademicYearId, request.EvaluationPeriodId, request.AsOf, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/key-stage-evaluations/current", async (
            Guid studentPersonId, Guid subjectId, Guid evaluationPeriodId, IKeyStageEvaluationService service, CancellationToken ct) =>
        {
            var evaluation = await service.GetCurrentAsync(studentPersonId, subjectId, evaluationPeriodId, ct);
            return evaluation is null ? Results.NotFound() : Results.Ok(evaluation);
        });

        return endpoints;
    }
}

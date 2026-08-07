using HAMS.AssessmentEvaluation.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.AssessmentEvaluation.Endpoints;

public sealed record CreatePromotionPolicyRequest(string Code, string Name, int MinimumRank, int MinimumSubjectsRequiredToClear);
public sealed record RecordPromotionDecisionRequest(Guid StudentPersonId, Guid AcademicYearId, bool Promoted, Guid? NextGradeId, DateOnly DecisionDate, string? Notes);

/// <summary>Promotion/Progression surface (build plan Phase 11 scope). Configuring a policy is admin-gated like every other lookup entity; recording an actual decision is gated the same way — a one-way, significant administrative act, not routine recording.</summary>
internal static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/assessment/promotion").WithTags("Promotion").RequireAuthorization();

        group.MapGet("/policies", async (IAssessmentConfigAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetPromotionPoliciesAsync(ct)));

        group.MapPost("/policies", async (
            CreatePromotionPolicyRequest request, IAssessmentConfigAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreatePromotionPolicyAsync(request.Code, request.Name, request.MinimumRank, request.MinimumSubjectsRequiredToClear, ct);
            return Results.Created($"/api/v1/assessment/promotion/policies/{id}", new { id });
        });

        group.MapGet("/eligibility", async (
            Guid studentPersonId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, IPromotionService service, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.EvaluateEligibilityAsync(studentPersonId, academicYearId, evaluationPeriodId, asOf, ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/decisions", async (
            RecordPromotionDecisionRequest request, IPromotionService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();
            if (user.PersonId is not { } decidedByPersonId) return Results.Unauthorized();

            try
            {
                var id = await service.RecordDecisionAsync(
                    request.StudentPersonId, request.AcademicYearId, request.Promoted, request.NextGradeId, decidedByPersonId,
                    request.DecisionDate, request.Notes, ct);
                return Results.Created($"/api/v1/assessment/promotion/decisions/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/decisions", async (Guid studentPersonId, IPromotionService service, CancellationToken ct) =>
            Results.Ok(await service.GetDecisionsForStudentAsync(studentPersonId, ct)));

        group.MapGet("/worklist", async (Guid gradeId, Guid academicYearId, DateOnly asOf, IPromotionService service, CancellationToken ct) =>
            Results.Ok(await service.GetStudentsNeedingDecisionAsync(gradeId, academicYearId, asOf, ct)));

        return endpoints;
    }
}

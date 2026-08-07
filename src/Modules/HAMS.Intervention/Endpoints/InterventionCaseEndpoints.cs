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

public sealed record OpenCaseRequest(
    Guid StudentPersonId, Guid SubjectId, Guid AcademicYearId, string InterventionTypeCode, string ConfidentialityTierCode,
    Guid? LearningOutcomeId, Guid? TriggeringKeyStageEvaluationId, Guid? CarriedForwardGapId, DateOnly OpenedDate);

public sealed record CreatePlanRequest(string Description, Guid AssignedStaffPersonId, DateOnly StartDate, DateOnly TargetDate, string? Notes);

public sealed record RecordReassessmentAttemptRequest(Guid AcademicYearId, Guid EvaluationPeriodId, DateOnly AsOf);

public sealed record CloseCaseRequest(DateOnly ClosedDate);

/// <summary>
/// Intervention-case surface (build plan Phase 9 scope). Opening/planning/recording/closing a case
/// requires only authentication, matching every other case-worker-style action in the system
/// (Assessment recording, LearningEvidence). The single-case GET is the exception and the whole
/// point of this phase's confidentiality work: it is the first real
/// <see cref="IConfidentialRecordAccessor"/> consumer, gating access to a specific case beyond
/// plain authentication.
/// </summary>
internal static class InterventionCaseEndpoints
{
    public static IEndpointRouteBuilder MapInterventionCaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/intervention").WithTags("InterventionCases").RequireAuthorization();

        group.MapPost("/cases", async (
            OpenCaseRequest request, InterventionDbContext db, IInterventionCaseService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } openedBy) return Results.Unauthorized();

            var interventionType = await db.InterventionTypes.SingleOrDefaultAsync(t => t.Code == request.InterventionTypeCode && t.IsActive, ct);
            if (interventionType is null) return Results.BadRequest($"No active intervention type with code '{request.InterventionTypeCode}'.");

            try
            {
                var id = await service.OpenCaseAsync(
                    request.StudentPersonId, request.SubjectId, request.AcademicYearId, interventionType.Id, request.ConfidentialityTierCode,
                    request.LearningOutcomeId, request.TriggeringKeyStageEvaluationId, request.CarriedForwardGapId,
                    openedBy, request.OpenedDate, ct);
                return Results.Created($"/api/v1/intervention/cases/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/cases/{caseId:guid}", async (
            Guid caseId, IInterventionCaseService service, IConfidentialRecordAccessor accessor, HttpContext http, CancellationToken ct) =>
        {
            var interventionCase = await service.GetAsync(caseId, ct);
            if (interventionCase is null) return Results.NotFound();

            var authorized = await accessor.CanAccessAsync(http.User, interventionCase, nameof(InterventionCase), caseId.ToString(), ct);
            return authorized ? Results.Ok(interventionCase) : Results.Forbid();
        });

        group.MapPost("/cases/{caseId:guid}/plans", async (
            Guid caseId, CreatePlanRequest request, IInterventionCaseService service, CancellationToken ct) =>
        {
            try
            {
                var id = await service.CreatePlanAsync(
                    caseId, request.Description, request.AssignedStaffPersonId, request.StartDate, request.TargetDate, request.Notes, ct);
                return Results.Created($"/api/v1/intervention/cases/{caseId}/plans/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/cases/{caseId:guid}/plans", async (
            Guid caseId, IInterventionCaseService service, IConfidentialRecordAccessor accessor, HttpContext http, CancellationToken ct) =>
        {
            var interventionCase = await service.GetAsync(caseId, ct);
            if (interventionCase is null) return Results.NotFound();

            var authorized = await accessor.CanAccessAsync(http.User, interventionCase, nameof(InterventionCase), caseId.ToString(), ct);
            return authorized ? Results.Ok(await service.GetPlansAsync(caseId, ct)) : Results.Forbid();
        });

        group.MapPost("/cases/{caseId:guid}/reassessment-attempts", async (
            Guid caseId, RecordReassessmentAttemptRequest request, IInterventionCaseService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            try
            {
                var id = await service.RecordReassessmentAttemptAsync(
                    caseId, request.AcademicYearId, request.EvaluationPeriodId, request.AsOf, recordedBy, ct);
                return Results.Created($"/api/v1/intervention/cases/{caseId}/reassessment-attempts/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/cases/{caseId:guid}/reassessment-attempts", async (
            Guid caseId, IInterventionCaseService service, IConfidentialRecordAccessor accessor, HttpContext http, CancellationToken ct) =>
        {
            var interventionCase = await service.GetAsync(caseId, ct);
            if (interventionCase is null) return Results.NotFound();

            var authorized = await accessor.CanAccessAsync(http.User, interventionCase, nameof(InterventionCase), caseId.ToString(), ct);
            return authorized ? Results.Ok(await service.GetReassessmentAttemptsAsync(caseId, ct)) : Results.Forbid();
        });

        group.MapPost("/cases/{caseId:guid}/close", async (Guid caseId, CloseCaseRequest request, IInterventionCaseService service, CancellationToken ct) =>
        {
            try
            {
                await service.CloseCaseAsync(caseId, request.ClosedDate, ct);
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

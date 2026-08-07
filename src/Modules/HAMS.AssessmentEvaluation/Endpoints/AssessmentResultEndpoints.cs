using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Endpoints;

public sealed record RecordRawMarkRequest(Guid AssessmentId, Guid StudentPersonId, Guid KeyStagePolicyId, decimal? RawMark, string? SpecialResultStateCode);
public sealed record ReviseRawMarkRequest(decimal? RawMark, string? SpecialResultStateCode);
public sealed record BeginReviewRequest(decimal? AdjustedMark);
public sealed record ApproveResultRequest(decimal? ModeratedMark);
public sealed record EscalateResultRequest(string EscalationReason);
public sealed record ReviseApprovedResultRequest(decimal NewFinalMark);

/// <summary>
/// Assessment result recording/moderation surface (build plan Phase 7 scope: Submit → Review →
/// Approve/Reject/Return; Phase 13 adds Escalate). Recording/moderation actions require only
/// authentication — matching Attendance/LearningEvidence's precedent, since marking and moderating
/// exams is routine staff work, not admin-only. Two exceptions: <c>revise-approved</c> (correcting an
/// already-Published/Locked result is a sensitive override, admin-gated like every other
/// configuration mutation) and deciding an <b>escalated</b> result — <c>approve</c>/<c>reject</c>
/// check the result's current status first and require a live School/System Administrator check
/// only when it's <see cref="WorkflowStatus.Escalated"/>, since the entire point of escalating
/// (Phase 13: "advanced moderation") is that only a senior reviewer makes that final call; an
/// ordinary UnderReview result reaching either endpoint is unaffected. Every workflow transition
/// writes an audit row here at the endpoint layer (where the caller's identity is already resolved
/// via <c>ICurrentUser</c>) rather than inside <c>AssessmentModerationService</c> itself, so the
/// Application-layer service signatures — and everything that already unit-tests them directly —
/// stay untouched.
/// </summary>
internal static class AssessmentResultEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentResultEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/assessment").WithTags("AssessmentResults").RequireAuthorization();

        group.MapPost("/results", async (
            RecordRawMarkRequest request, AssessmentEvaluationDbContext db, IAssessmentModerationService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            Guid? specialResultStateId = null;
            if (request.SpecialResultStateCode is not null)
            {
                var state = await db.SpecialResultStates.SingleOrDefaultAsync(s => s.Code == request.SpecialResultStateCode && s.IsActive, ct);
                if (state is null) return Results.BadRequest($"No active special result state with code '{request.SpecialResultStateCode}'.");
                specialResultStateId = state.Id;
            }

            try
            {
                var id = await service.RecordRawMarkAsync(
                    request.AssessmentId, request.StudentPersonId, request.KeyStagePolicyId, request.RawMark, specialResultStateId, recordedBy, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/results", async (Guid assessmentId, AssessmentEvaluationDbContext db, CancellationToken ct) =>
            Results.Ok(await db.AssessmentResults.Where(r => r.AssessmentId == assessmentId && r.IsCurrent).ToListAsync(ct)));

        group.MapPost("/results/{resultId:guid}/revise-raw-mark", async (
            Guid resultId, ReviseRawMarkRequest request, AssessmentEvaluationDbContext db, IAssessmentModerationService service, CancellationToken ct) =>
        {
            Guid? specialResultStateId = null;
            if (request.SpecialResultStateCode is not null)
            {
                var state = await db.SpecialResultStates.SingleOrDefaultAsync(s => s.Code == request.SpecialResultStateCode && s.IsActive, ct);
                if (state is null) return Results.BadRequest($"No active special result state with code '{request.SpecialResultStateCode}'.");
                specialResultStateId = state.Id;
            }

            try
            {
                await service.ReviseRawMarkAsync(resultId, request.RawMark, specialResultStateId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/results/{resultId:guid}/submit", async (
            Guid resultId, IAssessmentModerationService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            try
            {
                await service.SubmitAsync(resultId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(AssessmentResult), resultId.ToString(), user.PersonId, "Assessment result submitted for moderation.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/results/{resultId:guid}/begin-review", async (
            Guid resultId, BeginReviewRequest request, IAssessmentModerationService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            try
            {
                await service.BeginReviewAsync(resultId, request.AdjustedMark, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(AssessmentResult), resultId.ToString(), user.PersonId, "Assessment result moderation review began.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/results/{resultId:guid}/approve", async (
            Guid resultId, ApproveResultRequest request, AssessmentEvaluationDbContext db, IAssessmentModerationService service,
            IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await RequireAdminIfEscalatedAsync(db, roles, user, clock, resultId, ct)) return Results.Forbid();

            try
            {
                await service.ApproveAsync(resultId, request.ModeratedMark, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(AssessmentResult), resultId.ToString(), user.PersonId, "Assessment result approved and published.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/results/{resultId:guid}/reject", async (
            Guid resultId, AssessmentEvaluationDbContext db, IAssessmentModerationService service,
            IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await RequireAdminIfEscalatedAsync(db, roles, user, clock, resultId, ct)) return Results.Forbid();

            try
            {
                await service.RejectAsync(resultId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(AssessmentResult), resultId.ToString(), user.PersonId, "Assessment result rejected.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/results/{resultId:guid}/escalate", async (
            Guid resultId, EscalateResultRequest request, IAssessmentModerationService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (user.PersonId is not { } escalatedBy) return Results.Unauthorized();

            try
            {
                await service.EscalateAsync(resultId, escalatedBy, request.EscalationReason, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(AssessmentResult), resultId.ToString(), user.PersonId, "Assessment result escalated for a senior decision.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/results/{resultId:guid}/return", async (
            Guid resultId, IAssessmentModerationService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            try
            {
                await service.ReturnAsync(resultId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(AssessmentResult), resultId.ToString(), user.PersonId, "Assessment result returned for correction.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/results/{resultId:guid}/revise-approved", async (
            Guid resultId, ReviseApprovedResultRequest request, IAssessmentModerationService service,
            IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.ReviseApprovedResultAsync(resultId, request.NewFinalMark, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(AssessmentResult), id.ToString(), user.PersonId, $"Approved assessment result revised (supersedes {resultId}).", cancellationToken: ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }

    /// <summary>
    /// True if the caller may proceed: either the result isn't currently Escalated (ordinary staff
    /// moderation, unaffected), or it is and the caller is a live System/School Administrator. A
    /// missing result also returns true — <c>ApproveAsync</c>/<c>RejectAsync</c> themselves are what
    /// produce the real "not found"-shaped 400 for that case, so this check never masks it with a 403.
    /// </summary>
    private static async Task<bool> RequireAdminIfEscalatedAsync(
        AssessmentEvaluationDbContext db, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, Guid resultId, CancellationToken ct)
    {
        var result = await db.AssessmentResults.FindAsync([resultId], ct);
        if (result is null || result.ModerationStatus != WorkflowStatus.Escalated)
        {
            return true;
        }

        return await roles.IsSystemOrSchoolAdminAsync(user, clock, ct);
    }
}

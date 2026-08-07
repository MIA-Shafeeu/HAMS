using HAMS.Platform.Access;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.ReportingAnalyticsAudit.Domain;
using HAMS.ReportingAnalyticsAudit.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.ReportingAnalyticsAudit.Endpoints;

public sealed record PrepareReportCardHttpRequest(
    Guid StudentPersonId, Guid AcademicYearId, Guid EvaluationPeriodId, string NarrativeEn, string NarrativeDv, string NextStepsEn, string NextStepsDv);
public sealed record ReviseReportCardHttpRequest(string NarrativeEn, string NarrativeDv, string NextStepsEn, string NextStepsDv);

/// <summary>
/// Report card surface (build plan Phase 11 scope) — staff-only throughout. Preparing/submitting/
/// reviewing is routine teaching/leadership work needing no admin escalation, but every route here
/// still requires <see cref="ICurrentUser.IsStaff"/>: guardians and students hold real JWTs too
/// (build plan §5 — one issuance path for every principal type), so "authenticated" alone would let
/// either read or drive the workflow of ANY student's report card by guessing its id. A guardian/
/// student's own access is published-only and relationship/ownership-scoped, and goes through their
/// own dedicated portal surface (<c>GuardianPortalEndpoints</c>/<c>StudentPortalEndpoints</c>), never
/// this one directly. Correcting an already-Published report card stays admin-gated on top of the
/// staff check, the same sensitivity level as <c>revise-approved</c> everywhere else this pattern appears.
/// </summary>
internal static class ReportCardEndpoints
{
    public static IEndpointRouteBuilder MapReportCardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reporting/report-cards").WithTags("ReportCards").RequireAuthorization();

        group.MapPost("/", async (
            PrepareReportCardHttpRequest request, IReportCardService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();
            if (user.PersonId is not { } preparedBy) return Results.Unauthorized();

            try
            {
                var id = await service.PrepareAsync(new PrepareReportCardRequest(
                    request.StudentPersonId, request.AcademicYearId, request.EvaluationPeriodId,
                    request.NarrativeEn, request.NarrativeDv, request.NextStepsEn, request.NextStepsDv, preparedBy), ct);
                return Results.Created($"/api/v1/reporting/report-cards/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/{reportCardId:guid}", async (Guid reportCardId, IReportCardService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            var reportCard = await service.GetAsync(reportCardId, ct);
            return reportCard is null ? Results.NotFound() : Results.Ok(reportCard);
        });

        group.MapGet("/{reportCardId:guid}/subject-results", async (Guid reportCardId, IReportCardService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            return Results.Ok(await service.GetSubjectResultsAsync(reportCardId, ct));
        });

        group.MapGet("/{reportCardId:guid}/key-competency-summaries", async (Guid reportCardId, IReportCardService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            return Results.Ok(await service.GetKeyCompetencySummariesAsync(reportCardId, ct));
        });

        group.MapGet("/{reportCardId:guid}/pdf", async (Guid reportCardId, IReportCardService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            try
            {
                var bytes = await service.RenderPdfAsync(reportCardId, ct);
                return Results.File(bytes, "application/pdf", "report-card.pdf");
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/", async (Guid studentPersonId, IReportCardService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            return Results.Ok(await service.GetPublishedForStudentAsync(studentPersonId, ct));
        });

        group.MapGet("/worklist", async (
            Guid gradeId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, IReportCardService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            return Results.Ok(await service.GetStudentsNeedingReportCardAsync(gradeId, academicYearId, evaluationPeriodId, asOf, ct));
        });

        group.MapPost("/{reportCardId:guid}/submit", async (
            Guid reportCardId, IReportCardService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            try
            {
                await service.SubmitAsync(reportCardId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ReportCard), reportCardId.ToString(), user.PersonId, "Report card submitted for review.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{reportCardId:guid}/begin-review", async (
            Guid reportCardId, IReportCardService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            try
            {
                await service.BeginReviewAsync(reportCardId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ReportCard), reportCardId.ToString(), user.PersonId, "Report card review began.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{reportCardId:guid}/approve", async (
            Guid reportCardId, IReportCardService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            try
            {
                await service.ApproveAsync(reportCardId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ReportCard), reportCardId.ToString(), user.PersonId, "Report card approved and published.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{reportCardId:guid}/reject", async (
            Guid reportCardId, IReportCardService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            try
            {
                await service.RejectAsync(reportCardId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ReportCard), reportCardId.ToString(), user.PersonId, "Report card rejected.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{reportCardId:guid}/return", async (
            Guid reportCardId, IReportCardService service, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!user.IsStaff) return Results.Forbid();

            try
            {
                await service.ReturnAsync(reportCardId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ReportCard), reportCardId.ToString(), user.PersonId, "Report card returned for correction.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/{reportCardId:guid}/revise-approved", async (
            Guid reportCardId, ReviseReportCardHttpRequest request, IReportCardService service,
            IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.ReviseApprovedReportCardAsync(
                    reportCardId, new ReviseReportCardRequest(request.NarrativeEn, request.NarrativeDv, request.NextStepsEn, request.NextStepsDv), ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(ReportCard), id.ToString(), user.PersonId, $"Approved report card revised (supersedes {reportCardId}).", cancellationToken: ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }
}

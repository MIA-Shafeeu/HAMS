using HAMS.CommunicationPortals.Application;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.CommunicationPortals.Endpoints;

public sealed record AcknowledgeRequest(string EntityType, string EntityId);

/// <summary>
/// The guardian portal's read surface (build plan Phase 10 scope: "published-only portal views").
/// Every route requires an authenticated principal whose JWT carries <c>hams:is_guardian=true</c> —
/// checked here, not just left to <c>RequireAuthorization()</c>, since a staff or student token is
/// also "authenticated" but must never reach a guardian-scoped read.
/// </summary>
internal static class GuardianPortalEndpoints
{
    public static IEndpointRouteBuilder MapGuardianPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/portal/guardian").WithTags("GuardianPortal").RequireAuthorization();

        group.MapGet("/students", async (IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            return Results.Ok(await portal.GetMyStudentsAsync(guardianPersonId, ct));
        });

        group.MapGet("/students/{studentPersonId:guid}/results", async (
            Guid studentPersonId, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                return Results.Ok(await portal.GetStudentResultsAsync(guardianPersonId, studentPersonId, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/students/{studentPersonId:guid}/attendance", async (
            Guid studentPersonId, DateOnly fromDate, DateOnly toDate, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                return Results.Ok(await portal.GetStudentAttendanceAsync(guardianPersonId, studentPersonId, fromDate, toDate, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/students/{studentPersonId:guid}/interventions", async (
            Guid studentPersonId, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                return Results.Ok(await portal.GetStudentInterventionUpdatesAsync(guardianPersonId, studentPersonId, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/students/{studentPersonId:guid}/report-cards", async (
            Guid studentPersonId, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                return Results.Ok(await portal.GetStudentReportCardsAsync(guardianPersonId, studentPersonId, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/students/{studentPersonId:guid}/report-cards/{reportCardId:guid}/pdf", async (
            Guid studentPersonId, Guid reportCardId, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                var bytes = await portal.GetStudentReportCardPdfAsync(guardianPersonId, studentPersonId, reportCardId, ct);
                return Results.File(bytes, "application/pdf", "report-card.pdf");
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        });

        group.MapGet("/students/{studentPersonId:guid}/behaviour", async (
            Guid studentPersonId, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                return Results.Ok(await portal.GetStudentBehaviourSummaryAsync(guardianPersonId, studentPersonId, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapPost("/students/{studentPersonId:guid}/acknowledgements", async (
            Guid studentPersonId, AcknowledgeRequest request, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                var id = await portal.AcknowledgeAsync(guardianPersonId, studentPersonId, request.EntityType, request.EntityId, ct);
                return Results.Ok(new { id });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/students/{studentPersonId:guid}/acknowledgements", async (
            Guid studentPersonId, string entityType, string entityId, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                var acknowledgement = await portal.GetAcknowledgementAsync(guardianPersonId, studentPersonId, entityType, entityId, ct);
                return Results.Ok(new { acknowledged = acknowledgement is not null, acknowledgedAtUtc = acknowledgement?.AcknowledgedAtUtc });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapGet("/students/{studentPersonId:guid}/homework", async (
            Guid studentPersonId, Guid academicYearId, IGuardianPortalService portal, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsGuardian || user.PersonId is not { } guardianPersonId) return Results.Forbid();

            try
            {
                return Results.Ok(await portal.GetStudentHomeworkAsync(guardianPersonId, studentPersonId, academicYearId, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        return endpoints;
    }
}

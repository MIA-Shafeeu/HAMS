using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.ReportingAnalyticsAudit.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.ReportingAnalyticsAudit.Endpoints;

/// <summary>Regulatory report download surface (build plan Phase 12 — "PDF/CSV/spreadsheet regulatory exports"). Plain GET endpoints so a browser navigation/download link works without any special client handling; admin-gated the same as the dashboard.</summary>
internal static class RegulatoryExportEndpoints
{
    public static IEndpointRouteBuilder MapRegulatoryExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reporting/exports").WithTags("RegulatoryExports").RequireAuthorization();

        group.MapGet("/student-roster", async (
            Guid academicYearId, ExportFormat format, IRegulatoryReportingService reports, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var export = await reports.ExportStudentRosterAsync(academicYearId, format, ct);
            return Results.File(export.Content, export.ContentType, export.FileName);
        });

        group.MapGet("/attendance-summary", async (
            Guid academicYearId, DateOnly fromDate, DateOnly toDate, ExportFormat format,
            IRegulatoryReportingService reports, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var export = await reports.ExportAttendanceSummaryAsync(academicYearId, fromDate, toDate, format, ct);
            return Results.File(export.Content, export.ContentType, export.FileName);
        });

        group.MapGet("/promotion-decisions", async (
            Guid academicYearId, ExportFormat format, IRegulatoryReportingService reports, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var export = await reports.ExportPromotionDecisionsAsync(academicYearId, format, ct);
            return Results.File(export.Content, export.ContentType, export.FileName);
        });

        return endpoints;
    }
}

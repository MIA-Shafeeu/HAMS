using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.ReportingAnalyticsAudit.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.ReportingAnalyticsAudit.Endpoints;

/// <summary>Management dashboard data surface (build plan Phase 12). Admin-gated: aggregate though these numbers are, they still summarize attendance/assessment/intervention activity across the whole school.</summary>
internal static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reporting/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("/academic-years", async (IDashboardQueryService dashboard, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            return Results.Ok(await dashboard.GetAvailableAcademicYearsAsync(ct));
        });

        group.MapGet("/snapshot", async (Guid academicYearId, IDashboardQueryService dashboard, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            return Results.Ok(await dashboard.GetSnapshotAsync(academicYearId, ct));
        });

        return endpoints;
    }
}

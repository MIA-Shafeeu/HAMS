using HAMS.Platform.Access;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.ReportingAnalyticsAudit.Endpoints;

/// <summary>Audit search surface (build plan Phase 12 — "search/export UI only, the write-path lives in Platform.Audit"). Admin-gated: the audit trail routinely includes other staff members' actions.</summary>
internal static class AuditLogEndpoints
{
    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reporting/audit-log").WithTags("AuditLog").RequireAuthorization();

        group.MapGet("/", async (
            DateTimeOffset? fromUtc, DateTimeOffset? toUtc, AuditAction? action, string? entityType, Guid? actorPersonId, string? searchText,
            int? page, int? pageSize, IAuditLogQuery query, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var result = await query.SearchAsync(
                new AuditLogSearchRequest(fromUtc, toUtc, action, entityType, actorPersonId, searchText, page ?? 1, pageSize ?? 50),
                ct);
            return Results.Ok(result);
        });

        group.MapGet("/entity-types", async (IAuditLogQuery query, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            return Results.Ok(await query.GetDistinctEntityTypesAsync(ct));
        });

        return endpoints;
    }
}

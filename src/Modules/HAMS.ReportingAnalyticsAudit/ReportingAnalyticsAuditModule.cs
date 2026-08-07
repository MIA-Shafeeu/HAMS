using HAMS.Platform.Audit.Infrastructure;
using HAMS.ReportingAnalyticsAudit.Application;
using HAMS.ReportingAnalyticsAudit.Endpoints;
using HAMS.ReportingAnalyticsAudit.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.ReportingAnalyticsAudit;

/// <summary>
/// Module registration entry point for the ReportingAnalyticsAudit module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.ReportingAnalyticsAudit`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class ReportingAnalyticsAuditModule
{
    /// <summary>Registers the "reporting" schema's <see cref="ReportingAnalyticsAuditDbContext"/> and Phase 11's report-card service — this module's first real functionality since Phase 0.</summary>
    public static IServiceCollection AddReportingAnalyticsAuditModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReportingAnalyticsAuditDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "reporting"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services.AddScoped<IReportCardService, ReportCardService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<IRegulatoryReportingService, RegulatoryReportingService>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapReportingAnalyticsAuditModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapReportCardEndpoints();
        endpoints.MapAuditLogEndpoints();
        endpoints.MapDashboardEndpoints();
        endpoints.MapRegulatoryExportEndpoints();
        return endpoints;
    }
}

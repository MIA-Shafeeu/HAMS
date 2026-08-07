using HAMS.CommunicationPortals.Application;
using HAMS.CommunicationPortals.Domain;
using HAMS.CommunicationPortals.Endpoints;
using HAMS.CommunicationPortals.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.CommunicationPortals;

/// <summary>
/// Module registration entry point for the CommunicationPortals module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.CommunicationPortals`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class CommunicationPortalsModule
{
    /// <summary>
    /// Registers Phase 10's guardian/student portal read services (pure read-orchestration over
    /// PeopleEnrollment/Attendance/AssessmentEvaluation/Intervention/LearningDelivery's own public
    /// Application contracts) plus Phase 13's "portals" schema — this module's first-ever owned
    /// data (<see cref="GuardianAcknowledgement"/>).
    /// </summary>
    public static IServiceCollection AddCommunicationPortalsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CommunicationPortalsDbContext>(options => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "portals")));

        services.AddScoped<IGuardianPortalService, GuardianPortalService>();
        services.AddScoped<IGuardianAcknowledgementService, GuardianAcknowledgementService>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapCommunicationPortalsModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGuardianPortalEndpoints();
        endpoints.MapStudentPortalEndpoints();
        return endpoints;
    }
}

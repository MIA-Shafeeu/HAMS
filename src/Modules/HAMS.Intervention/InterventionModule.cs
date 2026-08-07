using HAMS.Intervention.Application;
using HAMS.Intervention.Endpoints;
using HAMS.Intervention.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Intervention;

/// <summary>
/// Module registration entry point for the Intervention module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.Intervention`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class InterventionModule
{
    /// <summary>Registers the "intervention" schema's <see cref="InterventionDbContext"/> and Phase 9's application services.</summary>
    public static IServiceCollection AddInterventionModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InterventionDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "intervention")));

        services.AddScoped<IInterventionCaseService, InterventionCaseService>();
        services.AddScoped<ITopicClosureService, TopicClosureService>();
        services.AddScoped<IBehaviourIncidentService, BehaviourIncidentService>();
        services.AddScoped<IBehaviourCategoryLookup, BehaviourCategoryLookup>();
        services.AddScoped<IInterventionAdminService, InterventionAdminService>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapInterventionModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapInterventionCaseEndpoints();
        endpoints.MapTopicClosureEndpoints();
        endpoints.MapBehaviourIncidentEndpoints();
        endpoints.MapInterventionAdminEndpoints();
        return endpoints;
    }
}

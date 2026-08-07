using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.AdminIntegration;

/// <summary>
/// Module registration entry point for the AdminIntegration module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.AdminIntegration`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class AdminIntegrationModule
{
    /// <summary>Registers this module's Application/Infrastructure services. Empty until this module's phase begins.</summary>
    public static IServiceCollection AddAdminIntegrationModule(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint group. Empty until this module's phase begins.</summary>
    public static IEndpointRouteBuilder MapAdminIntegrationModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}

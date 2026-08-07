using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Fundraising;

/// <summary>
/// Module registration entry point for the Fundraising module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.Fundraising`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class FundraisingModule
{
    /// <summary>Registers this module's Application/Infrastructure services. Empty until this module's phase begins.</summary>
    public static IServiceCollection AddFundraisingModule(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint group. Empty until this module's phase begins.</summary>
    public static IEndpointRouteBuilder MapFundraisingModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}

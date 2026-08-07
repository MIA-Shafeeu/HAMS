using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Endpoints;
using HAMS.PeopleEnrollment.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.PeopleEnrollment;

/// <summary>
/// Module registration entry point for the PeopleEnrollment module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.PeopleEnrollment`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class PeopleEnrollmentModule
{
    /// <summary>Registers the "people" schema's <see cref="PeopleDbContext"/> and Phase 3's application services.</summary>
    public static IServiceCollection AddPeopleEnrollmentModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PeopleDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "people"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services.AddScoped<IPeopleAdminService, PeopleAdminService>();
        services.AddScoped<IGuardianRelationshipService, GuardianRelationshipService>();
        services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();
        services.AddScoped<IGuardianContactResolver, GuardianContactResolver>();
        services.AddScoped<IStudentProfileLookup, StudentProfileLookup>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint group.</summary>
    public static IEndpointRouteBuilder MapPeopleEnrollmentModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPeopleEndpoints();
        return endpoints;
    }
}

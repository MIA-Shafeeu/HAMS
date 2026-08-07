using HAMS.Attendance.Application;
using HAMS.Attendance.Endpoints;
using HAMS.Attendance.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Attendance;

/// <summary>
/// Module registration entry point for the Attendance module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.Attendance`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class AttendanceModule
{
    /// <summary>Registers the "attendance" schema's <see cref="AttendanceDbContext"/> and Phase 5's application services.</summary>
    public static IServiceCollection AddAttendanceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AttendanceDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "attendance"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceQueryService, AttendanceQueryService>();
        services.AddScoped<IAttendanceAdminService, AttendanceAdminService>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint group.</summary>
    public static IEndpointRouteBuilder MapAttendanceModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAttendanceEndpoints();
        return endpoints;
    }
}

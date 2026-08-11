using HAMS.Platform.Audit.Infrastructure;
using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Endpoints;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.TeachingTimetable;

/// <summary>
/// Module registration entry point for the TeachingTimetable module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.TeachingTimetable`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class TeachingTimetableModule
{
    /// <summary>Registers the "teaching" schema's <see cref="TeachingTimetableDbContext"/> and Phase 4's application services.</summary>
    public static IServiceCollection AddTeachingTimetableModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TeachingTimetableDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "teaching"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        // Registered against the concrete type too: SubstitutionService reuses
        // SubjectTeachingAssignmentService.AssignWithRoleAsync (internal) rather than duplicating
        // the assign+grant-projection path for the Substitute role.
        services.AddScoped<SubjectTeachingAssignmentService>();
        services.AddScoped<ISubjectTeachingAssignmentService>(sp => sp.GetRequiredService<SubjectTeachingAssignmentService>());

        services.AddScoped<IClassTeacherAssignmentService, ClassTeacherAssignmentService>();
        services.AddScoped<ILeadingTeacherAssignmentService, LeadingTeacherAssignmentService>();
        services.AddScoped<ISubstitutionService, SubstitutionService>();
        services.AddScoped<ITimetableService, TimetableService>();
        services.AddScoped<IPeriodAdminService, PeriodAdminService>();
        services.AddScoped<IStaffAccessScopeQuery, StaffAccessScopeQuery>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapTeachingTimetableModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAssignmentEndpoints();
        endpoints.MapTimetableEndpoints();
        return endpoints;
    }
}

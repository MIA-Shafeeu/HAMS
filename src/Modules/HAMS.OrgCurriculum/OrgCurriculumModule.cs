using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Endpoints;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.OrgCurriculum;

/// <summary>
/// Module registration entry point for the OrgCurriculum module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.OrgCurriculum`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class OrgCurriculumModule
{
    /// <summary>
    /// Registers the "org" schema's <see cref="OrgDbContext"/> and the Org Structure (Phase 1) +
    /// Curriculum &amp; Syllabus (Phase 2) application services.
    /// </summary>
    public static IServiceCollection AddOrgCurriculumModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrgDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "org"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services.AddScoped<IOrgAdminService, OrgAdminService>();
        services.AddScoped<ICurriculumAdminService, CurriculumAdminService>();
        services.AddScoped<IKeyStagePolicyResolver, KeyStagePolicyResolver>();
        services.AddScoped<IEvaluationModelLookup, EvaluationModelLookup>();
        services.AddScoped<ISubjectLookup, SubjectLookup>();
        services.AddScoped<IOrgStructureLookup, OrgStructureLookup>();

        services.AddScoped<ISyllabusPublishingService, SyllabusPublishingService>();
        services.AddScoped<ISyllabusResolver, SyllabusResolver>();
        services.AddScoped<ICurriculumCsvImportService, CurriculumCsvImportService>();
        services.AddScoped<ISchoolCalendarService, SchoolCalendarService>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapOrgCurriculumModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOrgEndpoints();
        endpoints.MapCurriculumEndpoints();
        return endpoints;
    }
}

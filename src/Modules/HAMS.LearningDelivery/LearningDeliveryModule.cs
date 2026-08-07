using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Endpoints;
using HAMS.LearningDelivery.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.LearningDelivery;

/// <summary>
/// Module registration entry point for the LearningDelivery module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.LearningDelivery`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class LearningDeliveryModule
{
    /// <summary>Registers the "learning" schema's <see cref="LearningDeliveryDbContext"/> and Phase 5's application services.</summary>
    public static IServiceCollection AddLearningDeliveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LearningDeliveryDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "learning"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services.AddScoped<ILessonSessionService, LessonSessionService>();
        services.AddScoped<ICoverageComparisonService, CoverageComparisonService>();
        services.AddScoped<IRecommendedLevelEngine, RecommendedLevelEngine>();
        services.AddScoped<IMasteryEvaluationService, MasteryEvaluationService>();
        services.AddScoped<ILearningEvidenceService, LearningEvidenceService>();
        services.AddScoped<IKeyCompetencyEvidenceService, KeyCompetencyEvidenceService>();
        services.AddScoped<IAchievementScaleQuery, AchievementScaleQuery>();
        services.AddScoped<ITeachingTopicQuery, TeachingTopicQuery>();
        services.AddScoped<IKeyCompetencyLookup, KeyCompetencyLookup>();
        services.AddScoped<IHomeworkService, HomeworkService>();
        services.AddScoped<IHomeworkSubmissionService, HomeworkSubmissionService>();
        services.AddScoped<ILessonPlanningService, LessonPlanningService>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapLearningDeliveryModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapLearningPlanEndpoints();
        endpoints.MapLessonSessionEndpoints();
        endpoints.MapMasteryEndpoints();
        endpoints.MapKeyCompetencyEndpoints();
        endpoints.MapHomeworkEndpoints();
        return endpoints;
    }
}

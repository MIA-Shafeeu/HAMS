using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Endpoints;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.AssessmentEvaluation;

/// <summary>
/// Module registration entry point for the AssessmentEvaluation module (see the build plan's Module Boundaries
/// table). `HAMS.WebHost`'s `Program.cs` is the only place this gets called — no other module
/// may reference `HAMS.AssessmentEvaluation`'s internals directly, only its public Application contracts (once
/// there are any) and this registration surface.
/// </summary>
public static class AssessmentEvaluationModule
{
    /// <summary>Registers the "assessment" schema's <see cref="AssessmentEvaluationDbContext"/> and Phases 7-8's application services.</summary>
    public static IServiceCollection AddAssessmentEvaluationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AssessmentEvaluationDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "assessment"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services.AddScoped<IAssessmentModerationService, AssessmentModerationService>();
        services.AddScoped<IAssessmentLookup, AssessmentLookup>();
        services.AddScoped<IAssessmentConfigAdminService, AssessmentConfigAdminService>();

        // Concrete registrations first (HybridEvaluationEngine composes Mastery+Assessment
        // directly, so both must be resolvable as themselves, not only as IEvaluationEngine), then
        // IEvaluationEngine factory delegates pointing at those same instances so
        // IKeyStageEvaluationService's IEnumerable<IEvaluationEngine> sees all three.
        services.AddScoped<MasteryEvaluationEngine>();
        services.AddScoped<AssessmentEvaluationEngine>();
        services.AddScoped<HybridEvaluationEngine>();
        services.AddScoped<IEvaluationEngine>(sp => sp.GetRequiredService<MasteryEvaluationEngine>());
        services.AddScoped<IEvaluationEngine>(sp => sp.GetRequiredService<AssessmentEvaluationEngine>());
        services.AddScoped<IEvaluationEngine>(sp => sp.GetRequiredService<HybridEvaluationEngine>());
        services.AddScoped<IKeyStageEvaluationService, KeyStageEvaluationService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IEvaluationPeriodLookup, EvaluationPeriodLookup>();

        return services;
    }

    /// <summary>Maps this module's minimal-API endpoint groups.</summary>
    public static IEndpointRouteBuilder MapAssessmentEvaluationModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAssessmentConfigEndpoints();
        endpoints.MapAssessmentResultEndpoints();
        endpoints.MapEvaluationEndpoints();
        endpoints.MapPromotionEndpoints();
        return endpoints;
    }
}

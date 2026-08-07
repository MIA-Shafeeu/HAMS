using HAMS.Platform.Access.Authorization;
using HAMS.Platform.Access.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Platform.Access;

/// <summary>
/// Registration entry point for the Access platform kernel. Every business module and
/// `HAMS.WebHost` may depend on this; per the plan's kernel design, this is reused rather than
/// re-implemented per-module.
/// </summary>
public static class PlatformAccessExtensions
{
    /// <summary>
    /// Registers the Access kernel: the "access" schema's <see cref="AccessDbContext"/>, the
    /// generic scope/confidentiality authorization handlers and policies (build plan §4), and the
    /// role-assignment/grant-projection services.
    /// </summary>
    public static IServiceCollection AddPlatformAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AccessDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "access"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        // Separate factory registration (alongside the scoped AddDbContext above, not instead of
        // it) purely for IRoleMembershipQuery/RoleMembershipQuery - see that class's own remarks
        // for why it can't share the ambient scoped AccessDbContext. Deliberately NOT the
        // AddDbContextFactory<T> extension method: it registers its own DbContextOptions<T> in the
        // container, which collides with AddDbContext<T>'s scoped one ("Cannot consume scoped
        // service DbContextOptions<AccessDbContext> from singleton IDbContextFactory<AccessDbContext>"
        // - caught by ASP.NET Core's DI scope validation before this ever reached production a
        // second time). A small hand-written factory building its own options sidesteps the
        // container entirely - nothing here asks DI for DbContextOptions<T>.
        services.AddSingleton<IDbContextFactory<AccessDbContext>>(_ => new AccessDbContextFactory(configuration));

        services.AddScoped<IAccessGrantProjectionService, AccessGrantProjectionService>();
        services.AddScoped<IPersonRoleAssignmentService, PersonRoleAssignmentService>();
        services.AddScoped<IConfidentialRecordAccessor, ConfidentialRecordAccessor>();
        services.AddScoped<IRoleMembershipQuery, RoleMembershipQuery>();
        services.AddScoped<IScopedAccessGrantProjector, ScopedAccessGrantProjector>();

        // Scoped, not singleton: both handlers depend on AccessDbContext (inherently scoped).
        services.AddScoped<IAuthorizationHandler, ScopeAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ConfidentialityAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PlatformAccessPolicies.Scope, policy => policy.Requirements.Add(ScopeRequirement.Instance))
            .AddPolicy(PlatformAccessPolicies.Confidentiality, policy => policy.Requirements.Add(ConfidentialityRequirement.Instance));

        return services;
    }
}

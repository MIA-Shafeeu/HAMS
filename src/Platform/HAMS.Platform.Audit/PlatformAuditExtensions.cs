using HAMS.Platform.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HAMS.Platform.Audit;

/// <summary>
/// Registration entry point for the Audit platform kernel. Every business module and
/// `HAMS.WebHost` may depend on this; per the plan's kernel design, this is reused rather than
/// re-implemented per-module.
/// </summary>
public static class PlatformAuditExtensions
{
    /// <summary>
    /// Registers the Audit kernel: the "audit" schema's own <see cref="AuditDbContext"/>, the
    /// <see cref="IAuditLogWriter"/> chokepoint every module writes through, and
    /// <see cref="SaveChangesGuardInterceptor"/> as a singleton so every module's own
    /// <c>AddDbContext</c> call can attach the same instance (it is stateless).
    /// </summary>
    public static IServiceCollection AddPlatformAudit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SaveChangesGuardInterceptor>();

        services.AddDbContext<AuditDbContext>((sp, options) => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "audit"))
            .AddInterceptors(sp.GetRequiredService<SaveChangesGuardInterceptor>()));

        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IAuditLogQuery, AuditLogQuery>();

        return services;
    }
}

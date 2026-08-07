using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HAMS.Platform.Notifications;

/// <summary>
/// Registration entry point for the Notifications platform kernel. Every business module and
/// `HAMS.WebHost`/`HAMS.Worker` may depend on this; per the plan's kernel design, this is reused
/// rather than re-implemented per-module. `HAMS.Worker` additionally needs this registered so its
/// recurring Hangfire job can resolve <see cref="INotificationDispatcher"/>.
/// </summary>
public static class PlatformNotificationsExtensions
{
    /// <summary>Registers the "notifications" schema's <see cref="NotificationsDbContext"/>, the outbox writer/dispatcher, and the log-only dev sender adapters.</summary>
    public static IServiceCollection AddPlatformNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options => options
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications")));

        services.AddScoped<INotificationOutboxWriter, NotificationOutboxWriter>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationAdminService, NotificationAdminService>();

        services.Configure<MsgowlOptions>(configuration.GetSection(MsgowlOptions.SectionName));
        services.AddScoped<LoggingSmsSender>();
        services.AddHttpClient<MsgowlSmsSender>((sp, client) =>
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<MsgowlOptions>>().Value.BaseUrl.TrimEnd('/') + "/"));

        // Which real ISmsSender implementation gets used is decided once, here — never silently,
        // per MsgowlOptions.Enabled's own remarks (a config section existing is not the same as
        // opting in to sending real messages).
        services.AddScoped<ISmsSender>(sp => sp.GetRequiredService<IOptions<MsgowlOptions>>().Value is { Enabled: true, ApiKey.Length: > 0 }
            ? sp.GetRequiredService<MsgowlSmsSender>()
            : sp.GetRequiredService<LoggingSmsSender>());

        services.AddScoped<IEmailSender, LoggingEmailSender>();

        return services;
    }
}

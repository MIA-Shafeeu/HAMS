using Hangfire;
using HAMS.Platform.Common;
using HAMS.Platform.Notifications;
using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Infrastructure;
using HAMS.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPlatformCommon();
builder.Services.AddPlatformNotifications(builder.Configuration);

// In-process Hangfire server sharing SQL Server storage with HAMS.WebHost (build plan §5) — this is
// the "second HAMS.Worker Windows Service" half of that split; drains the Notifications kernel's
// transactional outbox on a recurring schedule rather than sending anything synchronously in-request.
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new Hangfire.SqlServer.SqlServerStorageOptions { SchemaName = "hangfire" }));
builder.Services.AddHangfireServer();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();

    // Service-based API, not the static RecurringJob facade — the static facade relies on
    // JobStorage.Current, which the DI-registered AddHangfire(...) above never sets globally.
    scope.ServiceProvider.GetRequiredService<IRecurringJobManager>().AddOrUpdate<INotificationDispatcher>(
        "dispatch-pending-notifications", dispatcher => dispatcher.DispatchPendingAsync(CancellationToken.None), Cron.Minutely);
}

host.Run();

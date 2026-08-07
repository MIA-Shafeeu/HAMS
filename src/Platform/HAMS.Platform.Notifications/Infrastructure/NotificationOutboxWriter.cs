using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HAMS.Platform.Notifications.Infrastructure;

internal sealed class NotificationOutboxWriter(IClock clock) : INotificationOutboxWriter
{
    public async Task EnqueueManyAsync(
        DbContext sourceContext, Action stageSourceChanges, IReadOnlyList<OutboundNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        stageSourceChanges();

        await using var transaction = await sourceContext.Database.BeginTransactionAsync(cancellationToken);
        await sourceContext.SaveChangesAsync(cancellationToken);

        // Share the exact connection + transaction sourceContext is already using — one real SQL
        // Server transaction over one connection, not a distributed transaction (see
        // IScopedAccessGrantProjector, Platform.Access, for the identical established pattern).
        var notificationsOptions = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlServer(sourceContext.Database.GetDbConnection())
            .Options;
        await using var notificationsContext = new NotificationsDbContext(notificationsOptions);
        await notificationsContext.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);

        foreach (var notification in notifications)
        {
            var channel = await notificationsContext.NotificationChannels
                .SingleOrDefaultAsync(c => c.Code == notification.ChannelCode && c.IsActive, cancellationToken)
                ?? throw new InvalidOperationException($"No active notification channel with code '{notification.ChannelCode}'.");

            notificationsContext.NotificationOutboxEntries.Add(new NotificationOutboxEntry
            {
                Id = Guid.NewGuid(),
                ChannelId = channel.Id,
                Recipient = notification.Recipient,
                Subject = notification.Subject,
                Body = notification.Body,
                CreatedAtUtc = clock.UtcNow,
            });
        }

        await notificationsContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Domain;
using HAMS.Platform.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Notifications.Application;

internal sealed class NotificationDispatcher(
    NotificationsDbContext dbContext, ISmsSender smsSender, IEmailSender emailSender, IClock clock) : INotificationDispatcher
{
    private const int MaxAttemptsBeforeFailed = 5;
    private const int BatchSize = 50;

    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.NotificationOutboxEntries
            .Where(e => e.Status == NotificationDeliveryStatus.Pending)
            .OrderBy(e => e.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var entry in pending)
        {
            var channel = await dbContext.NotificationChannels.FindAsync([entry.ChannelId], cancellationToken);

            try
            {
                if (channel?.Code == NotificationChannelCodes.Sms)
                {
                    await smsSender.SendAsync(entry.Recipient, entry.Body, cancellationToken);
                }
                else if (channel?.Code == NotificationChannelCodes.Email)
                {
                    await emailSender.SendAsync(entry.Recipient, entry.Subject ?? string.Empty, entry.Body, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown or inactive notification channel '{channel?.Code}'.");
                }

                entry.Status = NotificationDeliveryStatus.Sent;
                entry.SentAtUtc = clock.UtcNow;
            }
            catch (Exception ex)
            {
                entry.AttemptCount++;
                entry.LastError = ex.Message;
                entry.Status = entry.AttemptCount >= MaxAttemptsBeforeFailed ? NotificationDeliveryStatus.Failed : NotificationDeliveryStatus.Pending;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

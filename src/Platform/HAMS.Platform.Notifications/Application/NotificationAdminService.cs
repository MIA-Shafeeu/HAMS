using HAMS.Platform.Notifications.Domain;
using HAMS.Platform.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Notifications.Application;

internal sealed class NotificationAdminService(NotificationsDbContext dbContext) : INotificationAdminService
{
    public async Task<IReadOnlyList<NotificationOutboxSummary>> GetEntriesAsync(
        NotificationDeliveryStatus? status, int take = 100, CancellationToken cancellationToken = default)
    {
        var query = dbContext.NotificationOutboxEntries.AsQueryable();
        if (status is { } filter)
        {
            query = query.Where(e => e.Status == filter);
        }

        // Order/Take on the raw entity BEFORE joining/projecting into the final record — ordering by
        // a property of an already-projected DTO doesn't translate against SQL Server (a mistake this
        // codebase has hit before with multi-join roster queries).
        return await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(take)
            .Join(dbContext.NotificationChannels, e => e.ChannelId, c => c.Id, (e, c) => new NotificationOutboxSummary(
                e.Id, c.Code, e.Recipient, e.Subject, e.Status, e.AttemptCount, e.LastError, e.CreatedAtUtc, e.SentAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task RetryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.NotificationOutboxEntries.FindAsync([entryId], cancellationToken)
            ?? throw new InvalidOperationException("Notification not found.");

        if (entry.Status != NotificationDeliveryStatus.Failed)
        {
            throw new InvalidOperationException($"Only a Failed notification can be retried (this one is {entry.Status}).");
        }

        entry.Status = NotificationDeliveryStatus.Pending;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

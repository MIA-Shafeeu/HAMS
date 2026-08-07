using HAMS.Platform.Notifications.Domain;

namespace HAMS.Platform.Notifications.Application;

public sealed record NotificationOutboxSummary(
    Guid Id, string ChannelCode, string Recipient, string? Subject, NotificationDeliveryStatus Status,
    int AttemptCount, string? LastError, DateTimeOffset CreatedAtUtc, DateTimeOffset? SentAtUtc);

/// <summary>
/// The Notification Outbox Monitor's whole read/retry surface (build plan Phase D — "view
/// pending/sent/failed, retry a failed one"). Deliberately thin: dispatch itself stays
/// <see cref="INotificationDispatcher"/>'s job, on its own Hangfire cadence — this is an
/// observability/recovery surface over that queue, not a second way to send anything.
/// </summary>
public interface INotificationAdminService
{
    Task<IReadOnlyList<NotificationOutboxSummary>> GetEntriesAsync(
        NotificationDeliveryStatus? status, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-queues a Failed entry by flipping it back to Pending — deliberately does NOT reset
    /// <see cref="NotificationOutboxEntry.AttemptCount"/>: the next dispatch attempt still counts
    /// against the same 5-attempt ceiling <see cref="INotificationDispatcher"/> already enforces, so
    /// a permanently-broken recipient (e.g. a disconnected phone number) can't be retried forever by
    /// repeatedly clicking this — it fails straight back to Failed on its very next attempt.
    /// </summary>
    /// <exception cref="InvalidOperationException">The entry is not currently Failed.</exception>
    Task RetryAsync(Guid entryId, CancellationToken cancellationToken = default);
}

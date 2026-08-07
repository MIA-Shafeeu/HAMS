namespace HAMS.Platform.Notifications.Domain;

/// <summary>
/// One queued outbound notification (build plan §2/§5: "a transactional Outbox table + Hangfire
/// dispatcher for anything crossing a process/external-system boundary"). Written transactionally
/// alongside the source module's own change via <c>INotificationOutboxWriter</c>, then drained by
/// <c>INotificationDispatcher</c> on a recurring Hangfire job — never sent synchronously in-request,
/// so a transient carrier/SMTP failure can never roll back or block the business write that
/// triggered it.
/// </summary>
public sealed class NotificationOutboxEntry
{
    public Guid Id { get; init; }

    public Guid ChannelId { get; init; }

    public required string Recipient { get; init; }

    public string? Subject { get; init; }

    public required string Body { get; init; }

    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? SentAtUtc { get; set; }
}

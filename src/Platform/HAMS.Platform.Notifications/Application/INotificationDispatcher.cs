namespace HAMS.Platform.Notifications.Application;

/// <summary>Drains Pending <c>NotificationOutboxEntry</c> rows — invoked from a recurring Hangfire job in <c>HAMS.Worker</c>, never from an inbound HTTP request.</summary>
public interface INotificationDispatcher
{
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}

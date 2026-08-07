using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Notifications.Application;

/// <summary>
/// The cross-module entry point for queuing outbound notifications atomically with the source
/// module's own write (build plan §2: "a transactional Outbox table ... for anything crossing a
/// process/external-system boundary"). Mirrors <c>IScopedAccessGrantProjector</c> (Platform.Access,
/// Phase 4) exactly, for the same reason: the source record lives in a different module's
/// <c>DbContext</c>/schema than <c>NotificationOutboxEntry</c>, so this runs both writes inside one
/// real SQL Server transaction shared over one connection — never a distributed transaction/MSDTC.
/// </summary>
public interface INotificationOutboxWriter
{
    /// <summary>
    /// Stages <paramref name="stageSourceChanges"/> on <paramref name="sourceContext"/> (call your
    /// own <c>Add</c>/property-mutations inside it, but do not call <c>SaveChangesAsync</c>
    /// yourself), then commits that together with <paramref name="notifications"/> queued as
    /// Pending outbox rows, in one transaction. Never sends anything itself — a recurring
    /// <c>INotificationDispatcher</c> job drains Pending rows separately, so a slow/failing
    /// carrier can never block or roll back the business write that triggered this.
    /// </summary>
    Task EnqueueManyAsync(
        DbContext sourceContext, Action stageSourceChanges, IReadOnlyList<OutboundNotification> notifications,
        CancellationToken cancellationToken = default);
}

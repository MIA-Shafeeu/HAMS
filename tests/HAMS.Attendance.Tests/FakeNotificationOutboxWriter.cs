using HAMS.Platform.Notifications.Application;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Tests;

/// <summary>
/// Records what would have been enqueued instead of opening a real cross-context SQL transaction —
/// EF Core InMemory can't exercise <c>Database.BeginTransactionAsync</c> (same reason
/// TeachingTimetable.Tests fakes <c>IScopedAccessGrantProjector</c>); the real writer's transactional
/// correctness is verified live against SQL Server instead.
/// </summary>
internal sealed class FakeNotificationOutboxWriter : INotificationOutboxWriter
{
    public List<OutboundNotification> Enqueued { get; } = [];

    public async Task EnqueueManyAsync(
        DbContext sourceContext, Action stageSourceChanges, IReadOnlyList<OutboundNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        stageSourceChanges();
        Enqueued.AddRange(notifications);
        await sourceContext.SaveChangesAsync(cancellationToken);
    }
}

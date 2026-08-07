using HAMS.Platform.Notifications.Application;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Tests;

/// <summary>Records what would have been enqueued instead of opening a real cross-context SQL transaction — mirrors HAMS.Attendance.Tests' identical fake (EF Core InMemory can't exercise <c>Database.BeginTransactionAsync</c>).</summary>
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

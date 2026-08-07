namespace HAMS.Platform.Notifications.Domain;

/// <summary>
/// A structural delivery-pipeline state, not business/reference data — same exception as
/// <c>RecordStatus</c>/<c>LessonSessionStatus</c>: "only Pending entries get dispatched, only
/// Failed entries stop retrying" is a code-branching rule regardless of storage, and renaming a
/// state wouldn't change what it means to the dispatcher.
/// </summary>
public enum NotificationDeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
}

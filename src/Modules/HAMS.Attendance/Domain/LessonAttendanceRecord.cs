namespace HAMS.Attendance.Domain;

/// <summary>
/// Per-period attendance mark, one row per student per <c>LessonSession</c> (loose reference into
/// LearningDelivery's "learning" schema — a lesson session's own creation is what enforces it
/// falls on a real school day, so this doesn't re-check the calendar itself).
/// </summary>
public sealed class LessonAttendanceRecord
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public Guid LessonSessionId { get; init; }

    public Guid AttendanceStatusId { get; set; }

    public Guid RecordedByPersonId { get; set; }

    public string? Notes { get; set; }
}

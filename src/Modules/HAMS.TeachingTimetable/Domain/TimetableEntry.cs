namespace HAMS.TeachingTimetable.Domain;

/// <summary>
/// One weekly recurring slot: this class has this subject, taught by whoever
/// <see cref="TeachingAssignmentId"/> resolves to, on this day, in this period. Placed and
/// conflict-checked by <c>ITimetableService</c> (build plan §4: "conflict checks" — a class can't
/// have two subjects in the same slot, and a staff member can't teach two classes in the same
/// slot). <see cref="DayOfWeek"/> is the BCL enum deliberately — the seven-day week is a
/// structural calendar fact, not admin-configurable business data, so it's exempt from the
/// no-enums principle the same way <c>RecordStatus</c> is.
/// </summary>
public sealed class TimetableEntry
{
    public Guid Id { get; init; }

    public Guid ClassId { get; init; }

    public Guid SubjectId { get; init; }

    public Guid TeachingAssignmentId { get; init; }

    public Guid AcademicYearId { get; init; }

    public DayOfWeek DayOfWeek { get; init; }

    public Guid PeriodId { get; init; }
}

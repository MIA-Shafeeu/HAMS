namespace HAMS.Attendance.Domain;

/// <summary>
/// One whole-day attendance mark per student per date. Only ever created/updated through
/// <c>IAttendanceService</c>, which rejects any date that isn't a real school day (build plan,
/// per explicit user instruction) — a school's configured working days AND-ed with its declared
/// holidays, both resolved via OrgCurriculum's <c>ISchoolCalendarService</c>.
/// </summary>
public sealed class DailyAttendanceRecord
{
    public Guid Id { get; init; }

    public Guid StudentPersonId { get; init; }

    public DateOnly Date { get; init; }

    public Guid AcademicYearId { get; init; }

    public Guid AttendanceStatusId { get; set; }

    public Guid RecordedByPersonId { get; set; }

    public string? Notes { get; set; }
}

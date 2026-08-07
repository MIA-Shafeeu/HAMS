namespace HAMS.TeachingTimetable.Domain;

/// <summary>A school-configurable timetable period (e.g. "Period 1", 08:00-08:40) — schools set their own period structure, so this is data, not an enum.</summary>
public sealed class Period
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// A purely structural lifecycle (build plan §3) — a genuine C# enum by the same exception as
/// <c>RecordStatus</c>: "Archived" always means the same thing to the code (no new enrolments,
/// no further mutation) regardless of school configuration.
/// </summary>
public enum AcademicYearStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2,
    Archived = 3,
}

public sealed class AcademicYear
{
    public Guid Id { get; init; }

    public Guid SchoolId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public AcademicYearStatus Status { get; set; } = AcademicYearStatus.Draft;
}

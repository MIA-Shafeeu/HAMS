using HAMS.Platform.Common.Contracts;

namespace HAMS.Attendance.Domain;

/// <summary>Configurable lookup (build plan §1.6 explicitly names "AttendanceStatus" as an example), not an enum.</summary>
public sealed class AttendanceStatus : ISimpleLookup
{
    public Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class AttendanceStatusCodes
{
    public const string Present = "PRESENT";
    public const string Absent = "ABSENT";
    public const string Late = "LATE";
    public const string Excused = "EXCUSED";
}

using HAMS.Attendance.Domain;

namespace HAMS.Attendance.Infrastructure;

/// <summary>Fixed reference data seeded via EF Core migrations — see <c>AccessSeedData</c> for the same pattern.</summary>
internal static class AttendanceSeedData
{
    public static readonly AttendanceStatus[] AttendanceStatuses =
    [
        new() { Id = new Guid("00000000-0000-0000-0015-000000000001"), Code = AttendanceStatusCodes.Present, Name = "Present", DisplayOrder = 1 },
        new() { Id = new Guid("00000000-0000-0000-0015-000000000002"), Code = AttendanceStatusCodes.Absent, Name = "Absent", DisplayOrder = 2 },
        new() { Id = new Guid("00000000-0000-0000-0015-000000000003"), Code = AttendanceStatusCodes.Late, Name = "Late", DisplayOrder = 3 },
        new() { Id = new Guid("00000000-0000-0000-0015-000000000004"), Code = AttendanceStatusCodes.Excused, Name = "Excused", DisplayOrder = 4 },
    ];
}

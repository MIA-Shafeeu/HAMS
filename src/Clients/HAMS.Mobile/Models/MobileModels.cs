namespace HAMS.Mobile.Models;

/// <summary>Minimal client-side read/write shapes for the mobile app's own screens — kept local rather than in HAMS.SharedContracts for the same reason the WASM portal's own display DTOs are: these mirror one endpoint's exact JSON shape, not a wire contract multiple servers/clients must agree on.</summary>
public sealed record SchoolRef(Guid Id, string Name);

public sealed record AcademicYearRef(Guid Id, string Name);

public sealed record ClassRef(Guid Id, string Name);

public sealed record StaffTimetableEntry(
    Guid Id, Guid ClassId, string SubjectName, string ClassName, DayOfWeek DayOfWeek, string PeriodName,
    TimeOnly PeriodStartTime, TimeOnly PeriodEndTime);

public sealed record ClassRosterEntry(Guid StudentPersonId, string NameEn, string NameDv, string AdmissionNumber);

public sealed record AttendanceStatusOption(Guid Id, string Code, string Name, int DisplayOrder, bool IsActive);

public sealed record MarkDailyAttendanceRequest(Guid SchoolId, Guid StudentPersonId, DateOnly Date, Guid AcademicYearId, string AttendanceStatusCode, string? Notes);

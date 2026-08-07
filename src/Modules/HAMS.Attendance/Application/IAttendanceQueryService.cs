namespace HAMS.Attendance.Application;

/// <summary>Portal-safe attendance projection (Phase 10) — resolves the status's own <c>Code</c> so a caller outside this module's boundary never needs its own <c>AttendanceDbContext</c> reference just to show a human-readable status.</summary>
public sealed record AttendanceRecordSummary(DateOnly Date, string AttendanceStatusCode, string? Notes);

/// <summary>
/// The one public, cross-module read surface over daily attendance (Phase 10) — until now, the only
/// reads of <c>DailyAttendanceRecord</c> were inline <c>AttendanceDbContext</c> queries inside this
/// module's own endpoints, which no other module may reach into (build plan §2). Deliberately
/// daily-only: lesson-level attendance detail is a staff/timetable concern, not something a
/// guardian/student portal needs to see per-period.
/// </summary>
public sealed record AttendanceStatusOption(Guid Id, string Code, string Name);

public interface IAttendanceQueryService
{
    Task<IReadOnlyList<AttendanceRecordSummary>> GetDailyRecordsAsync(
        Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    /// <summary>The configurable status set (build plan §1.6) for staff-facing marking UI — the same rows <c>AttendanceEndpoints</c>' <c>GET /statuses</c> already serves, now DI-injectable for Blazor pages.</summary>
    Task<IReadOnlyList<AttendanceStatusOption>> GetStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Every daily record already on file, for a caller-resolved set of students, on one date — lets
    /// a marking UI show what's already been taken instead of assuming a blank slate. Takes an
    /// explicit student id list rather than a <c>classId</c> deliberately: this module has no notion
    /// of "class" at all (only <see cref="Domain.DailyAttendanceRecord.StudentPersonId"/>/<c>Date</c>),
    /// and resolving a class roster is PeopleEnrollment's job (<c>GetActiveRosterForClassAsync</c>) —
    /// the caller (a Blazor page) composes the two, keeping each module's own scope intact.
    /// </summary>
    Task<IReadOnlyList<(Guid StudentPersonId, string AttendanceStatusCode)>> GetDailyRecordsForStudentsAsync(
        IReadOnlyList<Guid> studentPersonIds, DateOnly date, CancellationToken cancellationToken = default);
}

namespace HAMS.ReportingAnalyticsAudit.Domain.Views;

/// <summary>One daily attendance mark with the student's name and status name already resolved — backed by <c>reporting.vw_AttendanceRecords</c> (see <see cref="StudentRosterRow"/>'s remarks on the cross-schema view exception). Keyless EF Core query type.</summary>
public sealed class AttendanceRecordRow
{
    public Guid RecordId { get; init; }
    public Guid StudentPersonId { get; init; }
    public required string StudentNameEn { get; init; }
    public string? StudentNameDv { get; init; }
    public DateOnly Date { get; init; }
    public Guid AcademicYearId { get; init; }
    public Guid AttendanceStatusId { get; init; }
    public required string AttendanceStatusCode { get; init; }
    public required string AttendanceStatusName { get; init; }
}

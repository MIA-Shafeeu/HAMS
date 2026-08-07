namespace HAMS.ReportingAnalyticsAudit.Domain.Views;

/// <summary>
/// One currently-active enrolment, with names/codes already resolved — backed by the read-only
/// cross-schema SQL view <c>reporting.vw_StudentRoster</c> (build plan §2's explicit exception:
/// "ReportingAnalyticsAudit gets read-only cross-schema SQL views for dashboards/regulatory
/// reports — that's its entire job — but it never writes outside its own schema"). A keyless EF
/// Core query type, never inserted/updated/deleted through this module.
/// </summary>
public sealed class StudentRosterRow
{
    public Guid EnrollmentId { get; init; }
    public Guid StudentPersonId { get; init; }
    public required string AdmissionNumber { get; init; }
    public required string StudentNameEn { get; init; }
    public string? StudentNameDv { get; init; }
    public Guid AcademicYearId { get; init; }
    public required string AcademicYearCode { get; init; }
    public required string AcademicYearName { get; init; }
    public Guid GradeId { get; init; }
    public required string GradeCode { get; init; }
    public required string GradeName { get; init; }
    public Guid ClassId { get; init; }
    public required string ClassCode { get; init; }
    public required string ClassName { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
}

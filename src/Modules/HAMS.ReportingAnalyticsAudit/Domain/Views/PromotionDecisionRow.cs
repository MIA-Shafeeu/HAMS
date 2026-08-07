namespace HAMS.ReportingAnalyticsAudit.Domain.Views;

/// <summary>One recorded promotion/progression decision with names resolved — backed by <c>reporting.vw_PromotionDecisions</c> (see <see cref="StudentRosterRow"/>'s remarks on the cross-schema view exception). Keyless EF Core query type.</summary>
public sealed class PromotionDecisionRow
{
    public Guid DecisionId { get; init; }
    public Guid StudentPersonId { get; init; }
    public required string StudentNameEn { get; init; }
    public string? StudentNameDv { get; init; }
    public Guid AcademicYearId { get; init; }
    public required string AcademicYearCode { get; init; }
    public Guid CurrentGradeId { get; init; }
    public required string CurrentGradeName { get; init; }
    public bool Promoted { get; init; }
    public Guid? NextGradeId { get; init; }
    public string? NextGradeName { get; init; }
    public DateOnly DecisionDate { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; }
}

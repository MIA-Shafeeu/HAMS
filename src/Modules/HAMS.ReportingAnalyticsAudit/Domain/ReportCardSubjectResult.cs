namespace HAMS.ReportingAnalyticsAudit.Domain;

/// <summary>
/// A snapshot of one subject's official evaluation at the moment a <see cref="ReportCard"/> was
/// prepared — copies <c>KeyStageEvaluation</c>'s own result fields rather than referencing them
/// live, so this report card's content can never silently change if a later re-evaluation, a
/// subject rename, or a scale/band edit happens afterward. <see cref="SourceKeyStageEvaluationId"/>
/// is kept purely for provenance/audit traceability, never re-read to answer "what did the report
/// card say."
/// </summary>
public sealed class ReportCardSubjectResult
{
    public Guid Id { get; init; }

    public Guid ReportCardId { get; init; }

    public Guid SubjectId { get; init; }

    public Guid SourceKeyStageEvaluationId { get; init; }

    public Guid? AchievementLevelId { get; init; }

    public decimal? Percentage { get; init; }

    public Guid? GradeBandId { get; init; }
}

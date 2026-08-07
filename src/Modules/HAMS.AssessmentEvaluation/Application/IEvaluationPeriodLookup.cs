namespace HAMS.AssessmentEvaluation.Application;

public sealed record EvaluationPeriodWindow(DateOnly StartDate, DateOnly EndDate);

/// <summary>The one small read a report card (Phase 11, ReportingAnalyticsAudit) needs to resolve an evaluation period's own date window — reused to scope the Key Competency evidence summary to the same reporting window, rather than asking the caller to redundantly supply dates <c>EvaluationPeriod</c> already stores.</summary>
public interface IEvaluationPeriodLookup
{
    /// <returns>Null if the period doesn't exist.</returns>
    Task<EvaluationPeriodWindow?> GetWindowAsync(Guid evaluationPeriodId, CancellationToken cancellationToken = default);
}

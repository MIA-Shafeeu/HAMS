using HAMS.AssessmentEvaluation.Domain;

namespace HAMS.AssessmentEvaluation.Application;

public sealed record AssessmentOption(Guid Id, string Title, decimal MaxMarks, DateOnly ScheduledDate);

/// <summary>
/// Read-only listing for staff-facing marks-entry UI — the same "which assessment" picker
/// <c>AssessmentConfigEndpoints</c>' <c>GET /assessments</c> already serves inline, now
/// DI-injectable for a Blazor page (the config CRUD itself stays admin-only and endpoint-inline;
/// this is purely an additive read path, same precedent as <c>IOrgStructureLookup</c>).
/// </summary>
public interface IAssessmentLookup
{
    Task<IReadOnlyList<AssessmentOption>> GetAssessmentsAsync(
        Guid subjectId, Guid gradeId, Guid termId, CancellationToken cancellationToken = default);

    /// <summary>Current (non-superseded) result rows for one assessment — the same filter <c>AssessmentResultEndpoints</c>' <c>GET /results</c> already applies inline.</summary>
    Task<IReadOnlyList<AssessmentResult>> GetResultsForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default);

    /// <summary>Resolves an <see cref="Assessment"/> by its own id — a Staff page re-authorizing a moderation transition needs this to learn a result's <c>GradeId</c>/<c>SubjectId</c>, neither of which live on <see cref="AssessmentResult"/> itself.</summary>
    Task<Assessment?> GetAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default);
}

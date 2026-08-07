using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Application;

internal sealed class AssessmentLookup(AssessmentEvaluationDbContext dbContext) : IAssessmentLookup
{
    public async Task<IReadOnlyList<AssessmentOption>> GetAssessmentsAsync(
        Guid subjectId, Guid gradeId, Guid termId, CancellationToken cancellationToken = default) =>
        await dbContext.Assessments
            .Where(a => a.SubjectId == subjectId && a.GradeId == gradeId && a.TermId == termId)
            .OrderBy(a => a.ScheduledDate)
            .Select(a => new AssessmentOption(a.Id, a.Title, a.MaxMarks, a.ScheduledDate))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AssessmentResult>> GetResultsForAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default) =>
        await dbContext.AssessmentResults.Where(r => r.AssessmentId == assessmentId && r.IsCurrent).ToListAsync(cancellationToken);
}

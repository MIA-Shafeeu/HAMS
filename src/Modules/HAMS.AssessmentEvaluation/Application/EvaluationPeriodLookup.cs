using HAMS.AssessmentEvaluation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Application;

internal sealed class EvaluationPeriodLookup(AssessmentEvaluationDbContext dbContext) : IEvaluationPeriodLookup
{
    public async Task<EvaluationPeriodWindow?> GetWindowAsync(Guid evaluationPeriodId, CancellationToken cancellationToken = default)
        => await dbContext.EvaluationPeriods
            .Where(p => p.Id == evaluationPeriodId)
            .Select(p => new EvaluationPeriodWindow(p.StartDate, p.EndDate))
            .SingleOrDefaultAsync(cancellationToken);
}

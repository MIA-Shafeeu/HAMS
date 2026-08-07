using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class EvaluationModelLookup(OrgDbContext dbContext) : IEvaluationModelLookup
{
    public Task<EvaluationModel?> GetByIdAsync(Guid evaluationModelId, CancellationToken cancellationToken = default)
        => dbContext.EvaluationModels.SingleOrDefaultAsync(m => m.Id == evaluationModelId, cancellationToken);
}

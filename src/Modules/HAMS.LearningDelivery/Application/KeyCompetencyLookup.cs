using HAMS.LearningDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.LearningDelivery.Application;

internal sealed class KeyCompetencyLookup(LearningDeliveryDbContext dbContext) : IKeyCompetencyLookup
{
    public async Task<IReadOnlyList<KeyCompetencyName>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.KeyCompetencies
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new KeyCompetencyName(c.Id, c.NameEn, c.NameDv))
            .ToListAsync(cancellationToken);
}

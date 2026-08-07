using HAMS.Intervention.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Application;

internal sealed class BehaviourCategoryLookup(InterventionDbContext dbContext) : IBehaviourCategoryLookup
{
    public async Task<BehaviourCategoryInfo?> GetAsync(Guid behaviourCategoryId, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.BehaviourCategories.SingleOrDefaultAsync(c => c.Id == behaviourCategoryId, cancellationToken);
        return category is null ? null : new BehaviourCategoryInfo(category.Name, category.IsPositive);
    }

    public async Task<IReadOnlyList<BehaviourCategoryOption>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.BehaviourCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder)
            .Select(c => new BehaviourCategoryOption(c.Id, c.Code, c.Name, c.IsPositive)).ToListAsync(cancellationToken);
}

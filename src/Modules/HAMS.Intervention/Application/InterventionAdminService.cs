using HAMS.Intervention.Domain;
using HAMS.Intervention.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Application;

internal sealed class InterventionAdminService(InterventionDbContext dbContext) : IInterventionAdminService
{
    public async Task<Guid> CreateBehaviourCategoryAsync(string code, string name, bool isPositive, int displayOrder, CancellationToken cancellationToken = default)
    {
        var category = new BehaviourCategory { Id = Guid.NewGuid(), Code = code, Name = name, IsPositive = isPositive, DisplayOrder = displayOrder };
        dbContext.BehaviourCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    public async Task<IReadOnlyList<BehaviourCategory>> GetBehaviourCategoriesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.BehaviourCategories.OrderBy(c => c.DisplayOrder).ToListAsync(cancellationToken);

    public async Task SetBehaviourCategoryActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.BehaviourCategories.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Behaviour category not found.");

        category.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBehaviourCategoryAsync(Guid id, string name, bool isPositive, int displayOrder, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.BehaviourCategories.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Behaviour category not found.");

        category.Name = name;
        category.IsPositive = isPositive;
        category.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateInterventionTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var type = new InterventionType { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.InterventionTypes.Add(type);
        await dbContext.SaveChangesAsync(cancellationToken);
        return type.Id;
    }

    public async Task<IReadOnlyList<InterventionType>> GetInterventionTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.InterventionTypes.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

    public async Task SetInterventionTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var type = await dbContext.InterventionTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Intervention type not found.");

        type.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateInterventionTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var type = await dbContext.InterventionTypes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Intervention type not found.");

        type.Name = name;
        type.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

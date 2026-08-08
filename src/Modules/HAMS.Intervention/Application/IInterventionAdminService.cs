using HAMS.Intervention.Domain;

namespace HAMS.Intervention.Application;

/// <summary>
/// Behaviour-category / intervention-type admin surface (build plan §1.6 configurable-lookup rule) —
/// extracted from what had been inline <c>InterventionDbContext</c> queries buried inside
/// <c>BehaviourIncidentEndpoints</c>/<c>InterventionCaseEndpoints</c>' create handlers, the same
/// extraction already done for <c>IOrgAdminService</c>/<c>IPeopleAdminService</c>. <see cref="IBehaviourCategoryLookup"/>
/// already exposes a narrow, active-only, cross-module-safe read of <see cref="BehaviourCategory"/> —
/// deliberately not reused here since that one is a portal read contract (build plan §2), not an admin
/// CRUD surface. No equivalent lookup exists yet for <see cref="InterventionType"/>.
/// </summary>
public interface IInterventionAdminService
{
    Task<Guid> CreateBehaviourCategoryAsync(string code, string name, bool isPositive, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BehaviourCategory>> GetBehaviourCategoriesAsync(CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No behaviour category with that id exists.</exception>
    Task SetBehaviourCategoryActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No behaviour category with that id exists.</exception>
    Task UpdateBehaviourCategoryAsync(Guid id, string name, bool isPositive, int displayOrder, CancellationToken cancellationToken = default);

    Task<Guid> CreateInterventionTypeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterventionType>> GetInterventionTypesAsync(CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No intervention type with that id exists.</exception>
    Task SetInterventionTypeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No intervention type with that id exists.</exception>
    Task UpdateInterventionTypeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);
}

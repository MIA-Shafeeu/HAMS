using System.Linq.Expressions;

namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// The one <c>.ActiveAsOf(date)</c> query extension used everywhere an effective-dated row needs
/// to be resolved as of a point in time — this is a performance-critical hot path (it underlies
/// the Access-Scope kernel's permission checks on nearly every request), not just a modeling
/// nicety, so every module should use this rather than re-writing the date-range predicate
/// inline. Callers are expected to index <c>(EffectiveFrom, EffectiveTo)</c> alongside whatever
/// scope columns a given table also filters on.
/// </summary>
public static class EffectiveDatedQueryExtensions
{
    /// <summary>
    /// Filters an <see cref="IQueryable{T}"/> to rows active as of <paramref name="asOf"/>.
    /// Translates directly to SQL — safe to use in EF Core LINQ queries, not just in-memory.
    /// </summary>
    public static IQueryable<T> ActiveAsOf<T>(this IQueryable<T> source, DateOnly asOf)
        where T : IEffectiveDated
    {
        Expression<Func<T, bool>> predicate = x =>
            x.EffectiveFrom <= asOf && (x.EffectiveTo == null || x.EffectiveTo >= asOf);

        return source.Where(predicate);
    }

    /// <summary>In-memory equivalent for a single already-loaded instance.</summary>
    public static bool IsActiveAsOf(this IEffectiveDated entity, DateOnly asOf)
        => entity.EffectiveFrom <= asOf && (entity.EffectiveTo is null || entity.EffectiveTo >= asOf);
}

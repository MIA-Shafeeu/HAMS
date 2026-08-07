using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Audit.Infrastructure;

/// <summary>
/// Thrown by <see cref="SaveChangesGuardInterceptor"/> when application code attempts to
/// <c>Update</c> or <c>Remove</c> a row that had already reached an immutable status (build plan
/// §3: "an EF Core <c>SaveChanges</c> interceptor throws if anything else attempts
/// Update/Remove on a Published/Locked entity"). The only sanctioned way to change such a row is
/// a future <c>CorrectionService&lt;T&gt;</c> inserting a new version and re-pointing
/// <c>SupersedesId</c>/<c>SupersededById</c> — nothing in Phase 1 needs that path yet.
/// </summary>
public sealed class ImmutableRecordMutationException(string entityTypeName, EntityState attemptedState)
    : InvalidOperationException(
        $"Cannot {attemptedState} a {entityTypeName} row that is already immutable (Published/Locked). " +
        "Insert a new version and supersede it instead.")
{
    public string EntityTypeName { get; } = entityTypeName;
    public EntityState AttemptedState { get; } = attemptedState;
}

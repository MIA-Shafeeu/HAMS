using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HAMS.Platform.Audit.Infrastructure;

/// <summary>
/// Registered once (Platform.Audit) and attached to every module's <c>DbContext</c> so the
/// never-mutate-published rule (build plan §3) is enforced generically, without each module
/// needing to remember to check it. Recognises any entity implementing
/// <see cref="IVersionedRecord"/> regardless of the module or primary-key type it uses.
///
/// Checks the row's <em>original</em> (pre-change) immutability, not its current in-memory state —
/// this is deliberate: the one legitimate transition (e.g. Draft -&gt; Published) is itself a
/// <c>Modified</c> save where the original row was not yet immutable, so it must be allowed. Only
/// a change to a row that was <em>already</em> immutable before this <c>SaveChanges</c> call is
/// blocked — unless <see cref="ImmutableRecordCorrectionScope.IsInScope"/>, the one narrow,
/// explicit escape hatch for superseding an immutable row via a new version.
/// </summary>
public sealed class SaveChangesGuardInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Guard(DbContext? context)
    {
        if (context is null || ImmutableRecordCorrectionScope.IsInScope)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Entity is not IVersionedRecord)
            {
                continue;
            }

            var originalState = (IVersionedRecord)entry.OriginalValues.ToObject();
            if (originalState.IsImmutable)
            {
                throw new ImmutableRecordMutationException(entry.Entity.GetType().Name, entry.State);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Access;

/// <summary>
/// The cross-module entry point for projecting a scoped source-table change (e.g. a new teaching
/// assignment) into <c>AccessGrant</c>, atomically with the source module's own write — build plan
/// §4: "upserted synchronously in the same transaction as whatever source table changed." Unlike
/// <see cref="IAccessGrantProjectionService"/> (Phase 1's role-grant projector, which lives in the
/// same "access" schema/DbContext as <c>AccessGrant</c> so one <c>SaveChanges</c> call covers
/// both), a teaching assignment lives in a *different* module's <c>DbContext</c>/schema — this
/// runs both writes inside one real SQL Server transaction shared over one connection instead
/// (never a distributed transaction/MSDTC, since every module lives in the same physical
/// database, build plan §2 — that would contradict the plan's own operational-simplicity
/// principle for a solo-developer, no-dedicated-ops deployment).
/// </summary>
public interface IScopedAccessGrantProjector
{
    /// <summary>
    /// Stages <paramref name="stageSourceChanges"/> on <paramref name="sourceContext"/> (call your
    /// own <c>Add</c>/property-mutations inside it, but do not call <c>SaveChangesAsync</c>
    /// yourself), then commits that together with the upserted <see cref="ScopedAccessGrant"/> in
    /// one transaction. Upsert semantics match <c>IAccessGrantProjectionService</c>: a second call
    /// for the same <c>SourceType</c>/<c>SourceId</c> updates the existing grant's
    /// <c>EffectiveTo</c> rather than inserting a duplicate.
    /// </summary>
    Task ProjectAsync(
        DbContext sourceContext, Action stageSourceChanges, ScopedAccessGrant grant, CancellationToken cancellationToken = default);
}

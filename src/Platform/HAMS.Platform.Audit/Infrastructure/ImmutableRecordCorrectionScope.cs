namespace HAMS.Platform.Audit.Infrastructure;

/// <summary>
/// The one sanctioned way to touch an already-Published/Locked <see cref="Common.Contracts.IVersionedRecord"/>
/// row (build plan §3: "a generic <c>CorrectionService&lt;T&gt;</c> is the only code path allowed
/// to touch a Published/Locked row — it inserts a new version and flips pointers"). Deliberately
/// narrow: this only suspends <see cref="SaveChangesGuardInterceptor"/>'s guard for the duration of
/// the scope — callers are trusted to only ever flip the superseding pointer fields
/// (<c>IsCurrent</c>/<c>Status</c>/<c>SupersededById</c>) inside it, not make arbitrary edits.
/// First used by <c>ISyllabusPublishingService</c> (Phase 2); intended to become the primitive a
/// future generic <c>CorrectionService&lt;T&gt;</c> builds on once more than one caller needs it.
/// </summary>
public static class ImmutableRecordCorrectionScope
{
    private static readonly AsyncLocal<bool> Active = new();

    public static bool IsInScope => Active.Value;

    public static IDisposable Enter()
    {
        Active.Value = true;
        return new ScopeHandle();
    }

    private sealed class ScopeHandle : IDisposable
    {
        public void Dispose() => Active.Value = false;
    }
}

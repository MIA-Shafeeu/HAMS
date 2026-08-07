namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// Non-generic root of <see cref="IVersionedRecord{TKey}"/>, split out so
/// <c>SaveChangesGuardInterceptor</c> (Platform.Audit) can recognise and guard any versioned
/// entity via a single <c>entry.Entity is IVersionedRecord</c> check without needing to know each
/// entity's <c>TKey</c>.
/// </summary>
public interface IVersionedRecord
{
    /// <summary>
    /// True once this row has reached a published/locked state in its entity-specific status
    /// enum (e.g. <c>Published</c>, <c>Locked</c>, <c>Approved</c> depending on the entity) and
    /// must never be mutated again except by superseding it via a new version.
    /// </summary>
    bool IsImmutable { get; }
}

/// <summary>
/// Shared shape for the append-only, never-overwritten history pattern used across the system
/// for judgements and results (learning evidence, mastery evaluations, assessment results,
/// attendance records, report cards, promotion decisions, curriculum syllabus versions, etc.).
///
/// A row satisfying this contract is written once and never mutated after it becomes
/// <see cref="IVersionedRecord.IsImmutable"/>. Corrections happen by inserting a new version and
/// re-pointing <see cref="SupersedesId"/>/<see cref="SupersededById"/>, never by updating the row
/// in place. <c>SaveChangesGuardInterceptor</c> (Platform.Audit) enforces this generically, across
/// every entity that implements this interface, by checking <see cref="IVersionedRecord.IsImmutable"/>
/// alone — it does not need to know each entity's specific status vocabulary.
/// </summary>
/// <typeparam name="TKey">
/// The entity's own primary-key type. Left generic deliberately: different modules may choose
/// <c>long</c> (server-generated identity) or <c>Guid</c> (client-generated, e.g. for offline-first
/// MAUI drafts) as appropriate for a given entity — Platform.Common takes no position on this.
/// Constrained to <c>struct</c> (rather than <c>notnull</c>) specifically so <see cref="SupersedesId"/>/
/// <see cref="SupersededById"/>'s <c>TKey?</c> reliably erases to <c>Nullable&lt;TKey&gt;</c> — with
/// only <c>notnull</c>, an unconstrained-nullable type parameter's <c>?</c> is purely a source-level
/// reference-type annotation and does not produce <c>Nullable&lt;TKey&gt;</c> for a value-type TKey.
/// </typeparam>
public interface IVersionedRecord<TKey> : IVersionedRecord
    where TKey : struct
{
    TKey Id { get; }

    /// <summary>1-based version number within this record's lineage.</summary>
    int Version { get; }

    /// <summary>True for exactly one row per lineage: the most recent version.</summary>
    bool IsCurrent { get; }

    /// <summary>The previous version in this lineage, if this row supersedes one.</summary>
    TKey? SupersedesId { get; }

    /// <summary>The next version in this lineage, if this row has since been superseded.</summary>
    TKey? SupersededById { get; }
}

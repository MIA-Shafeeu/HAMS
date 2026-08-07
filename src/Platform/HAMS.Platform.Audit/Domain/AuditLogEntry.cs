namespace HAMS.Platform.Audit.Domain;

/// <summary>
/// One immutable, insert-only row per audited event. Every module writes to this same table
/// through <see cref="IAuditLogWriter"/> rather than maintaining its own audit trail (build plan
/// §1.4). Nothing in the system ever updates or deletes a row here — there is deliberately no
/// versioning/status pattern on this entity because the table itself is already append-only by
/// construction, not because it was overlooked.
/// </summary>
public sealed class AuditLogEntry
{
    public long Id { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public AuditAction Action { get; init; }

    /// <summary>Short entity/aggregate name, e.g. "AssessmentResult" — not a .NET type's full name.</summary>
    public required string EntityType { get; init; }

    /// <summary>Stringified primary key — entities key on <c>long</c> or <c>Guid</c> depending on module.</summary>
    public string? EntityId { get; init; }

    /// <summary>Null for system-initiated events (e.g. a scheduled job).</summary>
    public Guid? ActorPersonId { get; init; }

    public Guid? ActorUserId { get; init; }

    /// <summary>Short human-readable description of what happened.</summary>
    public required string Summary { get; init; }

    /// <summary>Optional serialized before/after detail, e.g. a property-level diff.</summary>
    public string? DataJson { get; init; }

    public string? IpAddress { get; init; }

    /// <summary>Ties together every audit row produced by one logical request/operation.</summary>
    public Guid? CorrelationId { get; init; }
}

namespace HAMS.CommunicationPortals.Domain;

/// <summary>
/// A guardian's confirmation that they've seen something shown to them in the portal (build plan
/// Phase 13 scope: "parent acknowledgements") — a real school need (e.g. confirming a report card
/// has been seen). Deliberately generic (<see cref="EntityType"/>/<see cref="EntityId"/>), the same
/// loose-reference shape <c>Platform.Audit</c>'s <c>AuditLogEntry</c> already uses, so it works for
/// report cards today and any future acknowledgeable item (a behaviour incident, a homework notice)
/// without a new table. Append-only — a guardian acknowledging twice is a no-op, never a second row
/// (see <c>IGuardianAcknowledgementService.AcknowledgeAsync</c>).
/// </summary>
public sealed class GuardianAcknowledgement
{
    public Guid Id { get; init; }

    public Guid GuardianPersonId { get; init; }

    public Guid StudentPersonId { get; init; }

    public required string EntityType { get; init; }

    public required string EntityId { get; init; }

    public DateTimeOffset AcknowledgedAtUtc { get; init; }
}

using HAMS.Platform.Audit.Domain;

namespace HAMS.Platform.Audit;

/// <summary>
/// A one-call shorthand for the exact <see cref="AuditLogEntry"/> shape <c>TokenIssuer</c> already
/// establishes — used at the endpoint layer (where the caller's identity is already resolved via
/// <c>ICurrentUser</c>) to close the gap where several workflow-transition/administrative actions
/// (assessment moderation, topic closure, report card approval, role grant/revoke, guardian
/// verify/reject, logout) previously wrote no audit row at all despite build plan §1.4's "written to
/// by every module" requirement.
/// </summary>
public static class AuditLogWriterExtensions
{
    public static Task WriteEntryAsync(
        this IAuditLogWriter writer, DateTimeOffset occurredAtUtc, AuditAction action, string entityType, string? entityId,
        Guid? actorPersonId, string summary, string? ipAddress = null, CancellationToken cancellationToken = default)
        => writer.WriteAsync(new AuditLogEntry
        {
            OccurredAtUtc = occurredAtUtc,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ActorPersonId = actorPersonId,
            Summary = summary,
            IpAddress = ipAddress,
        }, cancellationToken);
}

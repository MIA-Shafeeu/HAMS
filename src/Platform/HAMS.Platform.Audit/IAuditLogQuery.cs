using HAMS.Platform.Audit.Domain;

namespace HAMS.Platform.Audit;

public sealed record AuditLogSearchRequest(
    DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, AuditAction? Action = null, string? EntityType = null,
    Guid? ActorPersonId = null, string? SearchText = null, int Page = 1, int PageSize = 50);

public sealed record AuditLogSearchResult(IReadOnlyList<AuditLogEntry> Entries, int TotalCount);

/// <summary>
/// The read side of the audit trail (build plan Phase 12 — "Audit UI... search/export UI only, the
/// write-path lives in Platform.Audit"). Lives alongside <see cref="IAuditLogWriter"/> in the same
/// kernel rather than in <c>ReportingAnalyticsAudit</c>, since both sides own the same "audit"
/// schema — <c>ReportingAnalyticsAudit</c> (which already references this kernel) is the module
/// that exposes it to staff via endpoint/UI, not the module that owns the data.
/// </summary>
public interface IAuditLogQuery
{
    Task<AuditLogSearchResult> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Every distinct <see cref="AuditLogEntry.EntityType"/> value actually seen so far — feeds the search UI's filter dropdown without hardcoding a list that would drift from what's actually been audited across 12 modules.</summary>
    Task<IReadOnlyList<string>> GetDistinctEntityTypesAsync(CancellationToken cancellationToken = default);
}

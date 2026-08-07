using HAMS.Platform.Audit.Domain;

namespace HAMS.Platform.Audit;

/// <summary>
/// The one chokepoint every module writes audit rows through (build plan §1.4). Deliberately a
/// single flat method — callers build the <see cref="AuditLogEntry"/> themselves so this
/// interface never needs to grow overloads as new fields matter to different callers.
/// </summary>
public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

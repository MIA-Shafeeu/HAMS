using HAMS.Platform.Audit.Domain;

namespace HAMS.Platform.Audit.Infrastructure;

internal sealed class AuditLogWriter(AuditDbContext dbContext) : IAuditLogWriter
{
    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

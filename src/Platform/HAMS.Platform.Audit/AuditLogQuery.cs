using HAMS.Platform.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Platform.Audit;

internal sealed class AuditLogQuery(AuditDbContext dbContext) : IAuditLogQuery
{
    public async Task<AuditLogSearchResult> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditLogEntries.AsNoTracking().AsQueryable();

        if (request.FromUtc is { } fromUtc)
        {
            query = query.Where(e => e.OccurredAtUtc >= fromUtc);
        }

        if (request.ToUtc is { } toUtc)
        {
            query = query.Where(e => e.OccurredAtUtc <= toUtc);
        }

        if (request.Action is { } action)
        {
            query = query.Where(e => e.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(e => e.EntityType == request.EntityType);
        }

        if (request.ActorPersonId is { } actorPersonId)
        {
            query = query.Where(e => e.ActorPersonId == actorPersonId);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var pattern = $"%{request.SearchText}%";
            query = query.Where(e => EF.Functions.Like(e.Summary, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var page = Math.Max(request.Page, 1);

        var entries = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AuditLogSearchResult(entries, totalCount);
    }

    public async Task<IReadOnlyList<string>> GetDistinctEntityTypesAsync(CancellationToken cancellationToken = default)
        => await dbContext.AuditLogEntries.AsNoTracking()
            .Select(e => e.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
}

using HAMS.Platform.Access;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

/// <summary>
/// Mimics the observable effects of the real <c>ScopedAccessGrantProjector</c> (stage + save the
/// source context, record the grant) without needing a real relational transaction — the real one
/// calls <c>Database.BeginTransactionAsync()</c>, which EF Core's InMemory provider doesn't
/// support, so assignment-service unit tests use this instead and verify cross-context
/// transactional behaviour separately, live against real SQL Server.
/// </summary>
internal sealed class FakeScopedAccessGrantProjector : IScopedAccessGrantProjector
{
    public ScopedAccessGrant? LastGrant { get; private set; }

    public async Task ProjectAsync(DbContext sourceContext, Action stageSourceChanges, ScopedAccessGrant grant, CancellationToken cancellationToken = default)
    {
        stageSourceChanges();
        await sourceContext.SaveChangesAsync(cancellationToken);
        LastGrant = grant;
    }
}

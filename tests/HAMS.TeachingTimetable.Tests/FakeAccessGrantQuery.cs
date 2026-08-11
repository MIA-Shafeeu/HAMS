using HAMS.Platform.Access;

namespace HAMS.TeachingTimetable.Tests;

/// <summary>Returns whatever grant summaries a test registers, bypassing the real AccessGrant table entirely — PersonAccessScopeQuery's own reading of that table is covered separately in HAMS.Platform.Access.Tests; these tests only need to prove StaffAccessScopeQuery interprets a given set of grant shapes correctly.</summary>
internal sealed class FakeAccessGrantQuery(params AccessGrantSummary[] grants) : IPersonAccessScopeQuery
{
    public Task<IReadOnlyList<AccessGrantSummary>> GetActiveGrantsAsync(Guid personId, DateOnly asOf, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AccessGrantSummary>>(grants);
}

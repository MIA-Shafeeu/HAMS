using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Common.Tests;

public class EffectiveDatedQueryExtensionsTests
{
    private sealed record TestAssignment(string Name, DateOnly EffectiveFrom, DateOnly? EffectiveTo)
        : IEffectiveDated;

    public static TheoryData<DateOnly, DateOnly, DateOnly?, bool> BoundaryCases()
    {
        var jan1 = new DateOnly(2026, 1, 1);
        var jan10 = new DateOnly(2026, 1, 10);
        var jan20 = new DateOnly(2026, 1, 20);

        return new TheoryData<DateOnly, DateOnly, DateOnly?, bool>
        {
            // asOf,  effectiveFrom, effectiveTo, expectedActive
            { jan10, jan1,  null,  true },  // open-ended, well within range
            { jan10, jan20, null,  false }, // starts in the future relative to asOf
            { jan10, jan1,  jan1,  false }, // ended before asOf
            { jan1,  jan1,  null,  true },  // asOf == EffectiveFrom: inclusive lower bound
            { jan20, jan1,  jan20, true },  // asOf == EffectiveTo: inclusive upper bound
            { jan1,  jan10, jan20, false }, // asOf before EffectiveFrom
        };
    }

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void IsActiveAsOf_matches_expected_boundary_behaviour(
        DateOnly asOf, DateOnly effectiveFrom, DateOnly? effectiveTo, bool expectedActive)
    {
        var assignment = new TestAssignment("test", effectiveFrom, effectiveTo);

        Assert.Equal(expectedActive, assignment.IsActiveAsOf(asOf));
    }

    [Fact]
    public void ActiveAsOf_filters_an_IQueryable_to_only_currently_active_rows()
    {
        var asOf = new DateOnly(2026, 6, 1);

        var rows = new[]
        {
            new TestAssignment("still active, open-ended", new DateOnly(2026, 1, 1), null),
            new TestAssignment("expired last month", new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 1)),
            new TestAssignment("starts next month", new DateOnly(2026, 7, 1), null),
            new TestAssignment("active, closed-ended, includes asOf", new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 1)),
        }.AsQueryable();

        var active = rows.ActiveAsOf(asOf).Select(x => x.Name).ToList();

        Assert.Equal(
            new[] { "still active, open-ended", "active, closed-ended, includes asOf" },
            active);
    }
}

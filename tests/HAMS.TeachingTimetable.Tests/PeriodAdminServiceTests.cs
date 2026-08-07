using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class PeriodAdminServiceTests
{
    private static TeachingTimetableDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TeachingTimetableDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreatePeriodAsync_is_retrievable_via_GetPeriodsAsync()
    {
        await using var db = CreateContext();
        var service = new PeriodAdminService(db);
        var schoolId = Guid.NewGuid();

        var periodId = await service.CreatePeriodAsync(schoolId, "P1", "Period 1", new TimeOnly(8, 0), new TimeOnly(8, 40), 1);

        var periods = await service.GetPeriodsAsync(schoolId);
        var period = Assert.Single(periods);
        Assert.Equal(periodId, period.Id);
        Assert.Equal(new TimeOnly(8, 0), period.StartTime);
    }

    [Fact]
    public async Task GetPeriodsAsync_orders_by_display_order_and_excludes_other_schools()
    {
        await using var db = CreateContext();
        var service = new PeriodAdminService(db);
        var schoolId = Guid.NewGuid();
        await service.CreatePeriodAsync(schoolId, "P2", "Period 2", new TimeOnly(8, 45), new TimeOnly(9, 25), 2);
        await service.CreatePeriodAsync(schoolId, "P1", "Period 1", new TimeOnly(8, 0), new TimeOnly(8, 40), 1);
        await service.CreatePeriodAsync(Guid.NewGuid(), "P1", "Period 1 (other school)", new TimeOnly(8, 0), new TimeOnly(8, 40), 1);

        var periods = await service.GetPeriodsAsync(schoolId);

        Assert.Equal(["P1", "P2"], periods.Select(p => p.Code));
    }
}

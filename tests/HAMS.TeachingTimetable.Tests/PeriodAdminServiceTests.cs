using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Tests;

public class PeriodAdminServiceTests
{
    private static TeachingTimetableDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TeachingTimetableDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedPeriodAsync(TeachingTimetableDbContext db, Guid schoolId, string code, TimeOnly start, TimeOnly end, int displayOrder)
    {
        db.Periods.Add(new Period { Id = Guid.NewGuid(), SchoolId = schoolId, Code = code, Name = code, StartTime = start, EndTime = end, DisplayOrder = displayOrder });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPeriodsAsync_orders_by_display_order_and_excludes_other_schools()
    {
        await using var db = CreateContext();
        var service = new PeriodAdminService(db);
        var schoolId = Guid.NewGuid();
        await SeedPeriodAsync(db, schoolId, "P2", new TimeOnly(8, 45), new TimeOnly(9, 25), 2);
        await SeedPeriodAsync(db, schoolId, "P1", new TimeOnly(8, 0), new TimeOnly(8, 40), 1);
        await SeedPeriodAsync(db, Guid.NewGuid(), "P1", new TimeOnly(8, 0), new TimeOnly(8, 40), 1);

        var periods = await service.GetPeriodsAsync(schoolId);

        Assert.Equal(["P1", "P2"], periods.Select(p => p.Code));
    }
}

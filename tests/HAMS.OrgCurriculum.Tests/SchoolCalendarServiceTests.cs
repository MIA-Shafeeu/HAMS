using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Tests;

public class SchoolCalendarServiceTests
{
    private static OrgDbContext CreateContext() => new(
        new DbContextOptionsBuilder<OrgDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedMaldivianDefaultWeekAsync(OrgDbContext db, Guid schoolId)
    {
        foreach (var day in new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday })
        {
            db.WorkingDays.Add(new WorkingDay { Id = Guid.NewGuid(), SchoolId = schoolId, DayOfWeek = day });
        }
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, true)]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Tuesday, true)]
    [InlineData(DayOfWeek.Wednesday, true)]
    [InlineData(DayOfWeek.Thursday, true)]
    [InlineData(DayOfWeek.Friday, false)]
    [InlineData(DayOfWeek.Saturday, false)]
    public async Task IsWorkingDayOfWeekAsync_matches_the_Maldivian_default_week(DayOfWeek dayOfWeek, bool expectedWorkingDay)
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        await SeedMaldivianDefaultWeekAsync(db, schoolId);
        var service = new SchoolCalendarService(db);

        var isWorkingDay = await service.IsWorkingDayOfWeekAsync(schoolId, dayOfWeek);

        Assert.Equal(expectedWorkingDay, isWorkingDay);
    }

    [Fact]
    public async Task A_different_school_can_configure_a_completely_different_week()
    {
        // Proves the working week is genuinely per-school configuration, not a global assumption.
        await using var db = CreateContext();
        var mondayFridaySchoolId = Guid.NewGuid();
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            db.WorkingDays.Add(new WorkingDay { Id = Guid.NewGuid(), SchoolId = mondayFridaySchoolId, DayOfWeek = day });
        }
        await db.SaveChangesAsync();
        var service = new SchoolCalendarService(db);

        Assert.True(await service.IsWorkingDayOfWeekAsync(mondayFridaySchoolId, DayOfWeek.Friday));
        Assert.False(await service.IsWorkingDayOfWeekAsync(mondayFridaySchoolId, DayOfWeek.Sunday));
    }

    [Fact]
    public async Task IsHolidayAsync_is_true_only_for_a_declared_date()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        var holidayType = new HolidayType { Id = Guid.NewGuid(), Code = "TEST_TYPE", Name = "Test" };
        db.HolidayTypes.Add(holidayType);
        db.Holidays.Add(new Holiday
        {
            Id = Guid.NewGuid(), SchoolId = schoolId, Date = new DateOnly(2026, 7, 26), HolidayTypeId = holidayType.Id,
            NameEn = "Independence Day", NameDv = "Independence Day (Dv)",
        });
        await db.SaveChangesAsync();
        var service = new SchoolCalendarService(db);

        Assert.True(await service.IsHolidayAsync(schoolId, new DateOnly(2026, 7, 26)));
        Assert.False(await service.IsHolidayAsync(schoolId, new DateOnly(2026, 7, 27)));
    }

    [Fact]
    public async Task IsSchoolDayAsync_is_false_on_a_weekend_even_with_no_holidays_declared()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        await SeedMaldivianDefaultWeekAsync(db, schoolId);
        var service = new SchoolCalendarService(db);

        // 2026-01-02 is a Friday.
        Assert.False(await service.IsSchoolDayAsync(schoolId, new DateOnly(2026, 1, 2)));
    }

    [Fact]
    public async Task IsSchoolDayAsync_is_false_on_a_working_weekday_that_is_a_declared_holiday()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        await SeedMaldivianDefaultWeekAsync(db, schoolId);
        var holidayType = new HolidayType { Id = Guid.NewGuid(), Code = "TEST_TYPE2", Name = "Test" };
        db.HolidayTypes.Add(holidayType);
        var holidayDate = new DateOnly(2026, 1, 1); // a Thursday - an ordinary working day of week
        db.Holidays.Add(new Holiday { Id = Guid.NewGuid(), SchoolId = schoolId, Date = holidayDate, HolidayTypeId = holidayType.Id, NameEn = "New Year", NameDv = "New Year (Dv)" });
        await db.SaveChangesAsync();
        var service = new SchoolCalendarService(db);

        Assert.Equal(DayOfWeek.Thursday, holidayDate.DayOfWeek);
        Assert.False(await service.IsSchoolDayAsync(schoolId, holidayDate));
    }

    [Fact]
    public async Task IsSchoolDayAsync_is_true_on_an_ordinary_working_weekday_with_no_holiday()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        await SeedMaldivianDefaultWeekAsync(db, schoolId);
        var service = new SchoolCalendarService(db);

        // 2026-01-04 is a Sunday.
        Assert.True(await service.IsSchoolDayAsync(schoolId, new DateOnly(2026, 1, 4)));
    }
}

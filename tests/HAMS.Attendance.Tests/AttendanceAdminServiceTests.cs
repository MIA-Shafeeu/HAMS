using HAMS.Attendance.Application;
using HAMS.Attendance.Domain;
using HAMS.Attendance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Tests;

public class AttendanceAdminServiceTests
{
    private static AttendanceDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AttendanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateAttendanceStatusAsync_then_GetAttendanceStatusesAsync_round_trips_ordered_by_DisplayOrder()
    {
        await using var db = CreateContext();
        var service = new AttendanceAdminService(db);

        var lateId = await service.CreateAttendanceStatusAsync(AttendanceStatusCodes.Late, "Late", 2);
        var presentId = await service.CreateAttendanceStatusAsync(AttendanceStatusCodes.Present, "Present", 1);

        var statuses = await service.GetAttendanceStatusesAsync();

        Assert.Equal([presentId, lateId], statuses.Select(s => s.Id));
        Assert.Equal([AttendanceStatusCodes.Present, AttendanceStatusCodes.Late], statuses.Select(s => s.Code));
    }

    [Fact]
    public async Task GetAttendanceStatusesAsync_includes_inactive_statuses()
    {
        await using var db = CreateContext();
        var service = new AttendanceAdminService(db);
        var id = await service.CreateAttendanceStatusAsync(AttendanceStatusCodes.Excused, "Excused", 1);
        await service.SetAttendanceStatusActiveAsync(id, false);

        var statuses = await service.GetAttendanceStatusesAsync();

        var status = Assert.Single(statuses);
        Assert.False(status.IsActive);
    }

    [Fact]
    public async Task SetAttendanceStatusActiveAsync_flips_IsActive()
    {
        await using var db = CreateContext();
        var service = new AttendanceAdminService(db);
        var id = await service.CreateAttendanceStatusAsync(AttendanceStatusCodes.Absent, "Absent", 1);

        await service.SetAttendanceStatusActiveAsync(id, false);
        Assert.False((await db.AttendanceStatuses.SingleAsync(s => s.Id == id)).IsActive);

        await service.SetAttendanceStatusActiveAsync(id, true);
        Assert.True((await db.AttendanceStatuses.SingleAsync(s => s.Id == id)).IsActive);
    }

    [Fact]
    public async Task SetAttendanceStatusActiveAsync_throws_for_an_unknown_id()
    {
        await using var db = CreateContext();
        var service = new AttendanceAdminService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetAttendanceStatusActiveAsync(Guid.NewGuid(), false));
    }
}

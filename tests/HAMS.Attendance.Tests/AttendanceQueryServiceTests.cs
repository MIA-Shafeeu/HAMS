using HAMS.Attendance.Application;
using HAMS.Attendance.Domain;
using HAMS.Attendance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Tests;

public class AttendanceQueryServiceTests
{
    private static AttendanceDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AttendanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetStatusesAsync_returns_only_active_statuses_ordered_by_DisplayOrder()
    {
        await using var db = CreateContext();
        db.AttendanceStatuses.AddRange(
            new AttendanceStatus { Id = Guid.NewGuid(), Code = AttendanceStatusCodes.Late, Name = "Late", DisplayOrder = 2, IsActive = true },
            new AttendanceStatus { Id = Guid.NewGuid(), Code = AttendanceStatusCodes.Present, Name = "Present", DisplayOrder = 1, IsActive = true },
            new AttendanceStatus { Id = Guid.NewGuid(), Code = "RETIRED", Name = "Retired", DisplayOrder = 0, IsActive = false });
        await db.SaveChangesAsync();
        var service = new AttendanceQueryService(db);

        var result = await service.GetStatusesAsync();

        Assert.Equal([AttendanceStatusCodes.Present, AttendanceStatusCodes.Late], result.Select(s => s.Code));
    }

    [Fact]
    public async Task GetDailyRecordsForStudentsAsync_returns_only_the_requested_students_and_date()
    {
        await using var db = CreateContext();
        var presentStatus = new AttendanceStatus { Id = Guid.NewGuid(), Code = AttendanceStatusCodes.Present, Name = "Present", IsActive = true };
        db.AttendanceStatuses.Add(presentStatus);
        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        var studentC = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 6);
        db.DailyAttendanceRecords.AddRange(
            new DailyAttendanceRecord { Id = Guid.NewGuid(), StudentPersonId = studentA, Date = date, AcademicYearId = Guid.NewGuid(), AttendanceStatusId = presentStatus.Id, RecordedByPersonId = Guid.NewGuid() },
            new DailyAttendanceRecord { Id = Guid.NewGuid(), StudentPersonId = studentB, Date = date, AcademicYearId = Guid.NewGuid(), AttendanceStatusId = presentStatus.Id, RecordedByPersonId = Guid.NewGuid() },
            new DailyAttendanceRecord { Id = Guid.NewGuid(), StudentPersonId = studentC, Date = date, AcademicYearId = Guid.NewGuid(), AttendanceStatusId = presentStatus.Id, RecordedByPersonId = Guid.NewGuid() },
            new DailyAttendanceRecord { Id = Guid.NewGuid(), StudentPersonId = studentA, Date = date.AddDays(-1), AcademicYearId = Guid.NewGuid(), AttendanceStatusId = presentStatus.Id, RecordedByPersonId = Guid.NewGuid() });
        await db.SaveChangesAsync();
        var service = new AttendanceQueryService(db);

        var result = await service.GetDailyRecordsForStudentsAsync([studentA, studentB], date);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.StudentPersonId == studentA && r.AttendanceStatusCode == AttendanceStatusCodes.Present);
        Assert.Contains(result, r => r.StudentPersonId == studentB);
        Assert.DoesNotContain(result, r => r.StudentPersonId == studentC);
    }
}

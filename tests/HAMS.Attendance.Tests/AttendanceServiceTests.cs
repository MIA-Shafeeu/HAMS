using HAMS.Attendance.Application;
using HAMS.Attendance.Domain;
using HAMS.Attendance.Infrastructure;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Tests;

public class AttendanceServiceTests
{
    private static AttendanceDbContext CreateContext()
    {
        var db = new AttendanceDbContext(
            new DbContextOptionsBuilder<AttendanceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.AttendanceStatuses.AddRange(AttendanceStatuses());
        db.SaveChanges();
        return db;
    }

    private static AttendanceService CreateService(
        AttendanceDbContext db, FakeNotificationOutboxWriter? outboxWriter = null, params GuardianContact[] guardianContacts)
        => new(db, new FakeSchoolCalendarService(), new FakeGuardianContactResolver(guardianContacts), outboxWriter ?? new FakeNotificationOutboxWriter());

    private static AttendanceStatus[] AttendanceStatuses() =>
    [
        new() { Id = Guid.NewGuid(), Code = AttendanceStatusCodes.Present, Name = "Present", IsActive = true },
        new() { Id = Guid.NewGuid(), Code = AttendanceStatusCodes.Absent, Name = "Absent", IsActive = true },
        new() { Id = Guid.NewGuid(), Code = AttendanceStatusCodes.Late, Name = "Late", IsActive = true },
    ];

    [Fact]
    public async Task MarkDailyAttendanceAsync_rejects_a_date_that_is_not_a_school_day()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        var weekendDate = new DateOnly(2026, 1, 2);
        var service = new AttendanceService(
            db, new FakeSchoolCalendarService(weekendDate), new FakeGuardianContactResolver(), new FakeNotificationOutboxWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MarkDailyAttendanceAsync(
                schoolId, Guid.NewGuid(), weekendDate, Guid.NewGuid(), AttendanceStatusCodes.Present, Guid.NewGuid(), null));
    }

    [Fact]
    public async Task MarkDailyAttendanceAsync_records_a_new_record_on_a_school_day()
    {
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 4); // a Sunday
        var service = CreateService(db);

        var recordId = await service.MarkDailyAttendanceAsync(
            schoolId, studentId, date, Guid.NewGuid(), AttendanceStatusCodes.Present, Guid.NewGuid(), "on time");

        var record = await db.DailyAttendanceRecords.SingleAsync(r => r.Id == recordId);
        Assert.Equal(studentId, record.StudentPersonId);
        Assert.Equal("on time", record.Notes);
    }

    [Fact]
    public async Task MarkDailyAttendanceAsync_upserts_the_same_student_and_date_instead_of_duplicating()
    {
        // Same-day correction (e.g. Absent -> Late) is a normal real need, not an error.
        await using var db = CreateContext();
        var schoolId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var date = new DateOnly(2026, 1, 4);
        var service = CreateService(db);

        var firstRecordId = await service.MarkDailyAttendanceAsync(
            schoolId, studentId, date, academicYearId, AttendanceStatusCodes.Absent, Guid.NewGuid(), null);
        var secondRecordId = await service.MarkDailyAttendanceAsync(
            schoolId, studentId, date, academicYearId, AttendanceStatusCodes.Late, Guid.NewGuid(), "arrived late");

        Assert.Equal(firstRecordId, secondRecordId);
        Assert.Equal(1, await db.DailyAttendanceRecords.CountAsync(r => r.StudentPersonId == studentId && r.Date == date));
        var record = await db.DailyAttendanceRecords.SingleAsync(r => r.Id == firstRecordId);
        var lateStatus = await db.AttendanceStatuses.SingleAsync(s => s.Code == AttendanceStatusCodes.Late);
        Assert.Equal(lateStatus.Id, record.AttendanceStatusId);
        Assert.Equal("arrived late", record.Notes);
    }

    [Fact]
    public async Task MarkDailyAttendanceAsync_rejects_an_unknown_attendance_status_code()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MarkDailyAttendanceAsync(
                Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 4), Guid.NewGuid(), "NOT_A_REAL_CODE", Guid.NewGuid(), null));
    }

    [Fact]
    public async Task MarkLessonAttendanceAsync_upserts_the_same_student_and_lesson_session()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var lessonSessionId = Guid.NewGuid();
        var service = CreateService(db);

        var firstRecordId = await service.MarkLessonAttendanceAsync(
            studentId, lessonSessionId, AttendanceStatusCodes.Present, Guid.NewGuid(), null);
        var secondRecordId = await service.MarkLessonAttendanceAsync(
            studentId, lessonSessionId, AttendanceStatusCodes.Absent, Guid.NewGuid(), "left early");

        Assert.Equal(firstRecordId, secondRecordId);
        Assert.Equal(1, await db.LessonAttendanceRecords.CountAsync(r => r.StudentPersonId == studentId && r.LessonSessionId == lessonSessionId));
    }

    [Fact]
    public async Task MarkDailyAttendanceAsync_enqueues_an_sms_notification_for_a_notifiable_guardian_when_marked_absent()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var guardianContact = new GuardianContact(Guid.NewGuid(), "+9607777777", null);
        var outboxWriter = new FakeNotificationOutboxWriter();
        var service = CreateService(db, outboxWriter, guardianContact);

        var recordId = await service.MarkDailyAttendanceAsync(
            Guid.NewGuid(), studentId, new DateOnly(2026, 1, 4), Guid.NewGuid(), AttendanceStatusCodes.Absent, Guid.NewGuid(), null);

        Assert.NotEqual(Guid.Empty, recordId);
        var notification = Assert.Single(outboxWriter.Enqueued);
        Assert.Equal(NotificationChannelCodes.Sms, notification.ChannelCode);
        Assert.Equal(guardianContact.PhoneNumber, notification.Recipient);
        var record = await db.DailyAttendanceRecords.SingleAsync(r => r.Id == recordId);
        Assert.Equal(studentId, record.StudentPersonId);
    }

    [Fact]
    public async Task MarkDailyAttendanceAsync_falls_back_to_email_when_a_notifiable_guardian_has_no_phone_number()
    {
        await using var db = CreateContext();
        var guardianContact = new GuardianContact(Guid.NewGuid(), null, "guardian@example.mv");
        var outboxWriter = new FakeNotificationOutboxWriter();
        var service = CreateService(db, outboxWriter, guardianContact);

        await service.MarkDailyAttendanceAsync(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 4), Guid.NewGuid(), AttendanceStatusCodes.Absent, Guid.NewGuid(), null);

        var notification = Assert.Single(outboxWriter.Enqueued);
        Assert.Equal(NotificationChannelCodes.Email, notification.ChannelCode);
        Assert.Equal(guardianContact.Email, notification.Recipient);
    }

    [Fact]
    public async Task MarkDailyAttendanceAsync_enqueues_nothing_when_marked_present_even_with_a_notifiable_guardian()
    {
        await using var db = CreateContext();
        var guardianContact = new GuardianContact(Guid.NewGuid(), "+9607777777", null);
        var outboxWriter = new FakeNotificationOutboxWriter();
        var service = CreateService(db, outboxWriter, guardianContact);

        await service.MarkDailyAttendanceAsync(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 4), Guid.NewGuid(), AttendanceStatusCodes.Present, Guid.NewGuid(), null);

        Assert.Empty(outboxWriter.Enqueued);
    }

    [Fact]
    public async Task MarkDailyAttendanceAsync_enqueues_nothing_when_marked_absent_with_no_notifiable_guardian()
    {
        await using var db = CreateContext();
        var outboxWriter = new FakeNotificationOutboxWriter();
        var service = CreateService(db, outboxWriter);

        var recordId = await service.MarkDailyAttendanceAsync(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 4), Guid.NewGuid(), AttendanceStatusCodes.Absent, Guid.NewGuid(), null);

        Assert.Empty(outboxWriter.Enqueued);
        Assert.NotEqual(Guid.Empty, recordId);
    }
}

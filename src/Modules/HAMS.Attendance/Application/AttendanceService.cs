using HAMS.Attendance.Domain;
using HAMS.Attendance.Infrastructure;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Application;

internal sealed class AttendanceService(
    AttendanceDbContext dbContext, ISchoolCalendarService schoolCalendar,
    IGuardianContactResolver guardianContacts, INotificationOutboxWriter outboxWriter) : IAttendanceService
{
    public async Task<Guid> MarkDailyAttendanceAsync(
        Guid schoolId, Guid studentPersonId, DateOnly date, Guid academicYearId, string attendanceStatusCode,
        Guid recordedByPersonId, string? notes, CancellationToken cancellationToken = default)
    {
        if (!await schoolCalendar.IsSchoolDayAsync(schoolId, date, cancellationToken))
        {
            throw new InvalidOperationException($"{date} is not a school day for this school (weekend or declared holiday).");
        }

        var status = await dbContext.AttendanceStatuses
            .SingleOrDefaultAsync(s => s.Code == attendanceStatusCode && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active attendance status with code '{attendanceStatusCode}'.");

        var record = await dbContext.DailyAttendanceRecords
            .SingleOrDefaultAsync(r => r.StudentPersonId == studentPersonId && r.Date == date, cancellationToken);
        var isNewRecord = record is null;
        record ??= new DailyAttendanceRecord { Id = Guid.NewGuid(), StudentPersonId = studentPersonId, Date = date, AcademicYearId = academicYearId };

        void StageChanges()
        {
            if (isNewRecord)
            {
                dbContext.DailyAttendanceRecords.Add(record);
            }

            record.AttendanceStatusId = status.Id;
            record.RecordedByPersonId = recordedByPersonId;
            record.Notes = notes;
        }

        // Absence is the one attendance outcome a guardian needs to hear about the same day — queued
        // via the transactional outbox (build plan §2), never sent synchronously in-request, so a
        // slow/failing carrier can never block or roll back the attendance write itself.
        if (status.Code == AttendanceStatusCodes.Absent)
        {
            var contacts = await guardianContacts.ResolveNotifiableGuardianContactsAsync(studentPersonId, date, cancellationToken);
            var notifications = contacts
                .Select(c => c.PhoneNumber is not null
                    ? new OutboundNotification(NotificationChannelCodes.Sms, c.PhoneNumber, null, $"Your child was marked absent on {date:yyyy-MM-dd}.")
                    : c.Email is not null
                        ? new OutboundNotification(NotificationChannelCodes.Email, c.Email, "Absence notice", $"Your child was marked absent on {date:yyyy-MM-dd}.")
                        : null)
                .Where(n => n is not null)
                .Select(n => n!)
                .ToList();

            if (notifications.Count > 0)
            {
                await outboxWriter.EnqueueManyAsync(dbContext, StageChanges, notifications, cancellationToken);
                return record.Id;
            }
        }

        StageChanges();
        await dbContext.SaveChangesAsync(cancellationToken);

        return record.Id;
    }

    public async Task<Guid> MarkLessonAttendanceAsync(
        Guid studentPersonId, Guid lessonSessionId, string attendanceStatusCode, Guid recordedByPersonId, string? notes,
        CancellationToken cancellationToken = default)
    {
        var status = await dbContext.AttendanceStatuses
            .SingleOrDefaultAsync(s => s.Code == attendanceStatusCode && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active attendance status with code '{attendanceStatusCode}'.");

        var record = await dbContext.LessonAttendanceRecords
            .SingleOrDefaultAsync(r => r.StudentPersonId == studentPersonId && r.LessonSessionId == lessonSessionId, cancellationToken);

        if (record is null)
        {
            record = new LessonAttendanceRecord { Id = Guid.NewGuid(), StudentPersonId = studentPersonId, LessonSessionId = lessonSessionId };
            dbContext.LessonAttendanceRecords.Add(record);
        }

        record.AttendanceStatusId = status.Id;
        record.RecordedByPersonId = recordedByPersonId;
        record.Notes = notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return record.Id;
    }
}

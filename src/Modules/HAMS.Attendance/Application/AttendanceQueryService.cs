using HAMS.Attendance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Application;

internal sealed class AttendanceQueryService(AttendanceDbContext dbContext) : IAttendanceQueryService
{
    public async Task<IReadOnlyList<AttendanceRecordSummary>> GetDailyRecordsAsync(
        Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        => await (
            from record in dbContext.DailyAttendanceRecords
            where record.StudentPersonId == studentPersonId && record.Date >= fromDate && record.Date <= toDate
            join status in dbContext.AttendanceStatuses on record.AttendanceStatusId equals status.Id
            orderby record.Date
            select new AttendanceRecordSummary(record.Date, status.Code, record.Notes))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AttendanceStatusOption>> GetStatusesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AttendanceStatuses.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder)
            .Select(s => new AttendanceStatusOption(s.Id, s.Code, s.Name)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(Guid StudentPersonId, string AttendanceStatusCode)>> GetDailyRecordsForStudentsAsync(
        IReadOnlyList<Guid> studentPersonIds, DateOnly date, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from record in dbContext.DailyAttendanceRecords
            where record.Date == date && studentPersonIds.Contains(record.StudentPersonId)
            join status in dbContext.AttendanceStatuses on record.AttendanceStatusId equals status.Id
            select new { record.StudentPersonId, status.Code })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.StudentPersonId, r.Code)).ToList();
    }
}

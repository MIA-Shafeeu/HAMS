using HAMS.Attendance.Domain;
using HAMS.Attendance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Application;

internal sealed class AttendanceAdminService(AttendanceDbContext dbContext) : IAttendanceAdminService
{
    public async Task<Guid> CreateAttendanceStatusAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var status = new AttendanceStatus { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.AttendanceStatuses.Add(status);
        await dbContext.SaveChangesAsync(cancellationToken);
        return status.Id;
    }

    public async Task<IReadOnlyList<AttendanceStatus>> GetAttendanceStatusesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AttendanceStatuses.OrderBy(s => s.DisplayOrder).ToListAsync(cancellationToken);

    public async Task SetAttendanceStatusActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var status = await dbContext.AttendanceStatuses.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Attendance status not found.");

        status.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAttendanceStatusAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var status = await dbContext.AttendanceStatuses.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Attendance status not found.");

        status.Name = name;
        status.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

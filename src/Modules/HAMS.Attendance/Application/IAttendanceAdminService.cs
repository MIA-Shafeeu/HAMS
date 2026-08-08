using HAMS.Attendance.Domain;

namespace HAMS.Attendance.Application;

/// <summary>
/// Attendance-status admin surface (build plan §1.6 configurable-lookup rule) — extracted from what
/// had been an inline <c>AttendanceDbContext</c> query directly inside <c>AttendanceEndpoints</c>'
/// <c>GET /statuses</c> handler, the same extraction already done for <c>IOrgAdminService</c>/
/// <c>IPeopleAdminService</c>/<c>ICurriculumAdminService</c>. <see cref="IAttendanceQueryService"/>
/// already exposes a narrow, active-only, portal-safe read of this same table — deliberately not
/// reused here since that one is a cross-module read contract (build plan §2), not an admin CRUD surface.
/// </summary>
public interface IAttendanceAdminService
{
    Task<Guid> CreateAttendanceStatusAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceStatus>> GetAttendanceStatusesAsync(CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No attendance status with that id exists.</exception>
    Task SetAttendanceStatusActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">No attendance status with that id exists.</exception>
    Task UpdateAttendanceStatusAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default);
}

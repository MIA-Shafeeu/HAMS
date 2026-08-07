using HAMS.Attendance.Application;
using HAMS.Attendance.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Attendance.Endpoints;

public sealed record MarkDailyAttendanceRequest(Guid SchoolId, Guid StudentPersonId, DateOnly Date, Guid AcademicYearId, string AttendanceStatusCode, string? Notes);
public sealed record MarkLessonAttendanceRequest(Guid StudentPersonId, Guid LessonSessionId, string AttendanceStatusCode, string? Notes);

public sealed record CreateAttendanceStatusRequest(string Code, string Name, int DisplayOrder);
public sealed record SetActiveRequest(bool IsActive);

/// <summary>Attendance marking surface (build plan Phase 5 scope). Requires authentication; the recording staff member is taken from the caller's identity.</summary>
internal static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/attendance").WithTags("Attendance").RequireAuthorization();

        group.MapGet("/statuses", async (IAttendanceAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAttendanceStatusesAsync(ct)));

        group.MapPost("/statuses", async (
            CreateAttendanceStatusRequest request, IAttendanceAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateAttendanceStatusAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/attendance/statuses/{id}", new { id });
        });

        group.MapPost("/statuses/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IAttendanceAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetAttendanceStatusActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/daily", async (
            MarkDailyAttendanceRequest request, IAttendanceService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            try
            {
                var id = await service.MarkDailyAttendanceAsync(
                    request.SchoolId, request.StudentPersonId, request.Date, request.AcademicYearId,
                    request.AttendanceStatusCode, recordedBy, request.Notes, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/daily", async (Guid studentPersonId, DateOnly fromDate, DateOnly toDate, AttendanceDbContext db, CancellationToken ct) =>
            Results.Ok(await db.DailyAttendanceRecords
                .Where(r => r.StudentPersonId == studentPersonId && r.Date >= fromDate && r.Date <= toDate)
                .OrderBy(r => r.Date)
                .ToListAsync(ct)));

        group.MapPost("/lesson", async (
            MarkLessonAttendanceRequest request, IAttendanceService service, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.PersonId is not { } recordedBy) return Results.Unauthorized();

            try
            {
                var id = await service.MarkLessonAttendanceAsync(
                    request.StudentPersonId, request.LessonSessionId, request.AttendanceStatusCode, recordedBy, request.Notes, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/lesson", async (Guid lessonSessionId, AttendanceDbContext db, CancellationToken ct) =>
            Results.Ok(await db.LessonAttendanceRecords.Where(r => r.LessonSessionId == lessonSessionId).ToListAsync(ct)));

        return endpoints;
    }
}

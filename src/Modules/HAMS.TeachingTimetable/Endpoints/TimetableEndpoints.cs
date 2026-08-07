using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.TeachingTimetable.Application;
using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.TeachingTimetable.Endpoints;

public sealed record CreatePeriodRequest(Guid SchoolId, string Code, string Name, TimeOnly StartTime, TimeOnly EndTime, int DisplayOrder);
public sealed record ScheduleTimetableEntryRequest(Guid SchoolId, Guid ClassId, Guid SubjectId, Guid TeachingAssignmentId, Guid AcademicYearId, DayOfWeek DayOfWeek, Guid PeriodId);

/// <summary>Period/Timetable admin surface (build plan Phase 4 scope). Mutations require a live School/System Administrator check.</summary>
internal static class TimetableEndpoints
{
    public static IEndpointRouteBuilder MapTimetableEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/teaching").WithTags("Timetable").RequireAuthorization();

        group.MapGet("/periods", async (Guid schoolId, IPeriodAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetPeriodsAsync(schoolId, ct)));

        group.MapPost("/periods", async (
            CreatePeriodRequest request, IPeriodAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreatePeriodAsync(request.SchoolId, request.Code, request.Name, request.StartTime, request.EndTime, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/teaching/periods/{id}", new { id });
        });

        group.MapGet("/timetable", async (Guid classId, Guid academicYearId, ITimetableService service, CancellationToken ct) =>
            Results.Ok(await service.GetEntriesForClassAsync(classId, academicYearId, ct)));

        // Staff's own schedule (build plan Phase 14 — mobile) — deliberately resolves the caller
        // from ICurrentUser.PersonId, never a client-supplied staffPersonId, the same "you can only
        // ever be asking about yourself" discipline StudentPortalEndpoints already established.
        group.MapGet("/timetable/mine", async (
            Guid schoolId, Guid academicYearId, DateOnly? asOf, ITimetableService service, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!user.IsStaff || user.PersonId is not { } staffPersonId) return Results.Forbid();

            return Results.Ok(await service.GetEntriesForStaffAsync(staffPersonId, schoolId, academicYearId, asOf ?? clock.TodayUtc, ct));
        });

        group.MapPost("/timetable", async (
            ScheduleTimetableEntryRequest request, ITimetableService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.ScheduleAsync(
                    request.SchoolId, request.ClassId, request.SubjectId, request.TeachingAssignmentId, request.AcademicYearId,
                    request.DayOfWeek, request.PeriodId, ct);
                return Results.Created($"/api/v1/teaching/timetable/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        });

        group.MapDelete("/timetable/{timetableEntryId:guid}", async (
            Guid timetableEntryId, ITimetableService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await service.RemoveAsync(timetableEntryId, ct);
            return Results.NoContent();
        });

        return endpoints;
    }
}

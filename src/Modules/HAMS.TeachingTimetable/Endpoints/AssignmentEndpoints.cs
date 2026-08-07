using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using HAMS.TeachingTimetable.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.TeachingTimetable.Endpoints;

public sealed record AssignSubjectTeacherRequest(Guid StaffPersonId, Guid SubjectId, Guid ClassId, Guid AcademicYearId, Guid? SchoolId, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record AssignClassTeacherRequest(Guid StaffPersonId, Guid ClassId, Guid AcademicYearId, Guid? SchoolId, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record AssignLeadingTeacherRequest(Guid StaffPersonId, Guid SubjectId, Guid AcademicYearId, Guid? SchoolId, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record CreateSubstitutionRequest(Guid OriginalAssignmentId, Guid SubstituteStaffPersonId, DateOnly SubstitutionDate, Guid? SchoolId, string? Reason);

/// <summary>Teaching-assignment/substitution admin surface (build plan Phase 4 scope). Mutations require a live School/System Administrator check.</summary>
internal static class AssignmentEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/teaching").WithTags("TeachingAssignments").RequireAuthorization();

        group.MapPost("/subject-teaching-assignments", async (
            AssignSubjectTeacherRequest request, ISubjectTeachingAssignmentService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.AssignAsync(
                request.StaffPersonId, request.SubjectId, request.ClassId, request.AcademicYearId, request.SchoolId,
                request.EffectiveFrom, request.EffectiveTo, ct);
            return Results.Created($"/api/v1/teaching/subject-teaching-assignments/{id}", new { id });
        });

        group.MapGet("/subject-teaching-assignments", async (Guid classId, Guid academicYearId, ISubjectTeachingAssignmentService service, CancellationToken ct) =>
            Results.Ok(await service.GetAssignmentsForClassAsync(classId, academicYearId, ct)));

        group.MapPost("/subject-teaching-assignments/{assignmentId:guid}/end", async (
            Guid assignmentId, DateOnly effectiveTo, ISubjectTeachingAssignmentService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await service.EndAsync(assignmentId, effectiveTo, ct);
            return Results.NoContent();
        });

        group.MapPost("/class-teacher-assignments", async (
            AssignClassTeacherRequest request, IClassTeacherAssignmentService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.AssignAsync(
                request.StaffPersonId, request.ClassId, request.AcademicYearId, request.SchoolId,
                request.EffectiveFrom, request.EffectiveTo, ct);
            return Results.Created($"/api/v1/teaching/class-teacher-assignments/{id}", new { id });
        });

        group.MapGet("/class-teacher-assignments", async (Guid classId, Guid academicYearId, IClassTeacherAssignmentService service, CancellationToken ct) =>
            Results.Ok(await service.GetAssignmentsForClassAsync(classId, academicYearId, ct)));

        group.MapPost("/class-teacher-assignments/{assignmentId:guid}/end", async (
            Guid assignmentId, DateOnly effectiveTo, IClassTeacherAssignmentService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await service.EndAsync(assignmentId, effectiveTo, ct);
            return Results.NoContent();
        });

        group.MapPost("/leading-teacher-assignments", async (
            AssignLeadingTeacherRequest request, ILeadingTeacherAssignmentService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.AssignAsync(
                request.StaffPersonId, request.SubjectId, request.AcademicYearId, request.SchoolId,
                request.EffectiveFrom, request.EffectiveTo, ct);
            return Results.Created($"/api/v1/teaching/leading-teacher-assignments/{id}", new { id });
        });

        group.MapGet("/leading-teacher-assignments", async (Guid subjectId, Guid academicYearId, ILeadingTeacherAssignmentService service, CancellationToken ct) =>
            Results.Ok(await service.GetAssignmentsForSubjectAsync(subjectId, academicYearId, ct)));

        group.MapPost("/leading-teacher-assignments/{assignmentId:guid}/end", async (
            Guid assignmentId, DateOnly effectiveTo, ILeadingTeacherAssignmentService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await service.EndAsync(assignmentId, effectiveTo, ct);
            return Results.NoContent();
        });

        group.MapPost("/substitutions", async (
            CreateSubstitutionRequest request, ISubstitutionService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.CreateSubstitutionAsync(
                    request.OriginalAssignmentId, request.SubstituteStaffPersonId, request.SubstitutionDate, request.SchoolId,
                    request.Reason, ct);
                return Results.Created($"/api/v1/teaching/substitutions/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }
}

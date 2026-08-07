using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.PeopleEnrollment.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Audit;
using HAMS.Platform.Audit.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.PeopleEnrollment.Endpoints;

public sealed record CreateAtollRequest(string Code, string NameEn, string? NameDv, int DisplayOrder);
public sealed record CreateIslandRequest(Guid AtollId, string Code, string NameEn, string? NameDv, int DisplayOrder);

public sealed record AddressRequest(
    Guid IslandId, string RoadEn, string RoadDv, string HouseNameEn, string HouseNameDv,
    string? BuildingEn, string? BuildingDv, string? Floor, string? Apartment);

public sealed record CreatePersonRequest(string NameEn, string NameDv, DateOnly DateOfBirth, AddressRequest Address, string? PhoneNumber = null, string? Email = null);

public sealed record CreateStudentProfileRequest(Guid PersonId, string AdmissionNumber, DateOnly AdmissionDate);
public sealed record CreateStaffProfileRequest(Guid PersonId, string EmployeeNumber, DateOnly HireDate, string EmploymentStatusCode);
public sealed record CreateGuardianProfileRequest(Guid PersonId);
public sealed record AddStaffQualificationRequest(string Title, string? AwardingInstitution, int? YearAwarded);

public sealed record CreateStudentEnrollmentRequest(Guid StudentPersonId, Guid GradeId, Guid ClassId, Guid AcademicYearId, DateOnly EffectiveFrom);

public sealed record CreateSimpleLookupRequest(string Code, string Name, int DisplayOrder);
public sealed record SetActiveRequest(bool IsActive);

/// <summary>Student/Guardian/Staff master-data admin surface (build plan Phase 3 scope). Mutations require a live School/System Administrator check.</summary>
internal static class PeopleEndpoints
{
    public static IEndpointRouteBuilder MapPeopleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/people").WithTags("People").RequireAuthorization();

        group.MapGet("/atolls", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAtollsAsync(ct)));

        group.MapPost("/atolls", async (
            CreateAtollRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateAtollAsync(request.Code, request.NameEn, request.NameDv, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/people/atolls/{id}", new { id });
        });

        group.MapGet("/islands", async (Guid atollId, IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetIslandsAsync(atollId, ct)));

        group.MapPost("/islands", async (
            CreateIslandRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateIslandAsync(request.AtollId, request.Code, request.NameEn, request.NameDv, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/people/islands/{id}", new { id });
        });

        group.MapGet("/persons/{personId:guid}", async (Guid personId, IPeopleAdminService service, CancellationToken ct) =>
        {
            var person = await service.GetPersonAsync(personId, ct);
            return person is null ? Results.NotFound() : Results.Ok(person);
        });

        group.MapPost("/persons", async (
            CreatePersonRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var address = new Address
            {
                IslandId = request.Address.IslandId,
                RoadEn = request.Address.RoadEn,
                RoadDv = request.Address.RoadDv,
                HouseNameEn = request.Address.HouseNameEn,
                HouseNameDv = request.Address.HouseNameDv,
                BuildingEn = request.Address.BuildingEn,
                BuildingDv = request.Address.BuildingDv,
                Floor = request.Address.Floor,
                Apartment = request.Address.Apartment,
            };
            var id = await service.CreatePersonAsync(request.NameEn, request.NameDv, request.DateOfBirth, address, request.PhoneNumber, request.Email, ct);
            return Results.Created($"/api/v1/people/persons/{id}", new { id });
        });

        group.MapGet("/students", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetStudentProfilesAsync(ct)));

        group.MapPost("/students", async (
            CreateStudentProfileRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateStudentProfileAsync(request.PersonId, request.AdmissionNumber, request.AdmissionDate, ct);
            return Results.Created($"/api/v1/people/students/{id}", new { id });
        });

        group.MapGet("/employment-statuses", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetEmploymentStatusesAsync(ct)));

        group.MapGet("/employment-statuses/all", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllEmploymentStatusesAsync(ct)));

        group.MapPost("/employment-statuses", async (
            CreateSimpleLookupRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateEmploymentStatusAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/people/employment-statuses/{id}", new { id });
        });

        group.MapPost("/employment-statuses/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetEmploymentStatusActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/staff", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetStaffProfilesAsync(ct)));

        group.MapPost("/staff", async (
            CreateStaffProfileRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.CreateStaffProfileAsync(request.PersonId, request.EmployeeNumber, request.HireDate, request.EmploymentStatusCode, ct);
                return Results.Created($"/api/v1/people/staff/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/staff/{staffProfileId:guid}/qualifications", async (Guid staffProfileId, IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetStaffQualificationsAsync(staffProfileId, ct)));

        group.MapPost("/staff/{staffProfileId:guid}/qualifications", async (
            Guid staffProfileId, AddStaffQualificationRequest request, IPeopleAdminService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.AddStaffQualificationAsync(staffProfileId, request.Title, request.AwardingInstitution, request.YearAwarded, ct);
            return Results.Created($"/api/v1/people/staff/{staffProfileId}/qualifications/{id}", new { id });
        });

        group.MapGet("/guardians", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetGuardianProfilesAsync(ct)));

        group.MapPost("/guardians", async (
            CreateGuardianProfileRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateGuardianProfileAsync(request.PersonId, ct);
            return Results.Created($"/api/v1/people/guardians/{id}", new { id });
        });

        group.MapGet("/relationship-types", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetRelationshipTypesAsync(ct)));

        group.MapGet("/relationship-types/all", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllRelationshipTypesAsync(ct)));

        group.MapPost("/relationship-types", async (
            CreateSimpleLookupRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateRelationshipTypeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/people/relationship-types/{id}", new { id });
        });

        group.MapPost("/relationship-types/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetRelationshipTypeActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/restriction-types", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetRestrictionTypesAsync(ct)));

        group.MapGet("/restriction-types/all", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllRestrictionTypesAsync(ct)));

        group.MapPost("/restriction-types", async (
            CreateSimpleLookupRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateRestrictionTypeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/people/restriction-types/{id}", new { id });
        });

        group.MapPost("/restriction-types/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetRestrictionTypeActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/enrollment-types", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetEnrollmentTypesAsync(ct)));

        group.MapGet("/enrollment-types/all", async (IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllEnrollmentTypesAsync(ct)));

        group.MapPost("/enrollment-types", async (
            CreateSimpleLookupRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateEnrollmentTypeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/people/enrollment-types/{id}", new { id });
        });

        group.MapPost("/enrollment-types/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IPeopleAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetEnrollmentTypeActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/guardian-relationships", async (
            EstablishGuardianRelationshipRequest request, IGuardianRelationshipService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.EstablishAsync(request, ct);
            return Results.Created($"/api/v1/people/guardian-relationships/{id}", new { id });
        });

        group.MapPost("/guardian-relationships/{relationshipId:guid}/revise", async (
            Guid relationshipId, ReviseGuardianRelationshipRequest request, DateOnly effectiveFrom, IGuardianRelationshipService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.ReviseAsync(relationshipId, request, effectiveFrom, ct);
                return Results.Created($"/api/v1/people/guardian-relationships/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/guardian-relationships/{relationshipId:guid}/close", async (
            Guid relationshipId, DateOnly effectiveTo, IGuardianRelationshipService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await service.CloseAsync(relationshipId, effectiveTo, ct);
            return Results.NoContent();
        });

        group.MapGet("/guardian-relationships", async (Guid studentPersonId, IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetGuardianRelationshipsForStudentAsync(studentPersonId, ct)));

        group.MapPost("/guardian-relationships/{relationshipId:guid}/verify", async (
            Guid relationshipId, IGuardianRelationshipService service, IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.VerifyAsync(relationshipId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(GuardianStudentRelationship), relationshipId.ToString(), user.PersonId, "Guardian relationship verified.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/guardian-relationships/{relationshipId:guid}/reject", async (
            Guid relationshipId, IGuardianRelationshipService service, IRoleMembershipQuery roles, IAuditLogWriter audit, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.RejectAsync(relationshipId, ct);
                await audit.WriteEntryAsync(clock.UtcNow, AuditAction.Update, nameof(GuardianStudentRelationship), relationshipId.ToString(), user.PersonId, "Guardian relationship rejected.", cancellationToken: ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/enrollments", async (
            CreateStudentEnrollmentRequest request, IStudentEnrollmentService enrollmentService,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await enrollmentService.EnrollAsync(
                    request.StudentPersonId, request.GradeId, request.ClassId, request.AcademicYearId, request.EffectiveFrom, ct);
                return Results.Created($"/api/v1/people/enrollments/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        });

        group.MapGet("/enrollments", async (Guid studentPersonId, IPeopleAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetEnrollmentsForStudentAsync(studentPersonId, ct)));

        // First real HTTP surface for the class roster (build plan Phase 14 — mobile): the Blazor
        // staff Attendance page has called GetActiveRosterForClassAsync directly via DI since Phase
        // 12, but a real HTTP client (unlike a Blazor Server page) has no DI access, so mobile needs
        // an actual endpoint for the same read.
        group.MapGet("/roster/class/{classId:guid}", async (
            Guid classId, DateOnly? asOf, IStudentEnrollmentService enrollmentService, IClock clock, CancellationToken ct) =>
            Results.Ok(await enrollmentService.GetActiveRosterForClassAsync(classId, asOf ?? clock.TodayUtc, ct)));

        group.MapPost("/enrollments/{enrollmentId:guid}/end", async (
            Guid enrollmentId, DateOnly effectiveTo, IStudentEnrollmentService enrollmentService,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await enrollmentService.EndEnrollmentAsync(enrollmentId, effectiveTo, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }
}

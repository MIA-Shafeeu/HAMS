using HAMS.OrgCurriculum.Application;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.OrgCurriculum.Endpoints;

public sealed record CreateSchoolRequest(string Code, string Name);
public sealed record CreateCampusRequest(Guid SchoolId, string Code, string Name);
public sealed record CreateAcademicYearRequest(Guid SchoolId, string Code, string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record CreateTermRequest(Guid AcademicYearId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int DisplayOrder);
public sealed record CreatePhaseRequest(Guid SchoolId, string Code, string Name, int DisplayOrder);
public sealed record CreateKeyStageRequest(Guid SchoolId, Guid PhaseId, string Code, string Name, int DisplayOrder);
public sealed record CreateGradeRequest(Guid SchoolId, string Code, string Name, int DisplayOrder);
public sealed record SetNextGradeRequest(Guid? NextGradeId);
public sealed record CreateClassRequest(Guid SchoolId, Guid? CampusId, Guid AcademicYearId, string Code, string Name, string ColorHex, IReadOnlyList<Guid> GradeIds);
public sealed record CreateGradeKeyStageAssignmentRequest(Guid GradeId, Guid KeyStageId, Guid AcademicYearId, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record CreateKeyStagePolicyRequest(
    Guid KeyStageId, Guid AcademicYearId, string EvaluationModelCode,
    Guid? AchievementScaleId = null, Guid? AssessmentSchemeId = null, Guid? GradeScaleId = null, Guid? PromotionPolicyId = null);
public sealed record SetWorkingDayRequest(DayOfWeek DayOfWeek, bool IsWorkingDay);
public sealed record CreateHolidayRequest(DateOnly Date, string HolidayTypeCode, string NameEn, string NameDv);
public sealed record CreateEvaluationModelRequest(string Code, string Name, string? Description, int DisplayOrder);
public sealed record CreateHolidayTypeRequest(string Code, string Name, int DisplayOrder);
public sealed record SetActiveRequest(bool IsActive);

/// <summary>
/// Org Structure admin surface (build plan Phase 1 scope). Mutations require a live School/System
/// Administrator check. Delegates all reads/writes to <see cref="IOrgAdminService"/> — the single
/// implementation this endpoint group and the System Administration Blazor UI both share.
/// </summary>
internal static class OrgEndpoints
{
    public static IEndpointRouteBuilder MapOrgEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/org").WithTags("Org").RequireAuthorization();

        group.MapGet("/schools", async (IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetSchoolsAsync(ct)));

        group.MapPost("/schools", async (
            CreateSchoolRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateSchoolAsync(request.Code, request.Name, ct);
            return Results.Created($"/api/v1/org/schools/{id}", new { id });
        });

        group.MapGet("/campuses", async (Guid schoolId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetCampusesAsync(schoolId, ct)));

        group.MapPost("/campuses", async (
            CreateCampusRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateCampusAsync(request.SchoolId, request.Code, request.Name, ct);
            return Results.Created($"/api/v1/org/campuses/{id}", new { id });
        });

        group.MapGet("/academic-years", async (Guid schoolId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAcademicYearsAsync(schoolId, ct)));

        group.MapPost("/academic-years", async (
            CreateAcademicYearRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateAcademicYearAsync(request.SchoolId, request.Code, request.Name, request.StartDate, request.EndDate, ct);
            return Results.Created($"/api/v1/org/academic-years/{id}", new { id });
        });

        group.MapGet("/terms", async (Guid academicYearId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetTermsAsync(academicYearId, ct)));

        group.MapPost("/terms", async (
            CreateTermRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateTermAsync(request.AcademicYearId, request.Code, request.Name, request.StartDate, request.EndDate, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/terms/{id}", new { id });
        });

        group.MapGet("/phases", async (Guid schoolId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetPhasesAsync(schoolId, ct)));

        group.MapPost("/phases", async (
            CreatePhaseRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreatePhaseAsync(request.SchoolId, request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/phases/{id}", new { id });
        });

        group.MapGet("/key-stages", async (Guid schoolId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetKeyStagesAsync(schoolId, ct)));

        group.MapPost("/key-stages", async (
            CreateKeyStageRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateKeyStageAsync(request.SchoolId, request.PhaseId, request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/key-stages/{id}", new { id });
        });

        group.MapGet("/grades", async (Guid schoolId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetGradesAsync(schoolId, ct)));

        group.MapPost("/grades", async (
            CreateGradeRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateGradeAsync(request.SchoolId, request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/grades/{id}", new { id });
        });

        group.MapPost("/grades/{gradeId:guid}/next-grade", async (
            Guid gradeId, SetNextGradeRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetNextGradeAsync(gradeId, request.NextGradeId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/classes", async (Guid academicYearId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetClassesAsync(academicYearId, ct)));

        group.MapPost("/classes", async (
            CreateClassRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateClassAsync(request.SchoolId, request.CampusId, request.AcademicYearId, request.Code, request.Name, request.ColorHex, request.GradeIds, ct);
            return Results.Created($"/api/v1/org/classes/{id}", new { id });
        });

        group.MapPost("/grade-key-stage-assignments", async (
            CreateGradeKeyStageAssignmentRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateGradeKeyStageAssignmentAsync(request.GradeId, request.KeyStageId, request.AcademicYearId, request.EffectiveFrom, request.EffectiveTo, ct);
            return Results.Created($"/api/v1/org/grade-key-stage-assignments/{id}", new { id });
        });

        group.MapGet("/evaluation-models", async (IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetEvaluationModelsAsync(ct)));

        group.MapPost("/evaluation-models", async (
            CreateEvaluationModelRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateEvaluationModelAsync(request.Code, request.Name, request.Description, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/evaluation-models/{id}", new { id });
        });

        group.MapPost("/evaluation-models/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetEvaluationModelActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/key-stage-policies", async (Guid keyStageId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetKeyStagePoliciesAsync(keyStageId, ct)));

        group.MapPost("/key-stage-policies", async (
            CreateKeyStagePolicyRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.CreateKeyStagePolicyAsync(
                    request.KeyStageId, request.AcademicYearId, request.EvaluationModelCode,
                    request.AchievementScaleId, request.AssessmentSchemeId, request.GradeScaleId, request.PromotionPolicyId, ct);
                return Results.Created($"/api/v1/org/key-stage-policies/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/key-stage-policies/{policyId:guid}/publish", async (
            Guid policyId, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.PublishKeyStagePolicyAsync(policyId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/key-stage-policies/resolve", async (
            Guid gradeId, Guid academicYearId, DateOnly asOf, IKeyStagePolicyResolver resolver, CancellationToken ct) =>
        {
            var policy = await resolver.ResolveAsync(gradeId, academicYearId, asOf, ct);
            return policy is null ? Results.NotFound() : Results.Ok(policy);
        });

        group.MapGet("/working-days", async (Guid schoolId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetWorkingDaysAsync(schoolId, ct)));

        group.MapPost("/schools/{schoolId:guid}/working-days", async (
            Guid schoolId, SetWorkingDayRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await service.SetWorkingDayAsync(schoolId, request.DayOfWeek, request.IsWorkingDay, ct);
            return Results.NoContent();
        });

        group.MapGet("/holiday-types", async (IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetHolidayTypesAsync(ct)));

        group.MapPost("/holiday-types", async (
            CreateHolidayTypeRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateHolidayTypeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/holiday-types/{id}", new { id });
        });

        group.MapPost("/holiday-types/{id:guid}/status", async (
            Guid id, SetActiveRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetHolidayTypeActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/holidays", async (Guid schoolId, IOrgAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetHolidaysAsync(schoolId, ct)));

        group.MapPost("/schools/{schoolId:guid}/holidays", async (
            Guid schoolId, CreateHolidayRequest request, IOrgAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.CreateHolidayAsync(schoolId, request.Date, request.HolidayTypeCode, request.NameEn, request.NameDv, ct);
                return Results.Created($"/api/v1/org/holidays/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/calendar/is-school-day", async (
            Guid schoolId, DateOnly date, ISchoolCalendarService calendar, CancellationToken ct) =>
            Results.Ok(new { isSchoolDay = await calendar.IsSchoolDayAsync(schoolId, date, ct) }));

        return endpoints;
    }
}

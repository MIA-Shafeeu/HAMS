using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Access;
using HAMS.Platform.Common.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Endpoints;

public sealed record CreateCurriculumFrameworkRequest(string Code, string Name, string? Description);
public sealed record CreateLearningAreaRequest(Guid CurriculumFrameworkId, string Code, string Name, int DisplayOrder);
public sealed record CreateDeliveryModeRequest(string Code, string Name, int DisplayOrder);
public sealed record CreateMediumOfInstructionRequest(string Code, string Name, int DisplayOrder);
public sealed record CreateSubjectRequest(Guid SchoolId, Guid LearningAreaId, string Code, string Name, string DeliveryModeCode, string DefaultMediumOfInstructionCode, int DisplayOrder);
public sealed record AddSyllabusGradeApplicabilityRequest(Guid GradeId);

/// <summary>Curriculum & Syllabus admin surface (build plan Phase 2 scope). Mutations require a live School/System Administrator check.</summary>
internal static class CurriculumEndpoints
{
    public static IEndpointRouteBuilder MapCurriculumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/org").WithTags("Curriculum").RequireAuthorization();

        group.MapGet("/curriculum-frameworks", async (ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetCurriculumFrameworksAsync(ct)));

        group.MapPost("/curriculum-frameworks", async (
            CreateCurriculumFrameworkRequest request, ICurriculumAdminService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateCurriculumFrameworkAsync(request.Code, request.Name, request.Description, ct);
            return Results.Created($"/api/v1/org/curriculum-frameworks/{id}", new { id });
        });

        group.MapGet("/learning-areas", async (ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetLearningAreasAsync(ct)));

        group.MapPost("/learning-areas", async (
            CreateLearningAreaRequest request, ICurriculumAdminService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateLearningAreaAsync(request.CurriculumFrameworkId, request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/learning-areas/{id}", new { id });
        });

        group.MapGet("/delivery-modes", async (ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetDeliveryModesAsync(ct)));

        group.MapGet("/delivery-modes/all", async (ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllDeliveryModesAsync(ct)));

        group.MapPost("/delivery-modes", async (
            CreateDeliveryModeRequest request, ICurriculumAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateDeliveryModeAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/delivery-modes/{id}", new { id });
        });

        group.MapPost("/delivery-modes/{id:guid}/status", async (
            Guid id, SetActiveRequest request, ICurriculumAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetDeliveryModeActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/mediums-of-instruction", async (ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetMediumsOfInstructionAsync(ct)));

        group.MapGet("/mediums-of-instruction/all", async (ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllMediumsOfInstructionAsync(ct)));

        group.MapPost("/mediums-of-instruction", async (
            CreateMediumOfInstructionRequest request, ICurriculumAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await service.CreateMediumOfInstructionAsync(request.Code, request.Name, request.DisplayOrder, ct);
            return Results.Created($"/api/v1/org/mediums-of-instruction/{id}", new { id });
        });

        group.MapPost("/mediums-of-instruction/{id:guid}/status", async (
            Guid id, SetActiveRequest request, ICurriculumAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await service.SetMediumOfInstructionActiveAsync(id, request.IsActive, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });

        group.MapGet("/subjects", async (Guid schoolId, ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetSubjectsAsync(schoolId, ct)));

        group.MapPost("/subjects", async (
            CreateSubjectRequest request, ICurriculumAdminService service, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var id = await service.CreateSubjectAsync(
                    request.SchoolId, request.LearningAreaId, request.Code, request.Name,
                    request.DeliveryModeCode, request.DefaultMediumOfInstructionCode, request.DisplayOrder, ct);
                return Results.Created($"/api/v1/org/subjects/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/syllabuses", async (
            Guid subjectId, ISyllabusPublishingService publishing, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await publishing.CreateInitialDraftAsync(subjectId, ct);
            return Results.Created($"/api/v1/org/syllabuses/{id}", new { id });
        });

        group.MapPost("/syllabuses/{syllabusId:guid}/revise", async (
            Guid syllabusId, ISyllabusPublishingService publishing, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            var id = await publishing.CreateDraftRevisionAsync(syllabusId, ct);
            return Results.Created($"/api/v1/org/syllabuses/{id}", new { id });
        });

        group.MapPost("/syllabuses/{syllabusId:guid}/publish", async (
            Guid syllabusId, ISyllabusPublishingService publishing, IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                await publishing.PublishAsync(syllabusId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/subjects/{subjectId:guid}/syllabuses", async (Guid subjectId, ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetSyllabusesForSubjectAsync(subjectId, ct)));

        group.MapGet("/syllabuses/{syllabusId:guid}/grade-applicability", async (Guid syllabusId, ICurriculumAdminService service, CancellationToken ct) =>
            Results.Ok(await service.GetSyllabusGradeApplicabilitiesAsync(syllabusId, ct)));

        group.MapPost("/syllabuses/{syllabusId:guid}/grade-applicability", async (
            Guid syllabusId, AddSyllabusGradeApplicabilityRequest request, ICurriculumAdminService service,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            await service.AddSyllabusGradeApplicabilityAsync(syllabusId, request.GradeId, ct);
            return Results.NoContent();
        });

        group.MapPost("/syllabuses/{syllabusId:guid}/import", async (
            Guid syllabusId, HttpRequest request, ICurriculumCsvImportService importService,
            IRoleMembershipQuery roles, ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!await roles.IsSystemOrSchoolAdminAsync(user, clock, ct)) return Results.Forbid();

            try
            {
                var result = await importService.ImportAsync(syllabusId, request.Body, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/syllabuses/resolve", async (
            Guid subjectId, Guid gradeId, ISyllabusResolver resolver, CancellationToken ct) =>
        {
            var syllabus = await resolver.ResolveAsync(subjectId, gradeId, ct);
            return syllabus is null ? Results.NotFound() : Results.Ok(syllabus);
        });

        group.MapGet("/syllabuses/{syllabusId:guid}/tree", async (Guid syllabusId, OrgDbContext db, CancellationToken ct) =>
        {
            var strands = await db.Strands.Where(s => s.SyllabusId == syllabusId).OrderBy(s => s.DisplayOrder).ToListAsync(ct);
            var strandIds = strands.Select(s => s.Id).ToList();

            var subStrands = await db.SubStrands.Where(ss => strandIds.Contains(ss.StrandId)).OrderBy(ss => ss.DisplayOrder).ToListAsync(ct);
            var subStrandIds = subStrands.Select(ss => ss.Id).ToList();

            var outcomes = await db.LearningOutcomes.Where(o => subStrandIds.Contains(o.SubStrandId)).OrderBy(o => o.DisplayOrder).ToListAsync(ct);
            var outcomeIds = outcomes.Select(o => o.Id).ToList();

            var indicators = await db.Indicators.Where(i => outcomeIds.Contains(i.LearningOutcomeId)).OrderBy(i => i.DisplayOrder).ToListAsync(ct);

            return Results.Ok(new { strands, subStrands, outcomes, indicators });
        });

        return endpoints;
    }
}

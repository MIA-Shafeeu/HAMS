using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.Attendance.Application;
using HAMS.LearningDelivery.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Common.Contracts;
using HAMS.ReportingAnalyticsAudit.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HAMS.CommunicationPortals.Endpoints;

public sealed record SubmitHomeworkRequest(string? SubmissionText, string? FileReference);

/// <summary>
/// The student portal's read surface (build plan Phase 10 scope) — deliberately narrower than the
/// guardian portal: a student only ever reads their own data (no relationship/permission lookup
/// needed, <see cref="ICurrentUser.PersonId"/> already <b>is</b> the student), and intervention
/// updates are deliberately not exposed here at all — see <c>InterventionCaseService.GetCasesForStudentAsync</c>'s
/// remarks; whether a student should see their own support-case history is a real school-policy
/// question this phase doesn't answer, so it's left unbuilt rather than guessed at.
/// </summary>
internal static class StudentPortalEndpoints
{
    public static IEndpointRouteBuilder MapStudentPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/portal/student").WithTags("StudentPortal").RequireAuthorization();

        group.MapGet("/results", async (IKeyStageEvaluationService evaluationService, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStudent || user.PersonId is not { } studentPersonId) return Results.Forbid();

            return Results.Ok(await evaluationService.GetAllCurrentForStudentAsync(studentPersonId, ct));
        });

        group.MapGet("/attendance", async (
            DateOnly fromDate, DateOnly toDate, IAttendanceQueryService attendanceQueryService, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStudent || user.PersonId is not { } studentPersonId) return Results.Forbid();

            return Results.Ok(await attendanceQueryService.GetDailyRecordsAsync(studentPersonId, fromDate, toDate, ct));
        });

        group.MapGet("/report-cards", async (IReportCardService reportCardService, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStudent || user.PersonId is not { } studentPersonId) return Results.Forbid();

            return Results.Ok(await reportCardService.GetPublishedForStudentAsync(studentPersonId, ct));
        });

        group.MapGet("/report-cards/{reportCardId:guid}/pdf", async (
            Guid reportCardId, IReportCardService reportCardService, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStudent || user.PersonId is not { } studentPersonId) return Results.Forbid();

            var reportCard = await reportCardService.GetAsync(reportCardId, ct);
            if (reportCard is null || reportCard.StudentPersonId != studentPersonId || reportCard.Status != RecordStatus.Published)
            {
                return Results.NotFound();
            }

            var bytes = await reportCardService.RenderPdfAsync(reportCardId, ct);
            return Results.File(bytes, "application/pdf", "report-card.pdf");
        });

        group.MapGet("/homework", async (
            Guid academicYearId, IStudentEnrollmentService enrollmentService, IHomeworkService homeworkService,
            ICurrentUser user, IClock clock, CancellationToken ct) =>
        {
            if (!user.IsStudent || user.PersonId is not { } studentPersonId) return Results.Forbid();

            var enrollment = await enrollmentService.GetActiveEnrollmentAsync(studentPersonId, academicYearId, clock.TodayUtc, ct);
            if (enrollment is null)
            {
                return Results.Ok(Array.Empty<object>());
            }

            return Results.Ok(await homeworkService.ListForClassAsync(enrollment.ClassId, ct));
        });

        group.MapPost("/homework/{homeworkId:guid}/submissions", async (
            Guid homeworkId, SubmitHomeworkRequest request, IHomeworkSubmissionService submissions, ICurrentUser user, CancellationToken ct) =>
        {
            if (!user.IsStudent || user.PersonId is not { } studentPersonId) return Results.Forbid();

            try
            {
                var id = await submissions.SubmitAsync(homeworkId, studentPersonId, request.SubmissionText, request.FileReference, ct);
                return Results.Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return endpoints;
    }
}

using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.Attendance.Application;
using HAMS.CommunicationPortals.Domain;
using HAMS.Intervention.Application;
using HAMS.Intervention.Domain;
using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Domain;
using HAMS.ReportingAnalyticsAudit.Application;
using HAMS.ReportingAnalyticsAudit.Domain;

namespace HAMS.CommunicationPortals.Application;

internal sealed class GuardianPortalService(
    IGuardianRelationshipService guardianRelationshipService, IKeyStageEvaluationService keyStageEvaluationService,
    IAttendanceQueryService attendanceQueryService, IInterventionCaseService interventionCaseService, IReportCardService reportCardService,
    IStudentEnrollmentService studentEnrollmentService, IHomeworkService homeworkService,
    IBehaviourIncidentService behaviourIncidentService, IBehaviourCategoryLookup behaviourCategoryLookup,
    IGuardianAcknowledgementService acknowledgementService,
    IClock clock)
    : IGuardianPortalService
{
    public Task<IReadOnlyList<GuardianStudentSummary>> GetMyStudentsAsync(Guid guardianPersonId, CancellationToken cancellationToken = default)
        => guardianRelationshipService.GetStudentsForGuardianAsync(guardianPersonId, clock.TodayUtc, cancellationToken);

    public async Task<IReadOnlyList<KeyStageEvaluation>> GetStudentResultsAsync(
        Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default)
    {
        await RequireCanViewAsync(guardianPersonId, studentPersonId, s => s.CanViewAcademicRecords, cancellationToken);
        return await keyStageEvaluationService.GetAllCurrentForStudentAsync(studentPersonId, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecordSummary>> GetStudentAttendanceAsync(
        Guid guardianPersonId, Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        await RequireCanViewAsync(guardianPersonId, studentPersonId, s => s.CanViewAttendance, cancellationToken);
        return await attendanceQueryService.GetDailyRecordsAsync(studentPersonId, fromDate, toDate, cancellationToken);
    }

    public async Task<IReadOnlyList<InterventionUpdateSummary>> GetStudentInterventionUpdatesAsync(
        Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default)
    {
        await RequireCanViewAsync(guardianPersonId, studentPersonId, s => s.CanViewInterventionUpdates, cancellationToken);

        var cases = await interventionCaseService.GetCasesForStudentAsync(studentPersonId, cancellationToken);
        return cases
            .Select(c => new InterventionUpdateSummary(c.SubjectId, c.OpenedDate, c.Status == InterventionCaseStatus.Open, c.ClosedDate))
            .ToList();
    }

    public async Task<IReadOnlyList<ReportCard>> GetStudentReportCardsAsync(
        Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default)
    {
        await RequireCanViewAsync(guardianPersonId, studentPersonId, s => s.CanViewAcademicRecords, cancellationToken);
        return await reportCardService.GetPublishedForStudentAsync(studentPersonId, cancellationToken);
    }

    public async Task<byte[]> GetStudentReportCardPdfAsync(
        Guid guardianPersonId, Guid studentPersonId, Guid reportCardId, CancellationToken cancellationToken = default)
    {
        await RequireCanViewAsync(guardianPersonId, studentPersonId, s => s.CanViewAcademicRecords, cancellationToken);

        var reportCard = await reportCardService.GetAsync(reportCardId, cancellationToken);
        if (reportCard is null || reportCard.StudentPersonId != studentPersonId || reportCard.Status != RecordStatus.Published)
        {
            throw new InvalidOperationException("Report card not found.");
        }

        return await reportCardService.RenderPdfAsync(reportCardId, cancellationToken);
    }

    public async Task<IReadOnlyList<Homework>> GetStudentHomeworkAsync(
        Guid guardianPersonId, Guid studentPersonId, Guid academicYearId, CancellationToken cancellationToken = default)
    {
        await RequireCanViewAsync(guardianPersonId, studentPersonId, s => s.CanViewAcademicRecords, cancellationToken);

        var enrollment = await studentEnrollmentService.GetActiveEnrollmentAsync(studentPersonId, academicYearId, clock.TodayUtc, cancellationToken);
        if (enrollment is null)
        {
            return [];
        }

        return await homeworkService.ListForClassAsync(enrollment.ClassId, cancellationToken);
    }

    public async Task<IReadOnlyList<BehaviourIncidentSummary>> GetStudentBehaviourSummaryAsync(
        Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken = default)
    {
        await RequireCanViewAsync(guardianPersonId, studentPersonId, s => s.CanViewBehaviourRecords, cancellationToken);

        var incidents = await behaviourIncidentService.GetForStudentAsync(studentPersonId, cancellationToken);
        var summaries = new List<BehaviourIncidentSummary>();
        foreach (var incident in incidents.Where(i => i.Status == WorkflowStatus.Approved))
        {
            var category = await behaviourCategoryLookup.GetAsync(incident.BehaviourCategoryId, cancellationToken);
            if (category is not null)
            {
                summaries.Add(new BehaviourIncidentSummary(category.Name, category.IsPositive, incident.OccurredDate));
            }
        }

        return summaries;
    }

    public async Task<Guid> AcknowledgeAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        await RequireRelationshipExistsAsync(guardianPersonId, studentPersonId, cancellationToken);
        return await acknowledgementService.AcknowledgeAsync(guardianPersonId, studentPersonId, entityType, entityId, cancellationToken);
    }

    public async Task<GuardianAcknowledgement?> GetAcknowledgementAsync(
        Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        await RequireRelationshipExistsAsync(guardianPersonId, studentPersonId, cancellationToken);
        return await acknowledgementService.GetAsync(guardianPersonId, studentPersonId, entityType, entityId, cancellationToken);
    }

    private async Task RequireRelationshipExistsAsync(Guid guardianPersonId, Guid studentPersonId, CancellationToken cancellationToken)
    {
        var students = await guardianRelationshipService.GetStudentsForGuardianAsync(guardianPersonId, clock.TodayUtc, cancellationToken);
        if (!students.Any(s => s.StudentPersonId == studentPersonId))
        {
            throw new UnauthorizedAccessException("You do not have permission to act on behalf of this student.");
        }
    }

    private async Task RequireCanViewAsync(
        Guid guardianPersonId, Guid studentPersonId, Func<GuardianStudentSummary, bool> canView, CancellationToken cancellationToken)
    {
        var students = await guardianRelationshipService.GetStudentsForGuardianAsync(guardianPersonId, clock.TodayUtc, cancellationToken);
        var relationship = students.SingleOrDefault(s => s.StudentPersonId == studentPersonId);

        if (relationship is null || !canView(relationship))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this information for this student.");
        }
    }
}

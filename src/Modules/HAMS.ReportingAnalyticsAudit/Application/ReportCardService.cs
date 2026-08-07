using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.LearningDelivery.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using HAMS.ReportingAnalyticsAudit.Domain;
using HAMS.ReportingAnalyticsAudit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.ReportingAnalyticsAudit.Application;

internal sealed class ReportCardService(
    ReportingAnalyticsAuditDbContext dbContext, IWorkflowEngine workflowEngine, IKeyStageEvaluationService keyStageEvaluationService,
    IKeyCompetencyEvidenceService keyCompetencyEvidenceService, IEvaluationPeriodLookup evaluationPeriodLookup,
    ISubjectLookup subjectLookup, IKeyCompetencyLookup keyCompetencyLookup, IStudentEnrollmentService enrollmentService, IClock clock)
    : IReportCardService
{
    public async Task<Guid> PrepareAsync(PrepareReportCardRequest request, CancellationToken cancellationToken = default)
    {
        var window = await evaluationPeriodLookup.GetWindowAsync(request.EvaluationPeriodId, cancellationToken)
            ?? throw new InvalidOperationException("Evaluation period not found.");

        var evaluations = (await keyStageEvaluationService.GetAllCurrentForStudentAsync(request.StudentPersonId, cancellationToken))
            .Where(e => e.EvaluationPeriodId == request.EvaluationPeriodId)
            .ToList();

        if (evaluations.Count == 0)
        {
            throw new InvalidOperationException("This student has no current subject evaluations for that period.");
        }

        var competencySummaries = await keyCompetencyEvidenceService.GetSummaryForStudentAsync(
            request.StudentPersonId, window.StartDate, window.EndDate, cancellationToken);

        var reportCard = new ReportCard
        {
            Id = Guid.NewGuid(),
            StudentPersonId = request.StudentPersonId,
            AcademicYearId = request.AcademicYearId,
            EvaluationPeriodId = request.EvaluationPeriodId,
            NarrativeEn = request.NarrativeEn,
            NarrativeDv = request.NarrativeDv,
            NextStepsEn = request.NextStepsEn,
            NextStepsDv = request.NextStepsDv,
            PreparedByPersonId = request.PreparedByPersonId,
            PreparedAtUtc = clock.UtcNow,
        };
        dbContext.ReportCards.Add(reportCard);

        foreach (var evaluation in evaluations)
        {
            dbContext.ReportCardSubjectResults.Add(new ReportCardSubjectResult
            {
                Id = Guid.NewGuid(),
                ReportCardId = reportCard.Id,
                SubjectId = evaluation.SubjectId,
                SourceKeyStageEvaluationId = evaluation.Id,
                AchievementLevelId = evaluation.OverallAchievementLevelId,
                Percentage = evaluation.OverallPercentage,
                GradeBandId = evaluation.OverallGradeBandId,
            });
        }

        foreach (var summary in competencySummaries)
        {
            dbContext.ReportCardKeyCompetencySummaries.Add(new ReportCardKeyCompetencySummary
            {
                Id = Guid.NewGuid(),
                ReportCardId = reportCard.Id,
                KeyCompetencyId = summary.KeyCompetencyId,
                EvidenceCount = summary.EvidenceCount,
                AverageRatingScore = summary.AverageRatingScore,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return reportCard.Id;
    }

    public async Task SubmitAsync(Guid reportCardId, CancellationToken cancellationToken = default)
    {
        var reportCard = await GetRequiredAsync(reportCardId, cancellationToken);
        reportCard.ApprovalStatus = workflowEngine.Transition(reportCard.ApprovalStatus, WorkflowAction.Submit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginReviewAsync(Guid reportCardId, CancellationToken cancellationToken = default)
    {
        var reportCard = await GetRequiredAsync(reportCardId, cancellationToken);
        reportCard.ApprovalStatus = workflowEngine.Transition(reportCard.ApprovalStatus, WorkflowAction.Review);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid reportCardId, CancellationToken cancellationToken = default)
    {
        var reportCard = await GetRequiredAsync(reportCardId, cancellationToken);
        reportCard.ApprovalStatus = workflowEngine.Transition(reportCard.ApprovalStatus, WorkflowAction.Approve);
        reportCard.Status = RecordStatus.Published;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid reportCardId, CancellationToken cancellationToken = default)
    {
        var reportCard = await GetRequiredAsync(reportCardId, cancellationToken);
        reportCard.ApprovalStatus = workflowEngine.Transition(reportCard.ApprovalStatus, WorkflowAction.Reject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReturnAsync(Guid reportCardId, CancellationToken cancellationToken = default)
    {
        var reportCard = await GetRequiredAsync(reportCardId, cancellationToken);
        reportCard.ApprovalStatus = workflowEngine.Transition(reportCard.ApprovalStatus, WorkflowAction.Return);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> ReviseApprovedReportCardAsync(
        Guid reportCardId, ReviseReportCardRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await GetRequiredAsync(reportCardId, cancellationToken);
        if (existing.Status is not (RecordStatus.Published or RecordStatus.Locked))
        {
            throw new InvalidOperationException("Only an already-Published/Locked report card needs this correction path — a Draft one can just be re-prepared.");
        }

        var subjectResults = await dbContext.ReportCardSubjectResults.Where(r => r.ReportCardId == reportCardId).ToListAsync(cancellationToken);
        var competencySummaries = await dbContext.ReportCardKeyCompetencySummaries.Where(s => s.ReportCardId == reportCardId).ToListAsync(cancellationToken);

        var revised = new ReportCard
        {
            Id = Guid.NewGuid(),
            StudentPersonId = existing.StudentPersonId,
            AcademicYearId = existing.AcademicYearId,
            EvaluationPeriodId = existing.EvaluationPeriodId,
            NarrativeEn = request.NarrativeEn,
            NarrativeDv = request.NarrativeDv,
            NextStepsEn = request.NextStepsEn,
            NextStepsDv = request.NextStepsDv,
            PreparedByPersonId = existing.PreparedByPersonId,
            PreparedAtUtc = clock.UtcNow,
            ApprovalStatus = WorkflowStatus.Approved,
            Version = existing.Version + 1,
            IsCurrent = true,
            SupersedesId = existing.Id,
            Status = RecordStatus.Published,
        };

        using (ImmutableRecordCorrectionScope.Enter())
        {
            existing.IsCurrent = false;
            existing.Status = RecordStatus.Superseded;
            existing.SupersededById = revised.Id;

            dbContext.ReportCards.Add(revised);

            // The underlying academic record carries forward unchanged onto the new version — only
            // the narrative/next-steps text was corrected.
            foreach (var subjectResult in subjectResults)
            {
                dbContext.ReportCardSubjectResults.Add(new ReportCardSubjectResult
                {
                    Id = Guid.NewGuid(),
                    ReportCardId = revised.Id,
                    SubjectId = subjectResult.SubjectId,
                    SourceKeyStageEvaluationId = subjectResult.SourceKeyStageEvaluationId,
                    AchievementLevelId = subjectResult.AchievementLevelId,
                    Percentage = subjectResult.Percentage,
                    GradeBandId = subjectResult.GradeBandId,
                });
            }

            foreach (var summary in competencySummaries)
            {
                dbContext.ReportCardKeyCompetencySummaries.Add(new ReportCardKeyCompetencySummary
                {
                    Id = Guid.NewGuid(),
                    ReportCardId = revised.Id,
                    KeyCompetencyId = summary.KeyCompetencyId,
                    EvidenceCount = summary.EvidenceCount,
                    AverageRatingScore = summary.AverageRatingScore,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return revised.Id;
    }

    public async Task<ReportCard?> GetAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => await dbContext.ReportCards.FindAsync([reportCardId], cancellationToken);

    public async Task<IReadOnlyList<ReportCard>> GetPublishedForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => await dbContext.ReportCards
            .Where(r => r.StudentPersonId == studentPersonId && r.IsCurrent && r.Status == RecordStatus.Published)
            .OrderByDescending(r => r.EvaluationPeriodId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportCardSubjectResult>> GetSubjectResultsAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => await dbContext.ReportCardSubjectResults.Where(r => r.ReportCardId == reportCardId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportCardKeyCompetencySummary>> GetKeyCompetencySummariesAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => await dbContext.ReportCardKeyCompetencySummaries.Where(s => s.ReportCardId == reportCardId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClassRosterEntry>> GetStudentsNeedingReportCardAsync(
        Guid gradeId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var roster = await enrollmentService.GetActiveRosterForGradeAsync(gradeId, academicYearId, asOf, cancellationToken);

        var preparedStudentIds = (await dbContext.ReportCards
            .Where(r => r.AcademicYearId == academicYearId && r.EvaluationPeriodId == evaluationPeriodId)
            .Select(r => r.StudentPersonId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        return roster.Where(r => !preparedStudentIds.Contains(r.StudentPersonId)).ToList();
    }

    public async Task<byte[]> RenderPdfAsync(Guid reportCardId, CancellationToken cancellationToken = default)
    {
        var reportCard = await GetRequiredAsync(reportCardId, cancellationToken);
        var subjectResults = await GetSubjectResultsAsync(reportCardId, cancellationToken);
        var competencySummaries = await GetKeyCompetencySummariesAsync(reportCardId, cancellationToken);

        var subjectNames = new Dictionary<Guid, string>();
        foreach (var subjectId in subjectResults.Select(r => r.SubjectId).Distinct())
        {
            subjectNames[subjectId] = await subjectLookup.GetNameAsync(subjectId, cancellationToken) ?? "(unknown subject)";
        }

        var competencyNames = (await keyCompetencyLookup.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id);

        return ReportCardPdfRenderer.Render(reportCard, subjectResults, subjectNames, competencySummaries, competencyNames);
    }

    private async Task<ReportCard> GetRequiredAsync(Guid reportCardId, CancellationToken cancellationToken)
        => await dbContext.ReportCards.FindAsync([reportCardId], cancellationToken)
            ?? throw new InvalidOperationException("Report card not found.");
}

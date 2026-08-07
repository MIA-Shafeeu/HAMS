using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.LearningDelivery.Application;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using HAMS.ReportingAnalyticsAudit.Application;
using HAMS.ReportingAnalyticsAudit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.ReportingAnalyticsAudit.Tests;

public class ReportCardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
    private static readonly EvaluationPeriodWindow DefaultWindow = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30));

    private static ReportingAnalyticsAuditDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ReportingAnalyticsAuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SaveChangesGuardInterceptor())
            .Options);

    private static ReportCardService CreateService(
        ReportingAnalyticsAuditDbContext db, KeyStageEvaluation[]? evaluations = null, KeyCompetencySummary[]? competencySummaries = null,
        Guid? evaluationPeriodId = null, IReadOnlyDictionary<Guid, string>? subjectNames = null, KeyCompetencyName[]? competencyNames = null,
        HAMS.PeopleEnrollment.Domain.StudentEnrollment[]? enrollments = null)
        => new(
            db, new WorkflowEngine(), new FakeKeyStageEvaluationService(evaluations ?? []),
            new FakeKeyCompetencyEvidenceService(competencySummaries ?? []),
            new FakeEvaluationPeriodLookup(new Dictionary<Guid, EvaluationPeriodWindow> { [evaluationPeriodId ?? Guid.NewGuid()] = DefaultWindow }),
            new FakeSubjectLookup(subjectNames ?? new Dictionary<Guid, string>()),
            new FakeKeyCompetencyLookup(competencyNames ?? []),
            new FakeStudentEnrollmentService(enrollments ?? []),
            new FakeClock(Now));

    private static PrepareReportCardRequest CreateRequest(Guid studentId, Guid academicYearId, Guid evaluationPeriodId) => new(
        studentId, academicYearId, evaluationPeriodId, "Making good progress.", "ރަނގަޅު ކުރިއެރުމެއް.", "Keep practising reading.", "ކިޔެވުން ފަރިތަކުރައްވާ.", Guid.NewGuid());

    [Fact]
    public async Task PrepareAsync_throws_when_the_evaluation_period_does_not_exist()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PrepareAsync(CreateRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task PrepareAsync_throws_when_the_student_has_no_current_evaluations_for_that_period()
    {
        await using var db = CreateContext();
        var periodId = Guid.NewGuid();
        var service = CreateService(db, evaluationPeriodId: periodId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PrepareAsync(CreateRequest(Guid.NewGuid(), Guid.NewGuid(), periodId)));
    }

    [Fact]
    public async Task PrepareAsync_creates_a_Draft_card_and_snapshots_subject_results_and_competency_summaries()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var achievementLevelId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation
        {
            Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId,
            KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallAchievementLevelId = achievementLevelId,
        };
        var keyCompetencyId = Guid.NewGuid();
        var summary = new KeyCompetencySummary(keyCompetencyId, 3, 2.5);
        var service = CreateService(db, [evaluation], [summary], periodId);

        var reportCardId = await service.PrepareAsync(CreateRequest(studentId, academicYearId, periodId));

        var reportCard = await db.ReportCards.SingleAsync(r => r.Id == reportCardId);
        Assert.Equal(studentId, reportCard.StudentPersonId);
        Assert.Equal(WorkflowStatus.Draft, reportCard.ApprovalStatus);
        Assert.Equal(RecordStatus.Draft, reportCard.Status);
        Assert.Equal(Now, reportCard.PreparedAtUtc);

        var subjectResult = await db.ReportCardSubjectResults.SingleAsync(r => r.ReportCardId == reportCardId);
        Assert.Equal(subjectId, subjectResult.SubjectId);
        Assert.Equal(evaluation.Id, subjectResult.SourceKeyStageEvaluationId);
        Assert.Equal(achievementLevelId, subjectResult.AchievementLevelId);

        var competencySummary = await db.ReportCardKeyCompetencySummaries.SingleAsync(s => s.ReportCardId == reportCardId);
        Assert.Equal(keyCompetencyId, competencySummary.KeyCompetencyId);
        Assert.Equal(3, competencySummary.EvidenceCount);
        Assert.Equal(2.5, competencySummary.AverageRatingScore);
    }

    private static async Task<Guid> PrepareAndApproveAsync(ReportingAnalyticsAuditDbContext db, ReportCardService service, Guid studentId, Guid periodId)
    {
        var reportCardId = await service.PrepareAsync(CreateRequest(studentId, Guid.NewGuid(), periodId));
        await service.SubmitAsync(reportCardId);
        await service.BeginReviewAsync(reportCardId);
        await service.ApproveAsync(reportCardId);
        return reportCardId;
    }

    [Fact]
    public async Task Full_pipeline_from_submit_to_approve_publishes_the_report_card()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 88m };
        var service = CreateService(db, [evaluation], evaluationPeriodId: periodId);

        var reportCardId = await PrepareAndApproveAsync(db, service, studentId, periodId);

        var reportCard = await db.ReportCards.SingleAsync(r => r.Id == reportCardId);
        Assert.Equal(WorkflowStatus.Approved, reportCard.ApprovalStatus);
        Assert.Equal(RecordStatus.Published, reportCard.Status);
    }

    [Fact]
    public async Task RejectAsync_moves_to_Rejected_without_publishing()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 60m };
        var service = CreateService(db, [evaluation], evaluationPeriodId: periodId);
        var reportCardId = await service.PrepareAsync(CreateRequest(studentId, Guid.NewGuid(), periodId));
        await service.SubmitAsync(reportCardId);
        await service.BeginReviewAsync(reportCardId);

        await service.RejectAsync(reportCardId);

        var reportCard = await db.ReportCards.SingleAsync(r => r.Id == reportCardId);
        Assert.Equal(WorkflowStatus.Rejected, reportCard.ApprovalStatus);
        Assert.Equal(RecordStatus.Draft, reportCard.Status);
    }

    [Fact]
    public async Task Returned_report_cards_can_be_resubmitted()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 60m };
        var service = CreateService(db, [evaluation], evaluationPeriodId: periodId);
        var reportCardId = await service.PrepareAsync(CreateRequest(studentId, Guid.NewGuid(), periodId));
        await service.SubmitAsync(reportCardId);
        await service.BeginReviewAsync(reportCardId);
        await service.ReturnAsync(reportCardId);

        await service.SubmitAsync(reportCardId);

        var reportCard = await db.ReportCards.SingleAsync(r => r.Id == reportCardId);
        Assert.Equal(WorkflowStatus.Submitted, reportCard.ApprovalStatus);
    }

    [Fact]
    public async Task ReviseApprovedReportCardAsync_supersedes_the_original_and_carries_forward_the_snapshot_rows_unchanged()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 72m };
        var summary = new KeyCompetencySummary(Guid.NewGuid(), 4, 3.0);
        var service = CreateService(db, [evaluation], [summary], periodId);
        var originalId = await PrepareAndApproveAsync(db, service, studentId, periodId);

        var revisedId = await service.ReviseApprovedReportCardAsync(
            originalId, new ReviseReportCardRequest("Corrected narrative.", "އިސްލާހުކުރި ނަރޭޓިވް.", "Corrected next steps.", "އިސްލާހުކުރި ދެން ފިޔަވަޅު."));

        var original = await db.ReportCards.AsNoTracking().SingleAsync(r => r.Id == originalId);
        var revised = await db.ReportCards.AsNoTracking().SingleAsync(r => r.Id == revisedId);

        Assert.False(original.IsCurrent);
        Assert.Equal(RecordStatus.Superseded, original.Status);
        Assert.Equal(revisedId, original.SupersededById);

        Assert.True(revised.IsCurrent);
        Assert.Equal(RecordStatus.Published, revised.Status);
        Assert.Equal(originalId, revised.SupersedesId);
        Assert.Equal("Corrected narrative.", revised.NarrativeEn);
        Assert.Equal(2, revised.Version);

        var revisedSubjectResult = await db.ReportCardSubjectResults.AsNoTracking().SingleAsync(r => r.ReportCardId == revisedId);
        Assert.Equal(subjectId, revisedSubjectResult.SubjectId);
        Assert.Equal(72m, revisedSubjectResult.Percentage);

        var revisedSummary = await db.ReportCardKeyCompetencySummaries.AsNoTracking().SingleAsync(s => s.ReportCardId == revisedId);
        Assert.Equal(4, revisedSummary.EvidenceCount);
    }

    [Fact]
    public async Task ReviseApprovedReportCardAsync_rejects_a_report_card_that_is_still_Draft()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 72m };
        var service = CreateService(db, [evaluation], evaluationPeriodId: periodId);
        var reportCardId = await service.PrepareAsync(CreateRequest(studentId, Guid.NewGuid(), periodId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReviseApprovedReportCardAsync(reportCardId, new ReviseReportCardRequest("x", "y", "z", "w")));
    }

    [Fact]
    public async Task Directly_modifying_a_published_report_card_outside_the_service_still_throws()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 72m };
        var service = CreateService(db, [evaluation], evaluationPeriodId: periodId);
        var reportCardId = await PrepareAndApproveAsync(db, service, studentId, periodId);

        var reportCard = await db.ReportCards.SingleAsync(r => r.Id == reportCardId);
        reportCard.NarrativeEn = "Tampered.";

        await Assert.ThrowsAsync<ImmutableRecordMutationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task GetPublishedForStudentAsync_returns_only_current_Published_cards_for_that_student()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var evaluation1 = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 72m };
        var service = CreateService(db, [evaluation1], evaluationPeriodId: periodId);
        var publishedId = await PrepareAndApproveAsync(db, service, studentId, periodId);

        // A second, still-Draft card for the same student must not show up.
        var draftPeriodId = Guid.NewGuid();
        var evaluation2 = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = draftPeriodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 50m };
        var draftService = CreateService(db, [evaluation2], evaluationPeriodId: draftPeriodId);
        await draftService.PrepareAsync(CreateRequest(studentId, Guid.NewGuid(), draftPeriodId));

        var result = await service.GetPublishedForStudentAsync(studentId);

        var found = Assert.Single(result);
        Assert.Equal(publishedId, found.Id);
        Assert.Equal(RecordStatus.Published, found.Status);
        Assert.DoesNotContain(result, r => r.StudentPersonId == otherStudentId);
    }

    [Fact]
    public async Task RenderPdfAsync_produces_non_empty_bytes_starting_with_the_PDF_magic_header()
    {
        // Regression guard for the SixLabors.ImageSharp 1.0.4 -> 3.1.12 transitive version bump
        // (Directory.Packages.props) pinned to fix known CVEs — confirms PdfSharpCore's actual PDF
        // rendering still produces a valid document against the newer major version.
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var keyCompetencyId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 91m };
        var summary = new KeyCompetencySummary(keyCompetencyId, 5, 4.2);
        var service = CreateService(
            db, [evaluation], [summary], periodId,
            subjectNames: new Dictionary<Guid, string> { [subjectId] = "Mathematics" },
            competencyNames: [new KeyCompetencyName(keyCompetencyId, "Thinking Critically & Creatively", "ފުންކޮށް ވިސްނުން")]);
        var reportCardId = await PrepareAndApproveAsync(db, service, studentId, periodId);

        var pdfBytes = await service.RenderPdfAsync(reportCardId);

        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 100, "Rendered PDF is suspiciously small.");
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task RenderPdfAsync_throws_when_the_report_card_does_not_exist()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenderPdfAsync(Guid.NewGuid()));
    }

    private static readonly DateOnly AsOf = new(2026, 8, 5);

    private static HAMS.PeopleEnrollment.Domain.StudentEnrollment CreateEnrollment(Guid studentId, Guid gradeId, Guid academicYearId) => new()
    {
        Id = Guid.NewGuid(), StudentPersonId = studentId, GradeId = gradeId, ClassId = Guid.NewGuid(), AcademicYearId = academicYearId,
        EnrollmentTypeId = Guid.NewGuid(), EffectiveFrom = new DateOnly(2026, 1, 1),
    };

    [Fact]
    public async Task GetStudentsNeedingReportCardAsync_excludes_students_who_already_have_one_prepared_for_that_period()
    {
        await using var db = CreateContext();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var preparedStudentId = Guid.NewGuid();
        var unpreparedStudentId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = preparedStudentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 80m };
        var service = CreateService(
            db, [evaluation], evaluationPeriodId: periodId,
            enrollments: [CreateEnrollment(preparedStudentId, gradeId, academicYearId), CreateEnrollment(unpreparedStudentId, gradeId, academicYearId)]);
        await service.PrepareAsync(CreateRequest(preparedStudentId, academicYearId, periodId));

        var worklist = await service.GetStudentsNeedingReportCardAsync(gradeId, academicYearId, periodId, AsOf);

        Assert.Single(worklist, s => s.StudentPersonId == unpreparedStudentId);
    }

    [Fact]
    public async Task GetStudentsNeedingReportCardAsync_ignores_report_cards_from_a_different_evaluation_period()
    {
        await using var db = CreateContext();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var otherPeriodId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = otherPeriodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), OverallPercentage = 80m };
        var service = CreateService(db, [evaluation], evaluationPeriodId: otherPeriodId, enrollments: [CreateEnrollment(studentId, gradeId, academicYearId)]);
        await service.PrepareAsync(CreateRequest(studentId, academicYearId, otherPeriodId));

        var worklist = await service.GetStudentsNeedingReportCardAsync(gradeId, academicYearId, periodId, AsOf);

        Assert.Single(worklist, s => s.StudentPersonId == studentId);
    }
}

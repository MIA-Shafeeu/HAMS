using HAMS.AssessmentEvaluation.Domain;
using HAMS.Attendance.Application;
using HAMS.CommunicationPortals.Application;
using HAMS.Intervention.Application;
using HAMS.Intervention.Domain;
using HAMS.LearningDelivery.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Workflow.Domain;
using HAMS.ReportingAnalyticsAudit.Domain;

namespace HAMS.CommunicationPortals.Tests;

public class GuardianPortalServiceTests
{
    private static readonly DateOnly Today = new(2026, 8, 5);

    private static GuardianPortalService CreateService(
        IReadOnlyList<GuardianStudentSummary> students, KeyStageEvaluation[]? evaluations = null,
        AttendanceRecordSummary[]? attendance = null, InterventionCase[]? cases = null, ReportCard[]? reportCards = null,
        StudentEnrollment[]? enrollments = null, Homework[]? homeworks = null, BehaviourIncident[]? behaviourIncidents = null,
        (Guid Id, BehaviourCategoryInfo Info)[]? behaviourCategories = null, FakeGuardianAcknowledgementService? acknowledgements = null)
        => new(
            new FakeGuardianRelationshipService([.. students]), new FakeKeyStageEvaluationService(evaluations ?? []),
            new FakeAttendanceQueryService(attendance ?? []), new FakeInterventionCaseService(cases ?? []),
            new FakeReportCardService(reportCards ?? []), new FakeStudentEnrollmentService(enrollments ?? []),
            new FakeHomeworkService(homeworks ?? []), new FakeBehaviourIncidentService(behaviourIncidents ?? []),
            new FakeBehaviourCategoryLookup(behaviourCategories ?? []), acknowledgements ?? new FakeGuardianAcknowledgementService(),
            new FakeClock(Today));

    private static InterventionCase CreateCase(Guid studentId, Guid subjectId, bool isOpen = true) => new()
    {
        Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, AcademicYearId = Guid.NewGuid(),
        InterventionTypeId = Guid.NewGuid(), ConfidentialityTierCode = "RESTRICTED", OpenedByPersonId = Guid.NewGuid(),
        OpenedDate = new DateOnly(2026, 8, 1), Status = isOpen ? InterventionCaseStatus.Open : InterventionCaseStatus.Closed,
        ClosedDate = isOpen ? null : new DateOnly(2026, 8, 4),
    };

    [Fact]
    public async Task GetMyStudentsAsync_returns_whatever_the_relationship_service_reports()
    {
        var studentId = Guid.NewGuid();
        var students = new[] { new GuardianStudentSummary(studentId, true, true, true) };
        var service = CreateService(students);

        var result = await service.GetMyStudentsAsync(Guid.NewGuid());

        Assert.Single(result, s => s.StudentPersonId == studentId);
    }

    [Fact]
    public async Task GetStudentResultsAsync_throws_when_the_guardian_has_no_relationship_with_that_student()
    {
        var service = CreateService([new GuardianStudentSummary(Guid.NewGuid(), true, true, true)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStudentResultsAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetStudentResultsAsync_throws_when_CanViewAcademicRecords_is_false()
    {
        var studentId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, CanViewAcademicRecords: false, CanViewAttendance: true, CanViewInterventionUpdates: true)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStudentResultsAsync(Guid.NewGuid(), studentId));
    }

    [Fact]
    public async Task GetStudentResultsAsync_returns_evaluations_when_authorized()
    {
        var studentId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = Guid.NewGuid(), EvaluationPeriodId = Guid.NewGuid() };
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)], evaluations: [evaluation]);

        var result = await service.GetStudentResultsAsync(Guid.NewGuid(), studentId);

        Assert.Single(result, e => e.Id == evaluation.Id);
    }

    [Fact]
    public async Task GetStudentAttendanceAsync_throws_when_CanViewAttendance_is_false()
    {
        var studentId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, CanViewAcademicRecords: true, CanViewAttendance: false, CanViewInterventionUpdates: true)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetStudentAttendanceAsync(Guid.NewGuid(), studentId, Today, Today));
    }

    [Fact]
    public async Task GetStudentAttendanceAsync_returns_records_when_authorized()
    {
        var studentId = Guid.NewGuid();
        var record = new AttendanceRecordSummary(Today, "ABSENT", null);
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)], attendance: [record]);

        var result = await service.GetStudentAttendanceAsync(Guid.NewGuid(), studentId, Today, Today);

        Assert.Single(result, r => r.AttendanceStatusCode == "ABSENT");
    }

    [Fact]
    public async Task GetStudentInterventionUpdatesAsync_throws_when_CanViewInterventionUpdates_is_false()
    {
        var studentId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, CanViewAcademicRecords: true, CanViewAttendance: true, CanViewInterventionUpdates: false)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStudentInterventionUpdatesAsync(Guid.NewGuid(), studentId));
    }

    [Fact]
    public async Task GetStudentInterventionUpdatesAsync_maps_to_a_minimal_non_sensitive_summary()
    {
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var openCase = CreateCase(studentId, subjectId, isOpen: true);
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)], cases: [openCase]);

        var result = await service.GetStudentInterventionUpdatesAsync(Guid.NewGuid(), studentId);

        var summary = Assert.Single(result);
        Assert.Equal(subjectId, summary.SubjectId);
        Assert.Equal(openCase.OpenedDate, summary.OpenedDate);
        Assert.True(summary.IsOpen);
        Assert.Null(summary.ClosedDate);
    }

    [Fact]
    public async Task GetStudentInterventionUpdatesAsync_reports_closed_cases_correctly()
    {
        var studentId = Guid.NewGuid();
        var closedCase = CreateCase(studentId, Guid.NewGuid(), isOpen: false);
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)], cases: [closedCase]);

        var result = await service.GetStudentInterventionUpdatesAsync(Guid.NewGuid(), studentId);

        var summary = Assert.Single(result);
        Assert.False(summary.IsOpen);
        Assert.Equal(closedCase.ClosedDate, summary.ClosedDate);
    }

    [Fact]
    public async Task GetStudentHomeworkAsync_throws_when_CanViewAcademicRecords_is_false()
    {
        var studentId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, CanViewAcademicRecords: false, CanViewAttendance: true, CanViewInterventionUpdates: true)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStudentHomeworkAsync(Guid.NewGuid(), studentId, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetStudentHomeworkAsync_returns_empty_when_the_student_has_no_active_enrolment_for_that_year()
    {
        var studentId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)]);

        var result = await service.GetStudentHomeworkAsync(Guid.NewGuid(), studentId, Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStudentHomeworkAsync_returns_homework_for_the_students_active_class()
    {
        var studentId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var enrollment = new StudentEnrollment
        {
            Id = Guid.NewGuid(), StudentPersonId = studentId, GradeId = Guid.NewGuid(), ClassId = classId,
            AcademicYearId = academicYearId, EnrollmentTypeId = Guid.NewGuid(), EffectiveFrom = new DateOnly(2026, 1, 1),
        };
        var homework = new Homework
        {
            Id = Guid.NewGuid(), ClassId = classId, SubjectId = Guid.NewGuid(), TitleEn = "Fractions", TitleDv = "ބައި",
            InstructionsEn = "x", InstructionsDv = "x", AssignedDate = Today, DueDate = Today.AddDays(5), AssignedByPersonId = Guid.NewGuid(),
        };
        var service = CreateService(
            [new GuardianStudentSummary(studentId, true, true, true)], enrollments: [enrollment], homeworks: [homework]);

        var result = await service.GetStudentHomeworkAsync(Guid.NewGuid(), studentId, academicYearId);

        Assert.Single(result, h => h.Id == homework.Id);
    }

    [Fact]
    public async Task GetStudentBehaviourSummaryAsync_throws_when_CanViewBehaviourRecords_is_false()
    {
        var studentId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true, CanViewBehaviourRecords: false)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStudentBehaviourSummaryAsync(Guid.NewGuid(), studentId));
    }

    [Fact]
    public async Task GetStudentBehaviourSummaryAsync_excludes_incidents_not_yet_Approved()
    {
        var studentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var draftIncident = new BehaviourIncident
        {
            Id = Guid.NewGuid(), StudentPersonId = studentId, BehaviourCategoryId = categoryId, AcademicYearId = Guid.NewGuid(),
            Description = "x", ConfidentialityTierCode = "RESTRICTED", RecordedByPersonId = Guid.NewGuid(),
            OccurredDate = Today, Status = WorkflowStatus.Submitted,
        };
        var service = CreateService(
            [new GuardianStudentSummary(studentId, true, true, true, CanViewBehaviourRecords: true)],
            behaviourIncidents: [draftIncident], behaviourCategories: [(categoryId, new BehaviourCategoryInfo("Disruption", false))]);

        var result = await service.GetStudentBehaviourSummaryAsync(Guid.NewGuid(), studentId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStudentBehaviourSummaryAsync_maps_Approved_incidents_to_a_minimal_non_sensitive_summary()
    {
        var studentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var approvedIncident = new BehaviourIncident
        {
            Id = Guid.NewGuid(), StudentPersonId = studentId, BehaviourCategoryId = categoryId, AcademicYearId = Guid.NewGuid(),
            Description = "Sensitive staff-only detail", ActionTaken = "Also sensitive", ConfidentialityTierCode = "RESTRICTED",
            RecordedByPersonId = Guid.NewGuid(), OccurredDate = Today, Status = WorkflowStatus.Approved,
        };
        var service = CreateService(
            [new GuardianStudentSummary(studentId, true, true, true, CanViewBehaviourRecords: true)],
            behaviourIncidents: [approvedIncident], behaviourCategories: [(categoryId, new BehaviourCategoryInfo("Merit", true))]);

        var result = await service.GetStudentBehaviourSummaryAsync(Guid.NewGuid(), studentId);

        var summary = Assert.Single(result);
        Assert.Equal("Merit", summary.CategoryName);
        Assert.True(summary.IsPositive);
        Assert.Equal(Today, summary.OccurredDate);
    }

    [Fact]
    public async Task AcknowledgeAsync_throws_when_the_guardian_has_no_relationship_with_that_student()
    {
        var service = CreateService([new GuardianStudentSummary(Guid.NewGuid(), true, true, true)]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AcknowledgeAsync(Guid.NewGuid(), Guid.NewGuid(), "ReportCard", "abc"));
    }

    [Fact]
    public async Task AcknowledgeAsync_is_idempotent_for_the_same_entity()
    {
        var studentId = Guid.NewGuid();
        var acknowledgements = new FakeGuardianAcknowledgementService();
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)], acknowledgements: acknowledgements);
        var guardianId = Guid.NewGuid();

        var firstId = await service.AcknowledgeAsync(guardianId, studentId, "ReportCard", "rc-1");
        var secondId = await service.AcknowledgeAsync(guardianId, studentId, "ReportCard", "rc-1");

        Assert.Equal(firstId, secondId);
        Assert.Single(acknowledgements.Acknowledgements);
    }

    [Fact]
    public async Task GetAcknowledgementAsync_returns_null_when_nothing_was_acknowledged_yet()
    {
        var studentId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)]);

        var result = await service.GetAcknowledgementAsync(Guid.NewGuid(), studentId, "ReportCard", "rc-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAcknowledgementAsync_returns_the_recorded_acknowledgement()
    {
        var studentId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();
        var service = CreateService([new GuardianStudentSummary(studentId, true, true, true)]);
        await service.AcknowledgeAsync(guardianId, studentId, "ReportCard", "rc-1");

        var result = await service.GetAcknowledgementAsync(guardianId, studentId, "ReportCard", "rc-1");

        Assert.NotNull(result);
        Assert.Equal("ReportCard", result!.EntityType);
        Assert.Equal("rc-1", result.EntityId);
    }
}

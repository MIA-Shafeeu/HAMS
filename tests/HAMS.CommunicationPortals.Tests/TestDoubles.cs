using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.Attendance.Application;
using HAMS.CommunicationPortals.Application;
using HAMS.CommunicationPortals.Domain;
using HAMS.Intervention.Application;
using HAMS.Intervention.Domain;
using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Common.Contracts;
using HAMS.ReportingAnalyticsAudit.Application;
using HAMS.ReportingAnalyticsAudit.Domain;

namespace HAMS.CommunicationPortals.Tests;

internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => today.ToDateTime(TimeOnly.MinValue);
    public DateOnly TodayUtc => today;
}

internal sealed class FakeGuardianRelationshipService(params GuardianStudentSummary[] students) : IGuardianRelationshipService
{
    public Task<Guid> EstablishAsync(EstablishGuardianRelationshipRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<Guid> ReviseAsync(Guid currentRelationshipId, ReviseGuardianRelationshipRequest request, DateOnly effectiveFrom, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task CloseAsync(Guid relationshipId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task VerifyAsync(Guid relationshipId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task RejectAsync(Guid relationshipId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<Guid?> FindVerifiedGuardianPersonIdByPhoneAsync(string phoneNumber, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<GuardianStudentSummary>> GetStudentsForGuardianAsync(Guid guardianPersonId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GuardianStudentSummary>>(students);
}

internal sealed class FakeKeyStageEvaluationService(params KeyStageEvaluation[] evaluations) : IKeyStageEvaluationService
{
    public Task<Guid> EvaluateAsync(Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<KeyStageEvaluation?> GetCurrentAsync(Guid studentPersonId, Guid subjectId, Guid evaluationPeriodId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<KeyStageEvaluation>> GetAllCurrentForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeyStageEvaluation>>(evaluations);
}

internal sealed class FakeAttendanceQueryService(params AttendanceRecordSummary[] records) : IAttendanceQueryService
{
    public Task<IReadOnlyList<AttendanceRecordSummary>> GetDailyRecordsAsync(Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AttendanceRecordSummary>>(records);

    public Task<IReadOnlyList<AttendanceStatusOption>> GetStatusesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<(Guid StudentPersonId, string AttendanceStatusCode)>> GetDailyRecordsForStudentsAsync(
        IReadOnlyList<Guid> studentPersonIds, DateOnly date, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");
}

internal sealed class FakeInterventionCaseService(params InterventionCase[] cases) : IInterventionCaseService
{
    public Task<Guid> OpenCaseAsync(
        Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid interventionTypeId, string confidentialityTierCode,
        Guid? learningOutcomeId, Guid? triggeringKeyStageEvaluationId, Guid? carriedForwardGapId, Guid openedByPersonId, DateOnly openedDate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<Guid> CreatePlanAsync(Guid interventionCaseId, string description, Guid assignedStaffPersonId, DateOnly startDate, DateOnly targetDate, string? notes, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<Guid> RecordReassessmentAttemptAsync(Guid interventionCaseId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, Guid recordedByPersonId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task CloseCaseAsync(Guid interventionCaseId, DateOnly closedDate, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<InterventionCase?> GetAsync(Guid interventionCaseId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<InterventionCase>> GetCasesForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InterventionCase>>(cases);

    public Task<IReadOnlyList<InterventionPlan>> GetPlansAsync(Guid interventionCaseId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<ReassessmentAttempt>> GetReassessmentAttemptsAsync(Guid interventionCaseId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<InterventionTypeOption>> GetActiveInterventionTypesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");
}

internal sealed class FakeReportCardService(params ReportCard[] reportCards) : IReportCardService
{
    public Task<Guid> PrepareAsync(PrepareReportCardRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task SubmitAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task BeginReviewAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task ApproveAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task RejectAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task ReturnAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<Guid> ReviseApprovedReportCardAsync(Guid reportCardId, ReviseReportCardRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<ReportCard?> GetAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => Task.FromResult(reportCards.SingleOrDefault(r => r.Id == reportCardId));

    public Task<IReadOnlyList<ReportCard>> GetPublishedForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReportCard>>(
            reportCards.Where(r => r.StudentPersonId == studentPersonId && r.Status == RecordStatus.Published).ToList());

    public Task<IReadOnlyList<ReportCardSubjectResult>> GetSubjectResultsAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<ReportCardKeyCompetencySummary>> GetKeyCompetencySummariesAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<byte[]> RenderPdfAsync(Guid reportCardId, CancellationToken cancellationToken = default)
        => Task.FromResult(reportCards.Any(r => r.Id == reportCardId)
            ? "%PDF-fake"u8.ToArray()
            : throw new InvalidOperationException("Report card not found."));

    public Task<IReadOnlyList<HAMS.PeopleEnrollment.Application.ClassRosterEntry>> GetStudentsNeedingReportCardAsync(
        Guid gradeId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");
}

internal sealed class FakeStudentEnrollmentService(params StudentEnrollment[] enrollments) : IStudentEnrollmentService
{
    public Task<Guid> EnrollAsync(Guid studentPersonId, Guid gradeId, Guid classId, Guid academicYearId, DateOnly effectiveFrom, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<StudentEnrollment?> GetActiveEnrollmentAsync(Guid studentPersonId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(enrollments.SingleOrDefault(e => e.StudentPersonId == studentPersonId && e.AcademicYearId == academicYearId));

    public Task EndEnrollmentAsync(Guid enrollmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForClassAsync(Guid classId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForGradeAsync(Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");
}

internal sealed class FakeHomeworkService(params Homework[] homeworks) : IHomeworkService
{
    public Task<Guid> CreateAsync(
        Guid classId, Guid subjectId, Guid? teachingTopicId, string titleEn, string titleDv,
        string instructionsEn, string instructionsDv, DateOnly assignedDate, DateOnly dueDate,
        int? maxScore, Guid assignedByPersonId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<Homework?> GetAsync(Guid homeworkId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<Homework>> ListForClassAsync(Guid classId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Homework>>(homeworks.Where(h => h.ClassId == classId).ToList());
}

internal sealed class FakeBehaviourIncidentService(params BehaviourIncident[] incidents) : IBehaviourIncidentService
{
    public Task<Guid> RecordAsync(
        Guid studentPersonId, Guid behaviourCategoryId, Guid? subjectId, Guid academicYearId, string description,
        string confidentialityTierCode, Guid recordedByPersonId, DateOnly occurredDate, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task SubmitAsync(Guid incidentId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task BeginReviewAsync(Guid incidentId, Guid reviewedByPersonId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task ApproveAsync(Guid incidentId, Guid reviewedByPersonId, string? actionTaken, string? reviewNotes, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task RejectAsync(Guid incidentId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task ReturnAsync(Guid incidentId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<BehaviourIncident?> GetAsync(Guid incidentId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");

    public Task<IReadOnlyList<BehaviourIncident>> GetForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BehaviourIncident>>(incidents.Where(i => i.StudentPersonId == studentPersonId).ToList());
}

internal sealed class FakeBehaviourCategoryLookup(params (Guid Id, BehaviourCategoryInfo Info)[] categories) : IBehaviourCategoryLookup
{
    public Task<BehaviourCategoryInfo?> GetAsync(Guid behaviourCategoryId, CancellationToken cancellationToken = default)
        => Task.FromResult(categories.Where(c => c.Id == behaviourCategoryId).Select(c => c.Info).SingleOrDefault());

    public Task<IReadOnlyList<BehaviourCategoryOption>> GetAllAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by portal tests.");
}

internal sealed class FakeGuardianAcknowledgementService : IGuardianAcknowledgementService
{
    public readonly List<GuardianAcknowledgement> Acknowledgements = [];

    public Task<Guid> AcknowledgeAsync(Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        var existing = Acknowledgements.SingleOrDefault(a =>
            a.GuardianPersonId == guardianPersonId && a.StudentPersonId == studentPersonId && a.EntityType == entityType && a.EntityId == entityId);
        if (existing is not null)
        {
            return Task.FromResult(existing.Id);
        }

        var acknowledgement = new GuardianAcknowledgement
        {
            Id = Guid.NewGuid(), GuardianPersonId = guardianPersonId, StudentPersonId = studentPersonId,
            EntityType = entityType, EntityId = entityId, AcknowledgedAtUtc = DateTimeOffset.UtcNow,
        };
        Acknowledgements.Add(acknowledgement);
        return Task.FromResult(acknowledgement.Id);
    }

    public Task<GuardianAcknowledgement?> GetAsync(Guid guardianPersonId, Guid studentPersonId, string entityType, string entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(Acknowledgements.SingleOrDefault(a =>
            a.GuardianPersonId == guardianPersonId && a.StudentPersonId == studentPersonId && a.EntityType == entityType && a.EntityId == entityId));
}

using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.LearningDelivery.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Common.Contracts;

namespace HAMS.ReportingAnalyticsAudit.Tests;

internal sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow => utcNow;
    public DateOnly TodayUtc => DateOnly.FromDateTime(utcNow.UtcDateTime);
}

internal sealed class FakeKeyStageEvaluationService(params KeyStageEvaluation[] evaluations) : IKeyStageEvaluationService
{
    public Task<Guid> EvaluateAsync(Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by report card tests.");

    public Task<KeyStageEvaluation?> GetCurrentAsync(Guid studentPersonId, Guid subjectId, Guid evaluationPeriodId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by report card tests.");

    public Task<IReadOnlyList<KeyStageEvaluation>> GetAllCurrentForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeyStageEvaluation>>(evaluations.Where(e => e.StudentPersonId == studentPersonId).ToList());
}

internal sealed class FakeKeyCompetencyEvidenceService(params KeyCompetencySummary[] summaries) : IKeyCompetencyEvidenceService
{
    public Task<Guid> RecordAsync(
        Guid studentPersonId, Guid keyCompetencyIndicatorId, string evidenceTypeCode, int? ratingScore,
        DateOnly recordedDate, Guid recordedByPersonId, string? notes, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by report card tests.");

    public Task<IReadOnlyList<KeyCompetencySummary>> GetSummaryForStudentAsync(
        Guid studentPersonId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeyCompetencySummary>>(summaries);
}

internal sealed class FakeEvaluationPeriodLookup(IReadOnlyDictionary<Guid, EvaluationPeriodWindow> windowsByPeriodId) : IEvaluationPeriodLookup
{
    public Task<EvaluationPeriodWindow?> GetWindowAsync(Guid evaluationPeriodId, CancellationToken cancellationToken = default)
        => Task.FromResult(windowsByPeriodId.GetValueOrDefault(evaluationPeriodId));
}

internal sealed class FakeSubjectLookup(IReadOnlyDictionary<Guid, string> namesBySubjectId) : ISubjectLookup
{
    public Task<string?> GetNameAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => Task.FromResult(namesBySubjectId.GetValueOrDefault(subjectId));
}

internal sealed class FakeKeyCompetencyLookup(params KeyCompetencyName[] names) : IKeyCompetencyLookup
{
    public Task<IReadOnlyList<KeyCompetencyName>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeyCompetencyName>>(names);
}

internal sealed class FakeStudentEnrollmentService(params StudentEnrollment[] enrollments) : IStudentEnrollmentService
{
    public Task<Guid> EnrollAsync(Guid studentPersonId, Guid gradeId, Guid classId, Guid academicYearId, DateOnly effectiveFrom, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by report card tests.");

    public Task<StudentEnrollment?> GetActiveEnrollmentAsync(Guid studentPersonId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by report card tests.");

    public Task EndEnrollmentAsync(Guid enrollmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by report card tests.");

    public Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForClassAsync(Guid classId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by report card tests.");

    public Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForGradeAsync(Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ClassRosterEntry>>(enrollments
            .Where(e => e.GradeId == gradeId && e.AcademicYearId == academicYearId && e.EffectiveFrom <= asOf && (e.EffectiveTo == null || e.EffectiveTo >= asOf))
            .Select(e => new ClassRosterEntry(e.StudentPersonId, $"Student {e.StudentPersonId}", $"Student {e.StudentPersonId}", e.StudentPersonId.ToString()))
            .ToList());
}

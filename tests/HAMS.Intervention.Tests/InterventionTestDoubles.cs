using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.LearningDelivery.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Application;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Tests;

/// <summary>Defaults to no notifiable guardians — pass specific contacts to test the case-opened notification path.</summary>
internal sealed class FakeGuardianContactResolver(params GuardianContact[] contacts) : IGuardianContactResolver
{
    public Task<IReadOnlyList<GuardianContact>> ResolveNotifiableGuardianContactsAsync(
        Guid studentPersonId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GuardianContact>>(contacts);
}

internal sealed class FakeNotificationOutboxWriter : INotificationOutboxWriter
{
    public List<OutboundNotification> Enqueued { get; } = [];

    public async Task EnqueueManyAsync(
        DbContext sourceContext, Action stageSourceChanges, IReadOnlyList<OutboundNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        stageSourceChanges();
        Enqueued.AddRange(notifications);
        await sourceContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => today.ToDateTime(TimeOnly.MinValue);
    public DateOnly TodayUtc => today;
}

/// <summary>Records the last call's arguments so tests can assert the case's own student/subject were forwarded, and always returns <paramref name="evaluationIdToReturn"/>.</summary>
internal sealed class FakeKeyStageEvaluationService(Guid evaluationIdToReturn) : IKeyStageEvaluationService
{
    public (Guid StudentPersonId, Guid SubjectId, Guid AcademicYearId, Guid EvaluationPeriodId, DateOnly AsOf)? LastCall { get; private set; }

    public Task<Guid> EvaluateAsync(
        Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        LastCall = (studentPersonId, subjectId, academicYearId, evaluationPeriodId, asOf);
        return Task.FromResult(evaluationIdToReturn);
    }

    public Task<KeyStageEvaluation?> GetCurrentAsync(Guid studentPersonId, Guid subjectId, Guid evaluationPeriodId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by intervention-case tests.");

    public Task<IReadOnlyList<KeyStageEvaluation>> GetAllCurrentForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by intervention-case tests.");
}

internal sealed class FakeTeachingTopicQuery(Guid? learningOutcomeId) : ITeachingTopicQuery
{
    public Task<Guid?> GetLearningOutcomeIdAsync(Guid teachingTopicId, CancellationToken cancellationToken = default)
        => Task.FromResult(learningOutcomeId);
}

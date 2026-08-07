using HAMS.Intervention.Application;
using HAMS.Intervention.Domain;
using HAMS.Intervention.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Tests;

public class InterventionCaseServiceTests
{
    private static InterventionDbContext CreateContext() => new(
        new DbContextOptionsBuilder<InterventionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static InterventionCaseService CreateService(
        InterventionDbContext db, FakeKeyStageEvaluationService? evaluationService = null, DateOnly? today = null,
        FakeGuardianContactResolver? guardianContacts = null, FakeNotificationOutboxWriter? outboxWriter = null)
        => new(
            db, evaluationService ?? new FakeKeyStageEvaluationService(Guid.NewGuid()), guardianContacts ?? new FakeGuardianContactResolver(),
            outboxWriter ?? new FakeNotificationOutboxWriter(), new FakeClock(today ?? new DateOnly(2026, 8, 5)));

    [Fact]
    public async Task OpenCaseAsync_creates_an_Open_case()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var caseId = await service.OpenCaseAsync(
            studentId, subjectId, Guid.NewGuid(), Guid.NewGuid(), ConfidentialityTierCodesForTests.Restricted,
            learningOutcomeId: null, triggeringKeyStageEvaluationId: null, carriedForwardGapId: null,
            Guid.NewGuid(), new DateOnly(2026, 8, 1));

        var interventionCase = await db.InterventionCases.SingleAsync(c => c.Id == caseId);
        Assert.Equal(studentId, interventionCase.StudentPersonId);
        Assert.Equal(subjectId, interventionCase.SubjectId);
        Assert.Equal(InterventionCaseStatus.Open, interventionCase.Status);
        Assert.Null(interventionCase.ClosedDate);
    }

    [Fact]
    public async Task CreatePlanAsync_links_the_plan_to_its_case()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var caseId = await OpenTestCaseAsync(service);

        var planId = await service.CreatePlanAsync(
            caseId, "Twice-weekly reading support", Guid.NewGuid(), new DateOnly(2026, 8, 4), new DateOnly(2026, 9, 4), "Focus on phonics");

        var plan = await db.InterventionPlans.SingleAsync(p => p.Id == planId);
        Assert.Equal(caseId, plan.InterventionCaseId);
        Assert.Equal("Twice-weekly reading support", plan.Description);
    }

    [Fact]
    public async Task CreatePlanAsync_throws_when_the_case_does_not_exist()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePlanAsync(Guid.NewGuid(), "desc", Guid.NewGuid(), new DateOnly(2026, 8, 4), new DateOnly(2026, 9, 4), null));
    }

    [Fact]
    public async Task RecordReassessmentAttemptAsync_reevaluates_the_cases_own_student_and_subject()
    {
        await using var db = CreateContext();
        var evaluationId = Guid.NewGuid();
        var evaluationService = new FakeKeyStageEvaluationService(evaluationId);
        var service = CreateService(db, evaluationService);
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var caseId = await service.OpenCaseAsync(
            studentId, subjectId, Guid.NewGuid(), Guid.NewGuid(), ConfidentialityTierCodesForTests.Restricted,
            null, null, null, Guid.NewGuid(), new DateOnly(2026, 8, 1));

        var academicYearId = Guid.NewGuid();
        var evaluationPeriodId = Guid.NewGuid();
        var attemptId = await service.RecordReassessmentAttemptAsync(
            caseId, academicYearId, evaluationPeriodId, new DateOnly(2026, 9, 1), Guid.NewGuid());

        var attempt = await db.ReassessmentAttempts.SingleAsync(a => a.Id == attemptId);
        Assert.Equal(evaluationId, attempt.KeyStageEvaluationId);
        Assert.Equal(caseId, attempt.InterventionCaseId);

        Assert.NotNull(evaluationService.LastCall);
        Assert.Equal(studentId, evaluationService.LastCall!.Value.StudentPersonId);
        Assert.Equal(subjectId, evaluationService.LastCall!.Value.SubjectId);
        Assert.Equal(academicYearId, evaluationService.LastCall!.Value.AcademicYearId);
        Assert.Equal(evaluationPeriodId, evaluationService.LastCall!.Value.EvaluationPeriodId);
    }

    [Fact]
    public async Task RecordReassessmentAttemptAsync_throws_once_the_case_is_closed()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var caseId = await OpenTestCaseAsync(service);
        await service.CloseCaseAsync(caseId, new DateOnly(2026, 9, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordReassessmentAttemptAsync(caseId, Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 1), Guid.NewGuid()));
    }

    [Fact]
    public async Task CloseCaseAsync_sets_Status_and_ClosedDate()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var caseId = await OpenTestCaseAsync(service);

        await service.CloseCaseAsync(caseId, new DateOnly(2026, 9, 15));

        var interventionCase = await db.InterventionCases.SingleAsync(c => c.Id == caseId);
        Assert.Equal(InterventionCaseStatus.Closed, interventionCase.Status);
        Assert.Equal(new DateOnly(2026, 9, 15), interventionCase.ClosedDate);
    }

    [Fact]
    public async Task GetPlansAsync_and_GetReassessmentAttemptsAsync_return_only_rows_for_that_case()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var caseId = await OpenTestCaseAsync(service);
        var otherCaseId = await OpenTestCaseAsync(service);

        await service.CreatePlanAsync(caseId, "Plan A", Guid.NewGuid(), new DateOnly(2026, 8, 4), new DateOnly(2026, 9, 4), null);
        await service.CreatePlanAsync(otherCaseId, "Plan B", Guid.NewGuid(), new DateOnly(2026, 8, 4), new DateOnly(2026, 9, 4), null);
        await service.RecordReassessmentAttemptAsync(caseId, Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 1), Guid.NewGuid());

        var plans = await service.GetPlansAsync(caseId);
        var attempts = await service.GetReassessmentAttemptsAsync(caseId);

        Assert.Single(plans);
        Assert.Equal("Plan A", plans[0].Description);
        Assert.Single(attempts);
    }

    private static async Task<Guid> OpenTestCaseAsync(IInterventionCaseService service)
        => await service.OpenCaseAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ConfidentialityTierCodesForTests.Restricted,
            null, null, null, Guid.NewGuid(), new DateOnly(2026, 8, 1));
}

internal static class ConfidentialityTierCodesForTests
{
    public const string Restricted = "RESTRICTED";
}

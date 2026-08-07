using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.LearningDelivery.Domain;
using HAMS.OrgCurriculum.Domain;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Tests.Evaluation;

internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => today.ToDateTime(TimeOnly.MinValue);
    public DateOnly TodayUtc => today;
}

public class KeyStageEvaluationServiceTests
{
    private static AssessmentEvaluationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AssessmentEvaluationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static StudentEnrollment CreateEnrollment(Guid studentId, Guid gradeId, Guid classId, Guid academicYearId) => new()
    {
        Id = Guid.NewGuid(), StudentPersonId = studentId, GradeId = gradeId, ClassId = classId, AcademicYearId = academicYearId,
        EnrollmentTypeId = Guid.NewGuid(), EffectiveFrom = new DateOnly(2026, 1, 1),
    };

    private static EvaluationModel CreateModel(string code) => new() { Id = Guid.NewGuid(), Code = code, Name = code };

    [Fact]
    public async Task EvaluateAsync_throws_when_the_student_has_no_active_enrollment()
    {
        await using var db = CreateContext();
        var service = new KeyStageEvaluationService(
            db, [], new FakeStudentEnrollmentService(), new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()),
            new FakeEvaluationModelLookup(), new FakeClock(new DateOnly(2026, 2, 1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluateAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public async Task EvaluateAsync_throws_when_the_students_grade_has_no_published_policy()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var enrollment = CreateEnrollment(studentId, gradeId, Guid.NewGuid(), academicYearId);

        var service = new KeyStageEvaluationService(
            db, [], new FakeStudentEnrollmentService(enrollment), new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()),
            new FakeEvaluationModelLookup(), new FakeClock(new DateOnly(2026, 2, 1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluateAsync(studentId, Guid.NewGuid(), academicYearId, Guid.NewGuid(), new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public async Task EvaluateAsync_throws_when_no_engine_matches_the_resolved_model_code()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var enrollment = CreateEnrollment(studentId, gradeId, Guid.NewGuid(), academicYearId);
        var model = CreateModel("NONEXISTENT_MODEL");
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = model.Id, Status = RecordStatus.Published };
        var period = new EvaluationPeriod { Id = Guid.NewGuid(), AcademicYearId = academicYearId, Code = "T1", Name = "Term 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 4, 30) };
        db.EvaluationPeriods.Add(period);
        await db.SaveChangesAsync();

        var service = new KeyStageEvaluationService(
            db, [], new FakeStudentEnrollmentService(enrollment),
            new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy }),
            new FakeEvaluationModelLookup(model), new FakeClock(new DateOnly(2026, 2, 1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluateAsync(studentId, Guid.NewGuid(), academicYearId, period.Id, new DateOnly(2026, 2, 1)));
    }

    private sealed class StubEvaluationEngine(string modelCode, EvaluationOutcome outcome) : IEvaluationEngine
    {
        public string ModelCode => modelCode;
        public Task<EvaluationOutcome> EvaluateAsync(EvaluationContext context, CancellationToken cancellationToken = default) => Task.FromResult(outcome);
    }

    [Fact]
    public async Task EvaluateAsync_dispatches_to_the_matching_engine_and_stamps_the_policy_and_model()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var enrollment = CreateEnrollment(studentId, gradeId, Guid.NewGuid(), academicYearId);
        var model = CreateModel(EvaluationModelCodes.Mastery);
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = model.Id, Status = RecordStatus.Published };
        var period = new EvaluationPeriod { Id = Guid.NewGuid(), AcademicYearId = academicYearId, Code = "T1", Name = "Term 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 4, 30) };
        db.EvaluationPeriods.Add(period);
        await db.SaveChangesAsync();

        var expectedLevelId = Guid.NewGuid();
        var engines = new List<IEvaluationEngine>
        {
            new StubEvaluationEngine(EvaluationModelCodes.Assessment, EvaluationOutcome.Empty),
            new StubEvaluationEngine(EvaluationModelCodes.Mastery, new EvaluationOutcome(expectedLevelId, null, null)),
        };

        var service = new KeyStageEvaluationService(
            db, engines, new FakeStudentEnrollmentService(enrollment),
            new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy }),
            new FakeEvaluationModelLookup(model), new FakeClock(new DateOnly(2026, 2, 1)));

        var evaluationId = await service.EvaluateAsync(studentId, subjectId, academicYearId, period.Id, new DateOnly(2026, 2, 1));

        var evaluation = await db.KeyStageEvaluations.SingleAsync(e => e.Id == evaluationId);
        Assert.Equal(expectedLevelId, evaluation.OverallAchievementLevelId);
        Assert.Equal(policy.Id, evaluation.KeyStagePolicyId);
        Assert.Equal(model.Id, evaluation.EvaluationModelId);
    }

    [Fact]
    public async Task GetCurrentAsync_returns_the_most_recently_recorded_evaluation()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        db.KeyStageEvaluations.AddRange(
            new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), RecordedAtUtc = new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero) },
            new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = Guid.NewGuid(), EvaluationModelId = Guid.NewGuid(), RecordedAtUtc = new DateTimeOffset(2026, 1, 11, 0, 0, 0, TimeSpan.Zero) });
        await db.SaveChangesAsync();
        var latest = await db.KeyStageEvaluations.OrderByDescending(e => e.RecordedAtUtc).FirstAsync();

        var service = new KeyStageEvaluationService(
            db, [], new FakeStudentEnrollmentService(), new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()),
            new FakeEvaluationModelLookup(), new FakeClock(new DateOnly(2026, 2, 1)));

        var current = await service.GetCurrentAsync(studentId, subjectId, periodId);

        Assert.NotNull(current);
        Assert.Equal(latest.Id, current!.Id);
    }

    /// <summary>
    /// The build plan's explicit, mandatory acceptance test (§12): "write a dedicated acceptance
    /// test mirroring a combined-class scenario before Phase 8 is considered done, not just the
    /// single-grade case." Two students share the SAME class (a real small-island combined-grade
    /// class), but belong to two DIFFERENT grades — each grade has its own KeyStagePolicy with a
    /// DIFFERENT evaluation model, so if the dispatcher ever resolved a grade from the shared Class
    /// instead of each student's own StudentEnrollment.GradeId, one student would silently get the
    /// other grade's model. This proves it never does.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_resolves_each_students_own_grade_policy_in_a_combined_grade_class_never_the_others()
    {
        await using var db = CreateContext();
        var sharedClassId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var asOf = new DateOnly(2026, 2, 1);

        var studentA = Guid.NewGuid();
        var gradeA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        var gradeB = Guid.NewGuid();

        // Both students enrolled in the same physical Class, but different Grades.
        var enrollmentService = new FakeStudentEnrollmentService(
            CreateEnrollment(studentA, gradeA, sharedClassId, academicYearId),
            CreateEnrollment(studentB, gradeB, sharedClassId, academicYearId));

        var masteryModel = CreateModel(EvaluationModelCodes.Mastery);
        var assessmentModel = CreateModel(EvaluationModelCodes.Assessment);

        var policyA = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = masteryModel.Id, Status = RecordStatus.Published };
        var policyB = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = assessmentModel.Id, Status = RecordStatus.Published };

        var policyResolver = new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeA] = policyA, [gradeB] = policyB });
        var modelLookup = new FakeEvaluationModelLookup(masteryModel, assessmentModel);

        var period = new EvaluationPeriod { Id = Guid.NewGuid(), AcademicYearId = academicYearId, Code = "T1", Name = "Term 1", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 4, 30) };
        db.EvaluationPeriods.Add(period);
        await db.SaveChangesAsync();

        var masteryOnlyLevelId = Guid.NewGuid();
        var engines = new List<IEvaluationEngine>
        {
            new StubEvaluationEngine(EvaluationModelCodes.Mastery, new EvaluationOutcome(masteryOnlyLevelId, null, null)),
            new StubEvaluationEngine(EvaluationModelCodes.Assessment, new EvaluationOutcome(null, 77m, null)),
        };

        var service = new KeyStageEvaluationService(db, engines, enrollmentService, policyResolver, modelLookup, new FakeClock(asOf));

        var evaluationIdA = await service.EvaluateAsync(studentA, subjectId, academicYearId, period.Id, asOf);
        var evaluationIdB = await service.EvaluateAsync(studentB, subjectId, academicYearId, period.Id, asOf);

        var evaluationA = await db.KeyStageEvaluations.SingleAsync(e => e.Id == evaluationIdA);
        var evaluationB = await db.KeyStageEvaluations.SingleAsync(e => e.Id == evaluationIdB);

        // Student A (gradeA -> Mastery policy): got a mastery level, no percentage.
        Assert.Equal(policyA.Id, evaluationA.KeyStagePolicyId);
        Assert.Equal(masteryModel.Id, evaluationA.EvaluationModelId);
        Assert.Equal(masteryOnlyLevelId, evaluationA.OverallAchievementLevelId);
        Assert.Null(evaluationA.OverallPercentage);

        // Student B (gradeB -> Assessment policy): got a percentage, no mastery level — proving
        // it used gradeB's own policy, never gradeA's, despite sharing the same Class.
        Assert.Equal(policyB.Id, evaluationB.KeyStagePolicyId);
        Assert.Equal(assessmentModel.Id, evaluationB.EvaluationModelId);
        Assert.Null(evaluationB.OverallAchievementLevelId);
        Assert.Equal(77m, evaluationB.OverallPercentage);
    }
}

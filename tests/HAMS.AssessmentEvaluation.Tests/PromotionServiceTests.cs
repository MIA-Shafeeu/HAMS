using HAMS.AssessmentEvaluation.Application;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.AssessmentEvaluation.Tests.Evaluation;
using HAMS.OrgCurriculum.Domain;
using HAMS.PeopleEnrollment.Domain;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Tests;

public class PromotionServiceTests
{
    private static readonly DateOnly AsOf = new(2026, 8, 5);

    private static AssessmentEvaluationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AssessmentEvaluationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static StudentEnrollment CreateEnrollment(Guid studentId, Guid gradeId, Guid academicYearId) => new()
    {
        Id = Guid.NewGuid(), StudentPersonId = studentId, GradeId = gradeId, ClassId = Guid.NewGuid(), AcademicYearId = academicYearId,
        EnrollmentTypeId = Guid.NewGuid(), EffectiveFrom = new DateOnly(2026, 1, 1),
    };

    private static PromotionPolicy CreatePromotionPolicy(int minimumRank, int minimumSubjects) => new()
    {
        Id = Guid.NewGuid(), Code = "STANDARD", Name = "Standard", MinimumRank = minimumRank, MinimumSubjectsRequiredToClear = minimumSubjects,
    };

    private static PromotionService CreateService(
        AssessmentEvaluationDbContext db, KeyStageEvaluation[] evaluations, FakeStudentEnrollmentService enrollmentService,
        FakeKeyStagePolicyResolver policyResolver, FakeAchievementScaleQuery? achievementScaleQuery = null)
        => new(
            db, new FakeKeyStageEvaluationService(evaluations), policyResolver,
            achievementScaleQuery ?? new FakeAchievementScaleQuery(new Dictionary<Guid, int>()), enrollmentService,
            new FakeClock(AsOf));

    [Fact]
    public async Task EvaluateEligibilityAsync_throws_when_the_student_has_no_active_enrollment()
    {
        await using var db = CreateContext();
        var service = CreateService(
            db, [], new FakeStudentEnrollmentService(), new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluateEligibilityAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AsOf));
    }

    [Fact]
    public async Task EvaluateEligibilityAsync_throws_when_no_published_policy_exists_for_the_grade()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));
        var service = CreateService(db, [], enrollmentService, new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluateEligibilityAsync(studentId, academicYearId, Guid.NewGuid(), AsOf));
    }

    [Fact]
    public async Task EvaluateEligibilityAsync_throws_when_the_policy_has_no_promotion_policy_configured()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = Guid.NewGuid(), Status = RecordStatus.Published, PromotionPolicyId = null };
        var policyResolver = new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy });
        var service = CreateService(db, [], enrollmentService, policyResolver);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluateEligibilityAsync(studentId, academicYearId, Guid.NewGuid(), AsOf));
    }

    [Fact]
    public async Task EvaluateEligibilityAsync_throws_when_the_configured_promotion_policy_row_was_not_found()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = Guid.NewGuid(), Status = RecordStatus.Published, PromotionPolicyId = Guid.NewGuid() };
        var policyResolver = new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy });
        var service = CreateService(db, [], enrollmentService, policyResolver);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EvaluateEligibilityAsync(studentId, academicYearId, Guid.NewGuid(), AsOf));
    }

    [Fact]
    public async Task EvaluateEligibilityAsync_clears_the_bar_using_GradeBand_rank_for_an_Assessment_model_evaluation()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));

        var promotionPolicy = CreatePromotionPolicy(minimumRank: 2, minimumSubjects: 1);
        db.PromotionPolicies.Add(promotionPolicy);
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = Guid.NewGuid(), Status = RecordStatus.Published, PromotionPolicyId = promotionPolicy.Id };
        var gradeBand = new GradeBand { Id = Guid.NewGuid(), GradeScaleId = Guid.NewGuid(), Code = "B", Name = "B", Rank = 3 };
        db.GradeBands.Add(gradeBand);
        await db.SaveChangesAsync();

        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = policy.Id, EvaluationModelId = policy.EvaluationModelId, OverallGradeBandId = gradeBand.Id };
        var policyResolver = new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy });
        var service = CreateService(db, [evaluation], enrollmentService, policyResolver);

        var result = await service.EvaluateEligibilityAsync(studentId, academicYearId, periodId, AsOf);

        Assert.True(result.MeetsThreshold);
        Assert.Equal(1, result.SubjectsCleared);
        Assert.Equal(1, result.TotalSubjectsEvaluated);
        Assert.Empty(result.SubjectIdsNotCleared);
    }

    [Fact]
    public async Task EvaluateEligibilityAsync_clears_the_bar_using_AchievementLevel_rank_via_the_evaluations_own_stamped_policy_for_a_Mastery_model_evaluation()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));

        var promotionPolicy = CreatePromotionPolicy(minimumRank: 2, minimumSubjects: 1);
        db.PromotionPolicies.Add(promotionPolicy);
        var achievementScaleId = Guid.NewGuid();
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = Guid.NewGuid(), Status = RecordStatus.Published, PromotionPolicyId = promotionPolicy.Id, AchievementScaleId = achievementScaleId };
        await db.SaveChangesAsync();

        var levelId = Guid.NewGuid();
        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = policy.Id, EvaluationModelId = policy.EvaluationModelId, OverallAchievementLevelId = levelId };
        // The evaluation's OWN stamped KeyStagePolicyId is what's resolved (GetByIdAsync), never a fresh grade/date resolution.
        var policyResolver = new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy });
        var achievementScaleQuery = new FakeAchievementScaleQuery(new Dictionary<Guid, int> { [levelId] = 2 });
        var service = CreateService(db, [evaluation], enrollmentService, policyResolver, achievementScaleQuery);

        var result = await service.EvaluateEligibilityAsync(studentId, academicYearId, periodId, AsOf);

        Assert.True(result.MeetsThreshold);
        Assert.Equal(1, result.SubjectsCleared);
    }

    [Fact]
    public async Task EvaluateEligibilityAsync_reports_which_subjects_did_not_clear_and_computes_MeetsThreshold_from_the_count()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var clearedSubjectId = Guid.NewGuid();
        var notClearedSubjectId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));

        // Requires 2 subjects cleared, but only 1 of 2 evaluated subjects will clear the bar.
        var promotionPolicy = CreatePromotionPolicy(minimumRank: 2, minimumSubjects: 2);
        db.PromotionPolicies.Add(promotionPolicy);
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = Guid.NewGuid(), Status = RecordStatus.Published, PromotionPolicyId = promotionPolicy.Id };
        var clearingBand = new GradeBand { Id = Guid.NewGuid(), GradeScaleId = Guid.NewGuid(), Code = "A", Name = "A", Rank = 3 };
        var failingBand = new GradeBand { Id = Guid.NewGuid(), GradeScaleId = Guid.NewGuid(), Code = "D", Name = "D", Rank = 1 };
        db.GradeBands.AddRange(clearingBand, failingBand);
        await db.SaveChangesAsync();

        var evaluations = new[]
        {
            new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = clearedSubjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = policy.Id, EvaluationModelId = policy.EvaluationModelId, OverallGradeBandId = clearingBand.Id },
            new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = notClearedSubjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = policy.Id, EvaluationModelId = policy.EvaluationModelId, OverallGradeBandId = failingBand.Id },
        };
        var policyResolver = new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy });
        var service = CreateService(db, evaluations, enrollmentService, policyResolver);

        var result = await service.EvaluateEligibilityAsync(studentId, academicYearId, periodId, AsOf);

        Assert.False(result.MeetsThreshold);
        Assert.Equal(1, result.SubjectsCleared);
        Assert.Equal(2, result.TotalSubjectsEvaluated);
        Assert.Single(result.SubjectIdsNotCleared, notClearedSubjectId);
    }

    [Fact]
    public async Task EvaluateEligibilityAsync_treats_an_evaluation_with_neither_facet_populated_as_not_cleared()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));

        var promotionPolicy = CreatePromotionPolicy(minimumRank: 1, minimumSubjects: 1);
        db.PromotionPolicies.Add(promotionPolicy);
        var policy = new KeyStagePolicy { Id = Guid.NewGuid(), KeyStageId = Guid.NewGuid(), AcademicYearId = academicYearId, EvaluationModelId = Guid.NewGuid(), Status = RecordStatus.Published, PromotionPolicyId = promotionPolicy.Id };
        await db.SaveChangesAsync();

        var evaluation = new KeyStageEvaluation { Id = Guid.NewGuid(), StudentPersonId = studentId, SubjectId = subjectId, EvaluationPeriodId = periodId, KeyStagePolicyId = policy.Id, EvaluationModelId = policy.EvaluationModelId };
        var policyResolver = new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy> { [gradeId] = policy });
        var service = CreateService(db, [evaluation], enrollmentService, policyResolver);

        var result = await service.EvaluateEligibilityAsync(studentId, academicYearId, periodId, AsOf);

        Assert.False(result.MeetsThreshold);
        Assert.Single(result.SubjectIdsNotCleared, subjectId);
    }

    [Fact]
    public async Task RecordDecisionAsync_creates_a_decision_row_and_closes_the_students_current_enrolment()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var nextGradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var enrollment = CreateEnrollment(studentId, gradeId, academicYearId);
        var enrollmentService = new FakeStudentEnrollmentService(enrollment);
        var service = CreateService(db, [], enrollmentService, new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()));
        var decidedBy = Guid.NewGuid();
        var decisionDate = new DateOnly(2026, 8, 10);

        var decisionId = await service.RecordDecisionAsync(studentId, academicYearId, promoted: true, nextGradeId, decidedBy, decisionDate, "Cleared all subjects.");

        var decision = await db.PromotionDecisions.SingleAsync(d => d.Id == decisionId);
        Assert.Equal(studentId, decision.StudentPersonId);
        Assert.Equal(gradeId, decision.CurrentGradeId);
        Assert.True(decision.Promoted);
        Assert.Equal(nextGradeId, decision.NextGradeId);
        Assert.Equal(decidedBy, decision.DecidedByPersonId);
        Assert.Equal(decisionDate, decision.DecisionDate);
        Assert.Equal("Cleared all subjects.", decision.Notes);

        Assert.Contains(enrollment.Id, enrollmentService.EndedEnrollmentIds);
        Assert.Equal(decisionDate, enrollment.EffectiveTo);
    }

    [Fact]
    public async Task RecordDecisionAsync_throws_when_the_student_has_no_active_enrollment_as_of_the_decision_date()
    {
        await using var db = CreateContext();
        var service = CreateService(
            db, [], new FakeStudentEnrollmentService(), new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordDecisionAsync(Guid.NewGuid(), Guid.NewGuid(), true, null, Guid.NewGuid(), AsOf, null));
    }

    [Fact]
    public async Task GetDecisionsForStudentAsync_returns_only_that_students_decisions_ordered_most_recent_first()
    {
        await using var db = CreateContext();
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var older = new PromotionDecision { Id = Guid.NewGuid(), StudentPersonId = studentId, AcademicYearId = Guid.NewGuid(), CurrentGradeId = Guid.NewGuid(), Promoted = true, DecidedByPersonId = Guid.NewGuid(), DecisionDate = new DateOnly(2025, 8, 1), RecordedAtUtc = new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero) };
        var newer = new PromotionDecision { Id = Guid.NewGuid(), StudentPersonId = studentId, AcademicYearId = Guid.NewGuid(), CurrentGradeId = Guid.NewGuid(), Promoted = false, DecidedByPersonId = Guid.NewGuid(), DecisionDate = new DateOnly(2026, 8, 1), RecordedAtUtc = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero) };
        var forOtherStudent = new PromotionDecision { Id = Guid.NewGuid(), StudentPersonId = otherStudentId, AcademicYearId = Guid.NewGuid(), CurrentGradeId = Guid.NewGuid(), Promoted = true, DecidedByPersonId = Guid.NewGuid(), DecisionDate = new DateOnly(2026, 8, 1), RecordedAtUtc = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero) };
        db.PromotionDecisions.AddRange(older, newer, forOtherStudent);
        await db.SaveChangesAsync();
        var service = CreateService(
            db, [], new FakeStudentEnrollmentService(), new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()));

        var result = await service.GetDecisionsForStudentAsync(studentId);

        Assert.Equal([newer.Id, older.Id], result.Select(d => d.Id));
    }

    [Fact]
    public async Task GetStudentsNeedingDecisionAsync_excludes_students_who_already_have_a_decision_this_year()
    {
        await using var db = CreateContext();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var decidedStudentId = Guid.NewGuid();
        var undecidedStudentId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(
            CreateEnrollment(decidedStudentId, gradeId, academicYearId),
            CreateEnrollment(undecidedStudentId, gradeId, academicYearId));
        db.PromotionDecisions.Add(new PromotionDecision
        {
            Id = Guid.NewGuid(), StudentPersonId = decidedStudentId, AcademicYearId = academicYearId, CurrentGradeId = gradeId,
            Promoted = true, DecidedByPersonId = Guid.NewGuid(), DecisionDate = AsOf, RecordedAtUtc = new DateTimeOffset(AsOf, TimeOnly.MinValue, TimeSpan.Zero),
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, [], enrollmentService, new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()));

        var worklist = await service.GetStudentsNeedingDecisionAsync(gradeId, academicYearId, AsOf);

        Assert.Single(worklist, s => s.StudentPersonId == undecidedStudentId);
    }

    [Fact]
    public async Task GetStudentsNeedingDecisionAsync_ignores_decisions_from_a_different_academic_year()
    {
        await using var db = CreateContext();
        var gradeId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var enrollmentService = new FakeStudentEnrollmentService(CreateEnrollment(studentId, gradeId, academicYearId));
        db.PromotionDecisions.Add(new PromotionDecision
        {
            Id = Guid.NewGuid(), StudentPersonId = studentId, AcademicYearId = Guid.NewGuid(), CurrentGradeId = gradeId,
            Promoted = true, DecidedByPersonId = Guid.NewGuid(), DecisionDate = AsOf, RecordedAtUtc = new DateTimeOffset(AsOf, TimeOnly.MinValue, TimeSpan.Zero),
        });
        await db.SaveChangesAsync();
        var service = CreateService(db, [], enrollmentService, new FakeKeyStagePolicyResolver(new Dictionary<Guid, KeyStagePolicy>()));

        var worklist = await service.GetStudentsNeedingDecisionAsync(gradeId, academicYearId, AsOf);

        Assert.Single(worklist, s => s.StudentPersonId == studentId);
    }
}

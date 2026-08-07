using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.LearningDelivery.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Application;

internal sealed class PromotionService(
    AssessmentEvaluationDbContext dbContext, IKeyStageEvaluationService keyStageEvaluationService,
    IKeyStagePolicyResolver keyStagePolicyResolver, IAchievementScaleQuery achievementScaleQuery,
    IStudentEnrollmentService enrollmentService, IClock clock)
    : IPromotionService
{
    public async Task<PromotionEligibilityResult> EvaluateEligibilityAsync(
        Guid studentPersonId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var enrollment = await enrollmentService.GetActiveEnrollmentAsync(studentPersonId, academicYearId, asOf, cancellationToken)
            ?? throw new InvalidOperationException("Student has no active enrolment for this academic year as of that date.");

        var policy = await keyStagePolicyResolver.ResolveAsync(enrollment.GradeId, academicYearId, asOf, cancellationToken)
            ?? throw new InvalidOperationException("No published key-stage policy exists for this student's grade/year.");

        if (policy.PromotionPolicyId is not { } promotionPolicyId)
        {
            throw new InvalidOperationException("No promotion policy is configured for this student's key stage.");
        }

        var promotionPolicy = await dbContext.PromotionPolicies.FindAsync([promotionPolicyId], cancellationToken)
            ?? throw new InvalidOperationException("The configured promotion policy was not found.");

        var evaluations = (await keyStageEvaluationService.GetAllCurrentForStudentAsync(studentPersonId, cancellationToken))
            .Where(e => e.EvaluationPeriodId == evaluationPeriodId)
            .ToList();

        // Resolved lazily, cached per achievement scale — most students share one scale across
        // several Mastery-model subjects within the same key stage.
        var levelRanksByScale = new Dictionary<Guid, IReadOnlyDictionary<Guid, int>>();
        var notCleared = new List<Guid>();

        foreach (var evaluation in evaluations)
        {
            if (!await ClearsBarAsync(evaluation, promotionPolicy.MinimumRank, levelRanksByScale, cancellationToken))
            {
                notCleared.Add(evaluation.SubjectId);
            }
        }

        var clearedCount = evaluations.Count - notCleared.Count;
        return new PromotionEligibilityResult(clearedCount >= promotionPolicy.MinimumSubjectsRequiredToClear, clearedCount, evaluations.Count, notCleared);
    }

    public async Task<Guid> RecordDecisionAsync(
        Guid studentPersonId, Guid academicYearId, bool promoted, Guid? nextGradeId, Guid decidedByPersonId, DateOnly decisionDate,
        string? notes, CancellationToken cancellationToken = default)
    {
        var enrollment = await enrollmentService.GetActiveEnrollmentAsync(studentPersonId, academicYearId, decisionDate, cancellationToken)
            ?? throw new InvalidOperationException("Student has no active enrolment for this academic year as of the decision date.");

        var decision = new PromotionDecision
        {
            Id = Guid.NewGuid(),
            StudentPersonId = studentPersonId,
            AcademicYearId = academicYearId,
            CurrentGradeId = enrollment.GradeId,
            Promoted = promoted,
            NextGradeId = nextGradeId,
            DecidedByPersonId = decidedByPersonId,
            DecisionDate = decisionDate,
            Notes = notes,
            RecordedAtUtc = clock.UtcNow,
        };
        dbContext.PromotionDecisions.Add(decision);
        await dbContext.SaveChangesAsync(cancellationToken);

        // The decision itself is the source of truth once recorded — closing the enrolment is a
        // mechanical follow-up safe to retry if it fails, which is why it happens second.
        await enrollmentService.EndEnrollmentAsync(enrollment.Id, decisionDate, cancellationToken);

        return decision.Id;
    }

    public async Task<IReadOnlyList<PromotionDecision>> GetDecisionsForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => await dbContext.PromotionDecisions
            .Where(d => d.StudentPersonId == studentPersonId)
            .OrderByDescending(d => d.RecordedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClassRosterEntry>> GetStudentsNeedingDecisionAsync(
        Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var roster = await enrollmentService.GetActiveRosterForGradeAsync(gradeId, academicYearId, asOf, cancellationToken);

        var decidedStudentIds = (await dbContext.PromotionDecisions
            .Where(d => d.AcademicYearId == academicYearId)
            .Select(d => d.StudentPersonId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        return roster.Where(r => !decidedStudentIds.Contains(r.StudentPersonId)).ToList();
    }

    private async Task<bool> ClearsBarAsync(
        KeyStageEvaluation evaluation, int minimumRank, Dictionary<Guid, IReadOnlyDictionary<Guid, int>> levelRanksByScale,
        CancellationToken cancellationToken)
    {
        if (evaluation.OverallGradeBandId is { } gradeBandId)
        {
            var rank = await dbContext.GradeBands.Where(b => b.Id == gradeBandId).Select(b => (int?)b.Rank).SingleOrDefaultAsync(cancellationToken);
            if (rank is not null)
            {
                return rank >= minimumRank;
            }
        }

        if (evaluation.OverallAchievementLevelId is { } levelId)
        {
            // The evaluation stamped its own KeyStagePolicyId when it was produced — resolved by id,
            // never re-resolved by grade/date, so a policy change afterward can't retroactively
            // change which achievement scale this historical evaluation is judged against.
            var evaluationPolicy = await keyStagePolicyResolver.GetByIdAsync(evaluation.KeyStagePolicyId, cancellationToken);
            if (evaluationPolicy?.AchievementScaleId is { } scaleId)
            {
                if (!levelRanksByScale.TryGetValue(scaleId, out var ranks))
                {
                    ranks = await achievementScaleQuery.GetLevelRanksAsync(scaleId, cancellationToken);
                    levelRanksByScale[scaleId] = ranks;
                }

                return ranks.TryGetValue(levelId, out var rank) && rank >= minimumRank;
            }
        }

        return false;
    }
}

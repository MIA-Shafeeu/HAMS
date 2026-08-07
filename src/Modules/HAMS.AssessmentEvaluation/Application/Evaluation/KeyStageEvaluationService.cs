using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Application.Evaluation;

internal sealed class KeyStageEvaluationService(
    AssessmentEvaluationDbContext dbContext, IEnumerable<IEvaluationEngine> engines,
    IStudentEnrollmentService enrollmentService, IKeyStagePolicyResolver policyResolver,
    IEvaluationModelLookup evaluationModelLookup, IClock clock) : IKeyStageEvaluationService
{
    public async Task<Guid> EvaluateAsync(
        Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        // The one place StudentEnrollment.GradeId is resolved in this whole chain — never a
        // Class's grade, so a combined-grade class can't leak one grade's policy onto the other's
        // students (build plan §3/§12).
        var enrollment = await enrollmentService.GetActiveEnrollmentAsync(studentPersonId, academicYearId, asOf, cancellationToken)
            ?? throw new InvalidOperationException("Student has no active enrolment for this academic year as of that date.");

        var policy = await policyResolver.ResolveAsync(enrollment.GradeId, academicYearId, asOf, cancellationToken)
            ?? throw new InvalidOperationException("No published key-stage policy exists for this student's grade/year.");

        var evaluationModel = await evaluationModelLookup.GetByIdAsync(policy.EvaluationModelId, cancellationToken)
            ?? throw new InvalidOperationException("The configured evaluation model was not found.");

        var period = await dbContext.EvaluationPeriods.FindAsync([evaluationPeriodId], cancellationToken)
            ?? throw new InvalidOperationException("Evaluation period not found.");

        var engine = engines.SingleOrDefault(e => e.ModelCode == evaluationModel.Code)
            ?? throw new InvalidOperationException($"No evaluation engine registered for model '{evaluationModel.Code}'.");

        var context = new EvaluationContext(studentPersonId, subjectId, enrollment.GradeId, academicYearId, policy, period);
        var outcome = await engine.EvaluateAsync(context, cancellationToken);

        var evaluation = new KeyStageEvaluation
        {
            Id = Guid.NewGuid(),
            StudentPersonId = studentPersonId,
            SubjectId = subjectId,
            EvaluationPeriodId = evaluationPeriodId,
            KeyStagePolicyId = policy.Id,
            EvaluationModelId = evaluationModel.Id,
            OverallAchievementLevelId = outcome.AchievementLevelId,
            OverallPercentage = outcome.OverallPercentage,
            OverallGradeBandId = outcome.GradeBandId,
            RecordedAtUtc = clock.UtcNow,
        };
        dbContext.KeyStageEvaluations.Add(evaluation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return evaluation.Id;
    }

    public async Task<KeyStageEvaluation?> GetCurrentAsync(
        Guid studentPersonId, Guid subjectId, Guid evaluationPeriodId, CancellationToken cancellationToken = default)
        => await dbContext.KeyStageEvaluations
            .Where(e => e.StudentPersonId == studentPersonId && e.SubjectId == subjectId && e.EvaluationPeriodId == evaluationPeriodId)
            .OrderByDescending(e => e.RecordedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<KeyStageEvaluation>> GetAllCurrentForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
    {
        // A single student's whole evaluation history (every subject x period x re-evaluation) is
        // small by nature, so group-to-latest-per-subject-period is done in memory rather than
        // reaching for a GroupBy/OrderBy/First shape EF Core's SQL translator handles inconsistently.
        var all = await dbContext.KeyStageEvaluations
            .Where(e => e.StudentPersonId == studentPersonId)
            .ToListAsync(cancellationToken);

        return all
            .GroupBy(e => (e.SubjectId, e.EvaluationPeriodId))
            .Select(g => g.OrderByDescending(e => e.RecordedAtUtc).First())
            .ToList();
    }
}

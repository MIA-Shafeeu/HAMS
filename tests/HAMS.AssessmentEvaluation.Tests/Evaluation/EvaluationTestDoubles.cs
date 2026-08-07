using HAMS.AssessmentEvaluation.Domain;
using HAMS.LearningDelivery.Application;
using HAMS.LearningDelivery.Domain;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.PeopleEnrollment.Application;
using HAMS.PeopleEnrollment.Domain;

namespace HAMS.AssessmentEvaluation.Tests.Evaluation;

/// <summary>
/// Fake cross-module test doubles for Phase 8's evaluation engine tests — the real implementations
/// all live in other modules and need a real relational provider or unrelated setup; these are
/// configured directly per test, the same pattern as every prior phase's Fake* doubles.
/// Tracks ended enrolments via <see cref="FakeStudentEnrollmentService.EndedEnrollmentIds"/> —
/// needed for real (not throw-away) use by Phase 11's <c>PromotionService</c> tests, which must
/// assert <c>RecordDecisionAsync</c> actually closes the enrolment.
/// </summary>
internal sealed class FakeStudentEnrollmentService : IStudentEnrollmentService
{
    private readonly List<StudentEnrollment> _enrollments;

    public FakeStudentEnrollmentService(params StudentEnrollment[] enrollments) => _enrollments = [.. enrollments];

    public List<Guid> EndedEnrollmentIds { get; } = [];

    public Task<Guid> EnrollAsync(Guid studentPersonId, Guid gradeId, Guid classId, Guid academicYearId, DateOnly effectiveFrom, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by evaluation engine tests.");

    public Task<StudentEnrollment?> GetActiveEnrollmentAsync(Guid studentPersonId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(_enrollments.SingleOrDefault(e =>
            e.StudentPersonId == studentPersonId && e.AcademicYearId == academicYearId
            && e.EffectiveFrom <= asOf && (e.EffectiveTo == null || e.EffectiveTo >= asOf)));

    public Task EndEnrollmentAsync(Guid enrollmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default)
    {
        var enrollment = _enrollments.SingleOrDefault(e => e.Id == enrollmentId)
            ?? throw new InvalidOperationException("Enrolment not found.");
        if (enrollment.EffectiveTo is not null)
        {
            throw new InvalidOperationException("Enrolment is already closed.");
        }

        enrollment.EffectiveTo = effectiveTo;
        EndedEnrollmentIds.Add(enrollmentId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForClassAsync(Guid classId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by evaluation engine tests.");

    public Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForGradeAsync(Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ClassRosterEntry>>(_enrollments
            .Where(e => e.GradeId == gradeId && e.AcademicYearId == academicYearId && e.EffectiveFrom <= asOf && (e.EffectiveTo == null || e.EffectiveTo >= asOf))
            .Select(e => new ClassRosterEntry(e.StudentPersonId, $"Student {e.StudentPersonId}", $"Student {e.StudentPersonId}", e.StudentPersonId.ToString()))
            .ToList());
}

/// <summary>Keyed directly by grade id (a test-only convenience — the real resolver's actual <c>Grade -&gt; GradeKeyStageAssignment -&gt; KeyStagePolicy</c> indirection is OrgCurriculum's own, already-tested concern; the dispatcher only needs a correct <c>gradeId -&gt; KeyStagePolicy</c> mapping to test its own logic). Also keeps a secondary by-id index so <see cref="GetByIdAsync"/> works for Phase 11's <c>PromotionService</c> tests, which resolve a stored <c>KeyStagePolicyId</c> off an evaluation row rather than by grade.</summary>
internal sealed class FakeKeyStagePolicyResolver(IReadOnlyDictionary<Guid, KeyStagePolicy> policiesByGradeId) : IKeyStagePolicyResolver
{
    private readonly Dictionary<Guid, KeyStagePolicy> _policiesById = policiesByGradeId.Values.ToDictionary(p => p.Id);

    public Task<KeyStagePolicy?> ResolveAsync(Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default)
        => Task.FromResult(policiesByGradeId.GetValueOrDefault(gradeId));

    public Task<KeyStagePolicy?> GetByIdAsync(Guid keyStagePolicyId, CancellationToken cancellationToken = default)
        => Task.FromResult(_policiesById.GetValueOrDefault(keyStagePolicyId));
}

internal sealed class FakeEvaluationModelLookup(params EvaluationModel[] models) : IEvaluationModelLookup
{
    public Task<EvaluationModel?> GetByIdAsync(Guid evaluationModelId, CancellationToken cancellationToken = default)
        => Task.FromResult(models.SingleOrDefault(m => m.Id == evaluationModelId));
}

internal sealed class FakeSyllabusResolver(Syllabus? syllabus, IReadOnlyList<Guid>? outcomeIds = null) : ISyllabusResolver
{
    public Task<Syllabus?> ResolveAsync(Guid subjectId, Guid gradeId, CancellationToken cancellationToken = default)
        => Task.FromResult(syllabus);

    public Task<IReadOnlyList<Guid>> GetLearningOutcomeIdsAsync(Guid syllabusId, CancellationToken cancellationToken = default)
        => Task.FromResult(outcomeIds ?? []);
}

internal sealed class FakeMasteryEvaluationService(IReadOnlyDictionary<Guid, MasteryEvaluation> currentByOutcome) : IMasteryEvaluationService
{
    public Task<Guid> RecordEvaluationAsync(
        Guid studentPersonId, Guid learningOutcomeId, Guid keyStagePolicyId, Guid achievementScaleId,
        Guid recordedByPersonId, Guid? manualAchievementLevelId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by evaluation engine tests.");

    public Task<MasteryEvaluation?> GetCurrentAsync(Guid studentPersonId, Guid learningOutcomeId, CancellationToken cancellationToken = default)
        => Task.FromResult(currentByOutcome.GetValueOrDefault(learningOutcomeId));

    public Task<IReadOnlyDictionary<Guid, MasteryEvaluation>> GetCurrentForOutcomesAsync(
        Guid studentPersonId, IReadOnlyList<Guid> learningOutcomeIds, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, MasteryEvaluation>>(
            currentByOutcome.Where(kvp => learningOutcomeIds.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
}

internal sealed class FakeAchievementScaleQuery(IReadOnlyDictionary<Guid, int> ranks) : IAchievementScaleQuery
{
    public Task<IReadOnlyDictionary<Guid, int>> GetLevelRanksAsync(Guid achievementScaleId, CancellationToken cancellationToken = default)
        => Task.FromResult(ranks);
}

/// <summary>Used by Phase 11's <c>PromotionService</c> tests, which need a fixed set of "current" evaluations for a student rather than the dispatch/recording behaviour <see cref="KeyStageEvaluationService"/> itself already covers.</summary>
internal sealed class FakeKeyStageEvaluationService(params KeyStageEvaluation[] evaluations) : HAMS.AssessmentEvaluation.Application.Evaluation.IKeyStageEvaluationService
{
    public Task<Guid> EvaluateAsync(Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by promotion service tests.");

    public Task<KeyStageEvaluation?> GetCurrentAsync(Guid studentPersonId, Guid subjectId, Guid evaluationPeriodId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not needed by promotion service tests.");

    public Task<IReadOnlyList<KeyStageEvaluation>> GetAllCurrentForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeyStageEvaluation>>(evaluations.Where(e => e.StudentPersonId == studentPersonId).ToList());
}

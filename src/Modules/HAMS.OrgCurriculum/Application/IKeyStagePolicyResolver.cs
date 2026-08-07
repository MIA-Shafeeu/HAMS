using HAMS.OrgCurriculum.Domain;

namespace HAMS.OrgCurriculum.Application;

/// <summary>
/// The concrete evaluation-model cascade mechanism (build plan §3): resolves a grade to its
/// currently-published <see cref="KeyStagePolicy"/> via
/// <c>Grade -&gt; GradeKeyStageAssignment.ActiveAsOf -&gt; KeyStagePolicy (IsCurrent)</c>.
///
/// Deliberately takes <see cref="Guid"/> gradeId as input rather than a <c>StudentEnrollment</c> —
/// PeopleEnrollment (Phase 3) will call this the same way once
/// <c>Student -&gt; StudentEnrollment.GradeId</c> exists; nothing about this resolver changes.
/// Callers MUST resolve from <c>StudentEnrollment.GradeId</c>, never from <c>Class</c> — a combined
/// Grade-5/6 class must not let one grade's students inherit the other grade's policy (§12).
/// </summary>
public interface IKeyStagePolicyResolver
{
    /// <returns>Null if the grade has no active key-stage assignment, or that key stage has no published policy, as of the given date.</returns>
    Task<KeyStagePolicy?> ResolveAsync(Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a policy already known by id — Phase 11's promotion eligibility check needs a
    /// stored <c>KeyStageEvaluation.KeyStagePolicyId</c>'s own <c>AchievementScaleId</c>/<c>GradeScaleId</c>,
    /// not a fresh as-of-date resolution (the evaluation already stamped which policy applied when
    /// it was produced — re-resolving by grade/date could silently pick up a newer policy).
    /// </summary>
    Task<KeyStagePolicy?> GetByIdAsync(Guid keyStagePolicyId, CancellationToken cancellationToken = default);
}

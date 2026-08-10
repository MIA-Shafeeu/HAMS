using HAMS.OrgCurriculum.Domain;

namespace HAMS.OrgCurriculum.Application;

/// <summary>
/// Resolves the currently-published <see cref="Syllabus"/> a subject teaches to a given grade —
/// the concrete lookup later phases (Scheme of Work, Lesson Planning, Mastery/Assessment) will use
/// to find the exact <c>LearningOutcomeId</c>/<c>IndicatorId</c> set in force. Unlike
/// <c>IKeyStagePolicyResolver</c>, this takes no as-of date: <see cref="Syllabus"/> versioning is
/// driven entirely by <c>IsCurrent</c>/<c>Status</c>, not an effective-dated range.
/// </summary>
public interface ISyllabusResolver
{
    /// <returns>Null if the subject has no published syllabus, or none applicable to that grade.</returns>
    Task<Syllabus?> ResolveAsync(Guid subjectId, Guid gradeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every <c>LearningOutcome</c> in <paramref name="syllabusId"/>'s tree (<c>Syllabus → Strand →
    /// SubStrand → LearningOutcome</c>) — the enumeration Phase 8's Mastery evaluation engine needs
    /// to know which outcomes make up "the whole subject" for a grade, since no denormalized
    /// <c>SubjectId</c> exists directly on <c>LearningOutcome</c>.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetLearningOutcomeIdsAsync(Guid syllabusId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same tree as <see cref="GetLearningOutcomeIdsAsync"/>, but with the Strand/SubStrand/Outcome
    /// display text a human picker (e.g. the Scheme of Work item form) needs instead of a bare id.
    /// </summary>
    Task<IReadOnlyList<LearningOutcomeOption>> GetLearningOutcomeOptionsAsync(Guid syllabusId, CancellationToken cancellationToken = default);
}

/// <summary>A <see cref="LearningOutcome"/>, with its ancestor Strand/SubStrand names, for display in a picker.</summary>
public sealed record LearningOutcomeOption(Guid Id, string StrandName, string SubStrandName, string Code, string Description);

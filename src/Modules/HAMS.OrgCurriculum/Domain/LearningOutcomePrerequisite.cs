namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// Self-referencing M:M (build plan §3): <see cref="LearningOutcomeId"/> requires
/// <see cref="PrerequisiteLearningOutcomeId"/> to have been mastered first — both outcomes must
/// belong to the same <see cref="Syllabus"/> revision (enforced by application code, not a DB
/// constraint, since it spans two FKs).
/// </summary>
public sealed class LearningOutcomePrerequisite
{
    public Guid Id { get; init; }

    public Guid LearningOutcomeId { get; init; }

    public Guid PrerequisiteLearningOutcomeId { get; init; }
}

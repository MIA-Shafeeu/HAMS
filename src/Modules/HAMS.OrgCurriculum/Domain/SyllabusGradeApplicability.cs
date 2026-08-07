namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// Which grades a specific <see cref="Syllabus"/> revision applies to (build plan §3: "applicability
/// M:M to grades/key stages"). Deliberately keyed on <see cref="Grade"/>, not <see cref="KeyStage"/>
/// directly — a grade's key stage is already resolvable via <see cref="GradeKeyStageAssignment"/>
/// (Phase 1), so storing key-stage applicability separately would risk the two drifting out of sync.
/// </summary>
public sealed class SyllabusGradeApplicability
{
    public Guid Id { get; init; }

    public Guid SyllabusId { get; init; }

    public Guid GradeId { get; init; }
}

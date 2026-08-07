using HAMS.Platform.Common.Contracts;

namespace HAMS.OrgCurriculum.Domain;

/// <summary>
/// Assigns a <see cref="Grade"/> to a <see cref="KeyStage"/> for an academic year — the link the
/// evaluation-model cascade resolves through (<c>StudentEnrollment.GradeId -&gt;
/// GradeKeyStageAssignment.KeyStageId -&gt; KeyStagePolicy</c>, build plan §3/§12). Effective-dated
/// so a grade's key-stage mapping can change between academic years without losing history.
/// </summary>
public sealed class GradeKeyStageAssignment : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid GradeId { get; init; }

    public Guid KeyStageId { get; init; }

    public Guid AcademicYearId { get; init; }

    public DateOnly EffectiveFrom { get; init; }

    public DateOnly? EffectiveTo { get; set; }
}
